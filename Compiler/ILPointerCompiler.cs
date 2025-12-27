using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.IO;

namespace BrainfxxkCompiler.Compiler
{
    public class ILPointerCompiler : IBFCompiler
    {
        public void CompileToExe(string brainfuckCode, string outputFilePath)
        {
            string fileName = Path.GetFileName(outputFilePath);
            string assemblyName = Path.GetFileNameWithoutExtension(fileName);

            AssemblyName name = new AssemblyName(assemblyName);
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
            ModuleBuilder mb = ab.DefineDynamicModule(assemblyName, fileName);

            TypeBuilder tb = mb.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            MethodBuilder meb = tb.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(string[]) });

            ILGenerator il = meb.GetILGenerator();

            // --- 变量定义 ---
            // Local 0: IntPtr baseAddress (纸带起始地址)
            // Local 1: byte* currentPtr (当前数据指针)
            LocalBuilder lbBaseAddr = il.DeclareLocal(typeof(IntPtr));
            LocalBuilder lbCurrentPtr = il.DeclareLocal(typeof(byte*));

            // 1. 申请内存: baseAddress = Marshal.AllocHGlobal(short.MaxValue)
            il.Emit(OpCodes.Ldc_I4, (int)short.MaxValue);
            il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("AllocHGlobal", new[] { typeof(int) }));
            il.Emit(OpCodes.Stloc, lbBaseAddr);

            // 2. 初始化内存为 0 (类似 ZeroMemory)
            il.Emit(OpCodes.Ldloc, lbBaseAddr);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4, (int)short.MaxValue);
            il.Emit(OpCodes.Initblk); // 直接初始化内存块

            // 3. 设置当前指针: currentPtr = (byte*)baseAddress
            il.Emit(OpCodes.Ldloc, lbBaseAddr);
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Stloc, lbCurrentPtr);

            Stack<(Label Start, Label End)> loopStack = new Stack<(Label, Label)>();

            foreach (char c in brainfuckCode)
            {
                switch (c)
                {
                    case '>': // currentPtr++
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Stloc, lbCurrentPtr);
                        break;

                    case '<': // currentPtr--
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Sub);
                        il.Emit(OpCodes.Stloc, lbCurrentPtr);
                        break;

                    case '+': // *currentPtr += 1
                        EmitModifyAtPointer(il, lbCurrentPtr, 1);
                        break;

                    case '-': // *currentPtr -= 1
                        EmitModifyAtPointer(il, lbCurrentPtr, -1);
                        break;

                    case '.': // Console.Write((char)*currentPtr)
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldind_U1); // 加载指针指向的值
                        il.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new[] { typeof(char) }));
                        break;

                    case ',': // *currentPtr = byte.Parse(Console.ReadLine())
                        il.Emit(OpCodes.Ldstr, "请输入ASCII码");
                        il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }));
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadLine"));
                        il.Emit(OpCodes.Call, typeof(byte).GetMethod("Parse", new[] { typeof(string) }));
                        il.Emit(OpCodes.Stind_I1); // 存储到指针地址
                        break;

                    case '[':
                        Label startLabel = il.DefineLabel();
                        Label endLabel = il.DefineLabel();
                        loopStack.Push((startLabel, endLabel));
                        il.MarkLabel(startLabel);
                        il.Emit(OpCodes.Ldloc, lbCurrentPtr);
                        il.Emit(OpCodes.Ldind_U1);
                        il.Emit(OpCodes.Brfalse, endLabel);
                        break;

                    case ']':
                        var labels = loopStack.Pop();
                        il.Emit(OpCodes.Br, labels.Start);
                        il.MarkLabel(labels.End);
                        break;
                }
            }

            // 释放内存 (规范起见)
            il.Emit(OpCodes.Ldloc, lbBaseAddr);
            il.Emit(OpCodes.Call, typeof(Marshal).GetMethod("FreeHGlobal", new[] { typeof(IntPtr) }));

            il.Emit(OpCodes.Call, typeof(Console).GetMethod("ReadKey", Type.EmptyTypes));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

            tb.CreateType();
            ab.SetEntryPoint(meb, PEFileKinds.ConsoleApplication);
            ab.Save(fileName);

            // 移动到目标路径
            if (Path.GetFullPath(fileName) != Path.GetFullPath(outputFilePath))
            {
                if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                File.Move(fileName, outputFilePath);
            }
        }

        private void EmitModifyAtPointer(ILGenerator il, LocalBuilder lbCurrentPtr, int delta)
        {
            // 逻辑: *ptr = (byte)(*ptr + delta)
            il.Emit(OpCodes.Ldloc, lbCurrentPtr); // 压入地址
            il.Emit(OpCodes.Dup);                 // 复制地址用于存储
            il.Emit(OpCodes.Ldind_U1);            // 取值
            il.Emit(OpCodes.Ldc_I4, delta);       // 压入增量
            il.Emit(OpCodes.Add);                 // 加法
            il.Emit(OpCodes.Stind_I1);            // 写回地址
        }
    }
}