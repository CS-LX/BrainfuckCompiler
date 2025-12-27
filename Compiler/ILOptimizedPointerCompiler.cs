using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.IO;
using BrainfxxkCompiler.Settings;

namespace BrainfxxkCompiler.Compiler
{
    public class ILOptimizedPointerCompiler : IBFCompiler
    {
        // 定义内部中间指令
        private enum Op { Add, Move, Out, In, LoopStart, LoopEnd, Clear }
        private struct Instruction {
            public Op Type;
            public int Value;
        }

        public void CompileToExe(string brainfuckCode, string outputFilePath, CompileSettings settings)
        {
            string fileName = Path.GetFileName(outputFilePath);
            string assemblyName = Path.GetFileNameWithoutExtension(fileName);

            // 1. 预解析与优化阶段
            var optimizedIns = PreProcess(brainfuckCode);

            // 2. 设置反射和程序集
            AssemblyName name = new AssemblyName(assemblyName);
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
            ModuleBuilder mb = ab.DefineDynamicModule(assemblyName, fileName);
            TypeBuilder tb = mb.DefineType("Program", TypeAttributes.Public);
            MethodBuilder meb = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(string[]) });

            ILGenerator il = meb.GetILGenerator();

            // 3. 定义变量 (依然使用最高效的原生指针)
            LocalBuilder lbBaseAddr = il.DeclareLocal(typeof(IntPtr));
            LocalBuilder lbCurrentPtr = il.DeclareLocal(typeof(byte*));

            // 分配 30000 字节内存
            il.Emit(OpCodes.Ldc_I4, (int)short.MaxValue);
            il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("AllocHGlobal", new[] { typeof(int) }));
            il.Emit(OpCodes.Stloc, lbBaseAddr);

            // 初始化内存为 0
            il.Emit(OpCodes.Ldloc, lbBaseAddr);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4, (int)short.MaxValue);
            il.Emit(OpCodes.Initblk);

            // 设置初始指针
            il.Emit(OpCodes.Ldloc, lbBaseAddr);
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Stloc, lbCurrentPtr);

            Stack<(Label Start, Label End)> loopStack = new Stack<(Label, Label)>();

            // 4. 根据优化后的指令流发射 IL
            foreach (var ins in optimizedIns)
            {
                switch (ins.Type)
                {
                    case Op.Move: // ptr += n
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldc_I4, ins.Value);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Stloc, lbCurrentPtr);
                        break;

                    case Op.Add: // *ptr += n
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldind_U1);
                        il.Emit(OpCodes.Ldc_I4, ins.Value);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Stind_I1);
                        break;

                    case Op.Clear: // *ptr = 0
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Stind_I1);
                        break;

                    case Op.Out: // 输出
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldind_U1);
                        il.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new[] { typeof(char) }));
                        break;

                    case Op.In: // 输入逻辑保持不变
                        switch (settings.InputMode) {
                            case InputMode.ASCIICode:
                                // *currentPtr = byte.Parse(Console.ReadLine())
                                il.Emit(OpCodes.Ldstr, "请输入ASCII码");
                                il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }));
                                il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                                il.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadLine"));
                                il.Emit(OpCodes.Call, typeof(byte).GetMethod("Parse", new[] { typeof(string) }));
                                il.Emit(OpCodes.Stind_I1); // 存储到指针地址
                                break;
                            case InputMode.Char:
                                // *currentPtr = Convert.ToByte(Console.ReadLine()[0]);
                                il.Emit(OpCodes.Ldstr, "请输入字符:");
                                il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }));
                                il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                                il.Emit(OpCodes.Call, typeof(Console).GetMethod("Read", Type.EmptyTypes));
                                il.Emit(OpCodes.Conv_U1);
                                il.Emit(OpCodes.Stind_I1);
                                break;
                        }
                        break;

                    case Op.LoopStart:
                        Label start = il.DefineLabel();
                        Label end = il.DefineLabel();
                        loopStack.Push((start, end));
                        il.MarkLabel(start);
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldind_U1);
                        il.Emit(OpCodes.Brfalse, end);
                        break;

                    case Op.LoopEnd:
                        var labels = loopStack.Pop();
                        il.Emit(OpCodes.Br, labels.Start);
                        il.MarkLabel(labels.End);
                        break;
                }
            }

            // 5. 收尾逻辑
            il.Emit(OpCodes.Ldloc, lbBaseAddr);
            il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("FreeHGlobal", new[] { typeof(IntPtr) }));
            il.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadKey", Type.EmptyTypes));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

            tb.CreateType();
            ab.SetEntryPoint(meb, PEFileKinds.ConsoleApplication);
            ab.Save(fileName);

            if (Path.GetFullPath(fileName) != Path.GetFullPath(outputFilePath)) {
                if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                File.Move(fileName, outputFilePath);
            }
        }

        // 核心：将 BF 字符转换为优化后的指令列表
        private List<Instruction> PreProcess(string code)
        {
            var list = new List<Instruction>();
            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];

                // 优化 1: 合并 [-] 清零操作
                if (c == '[' && i + 2 < code.Length && code[i + 1] == '-' && code[i + 2] == ']') {
                    list.Add(new Instruction { Type = Op.Clear });
                    i += 2;
                    continue;
                }

                // 优化 2: 合并连续的 + - > <
                if ("+-<>".Contains(c.ToString())) {
                    int count = 0;
                    char current = c;
                    while (i < code.Length && code[i] == current) {
                        count++; i++;
                    }
                    i--; // 抵消外层循环的 i++

                    if (current == '+') list.Add(new Instruction { Type = Op.Add, Value = count });
                    else if (current == '-') list.Add(new Instruction { Type = Op.Add, Value = -count });
                    else if (current == '>') list.Add(new Instruction { Type = Op.Move, Value = count });
                    else if (current == '<') list.Add(new Instruction { Type = Op.Move, Value = -count });
                }
                else if (c == '.') list.Add(new Instruction { Type = Op.Out });
                else if (c == ',') list.Add(new Instruction { Type = Op.In });
                else if (c == '[') list.Add(new Instruction { Type = Op.LoopStart });
                else if (c == ']') list.Add(new Instruction { Type = Op.LoopEnd });
            }
            return list;
        }
    }
}