using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainfuckCompiler
{
    public static class BFCompiler
    {
        public static int dataLength = 3000;
        /// <summary>
        /// 一次性执行完整个BF并且直接返回数据和结果
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Run(string code, out int[] data)
        {
            int dataPointer = 0;
            int instructionPointer = 0;
            data = new int[dataLength]; // Brainfuck的数据存储区域，默认大小为3000

            Stack<int> loopStack = new Stack<int>();
            StringBuilder output = new StringBuilder();
            while (instructionPointer < code.Length)
            {
                char currentInstruction = code[instructionPointer];

                switch (currentInstruction)
                {
                    case '>':
                        dataPointer++;
                        break;
                    case '<':
                        dataPointer--;
                        break;
                    case '+':
                        data[dataPointer]++;
                        break;
                    case '-':
                        data[dataPointer]--;
                        break;
                    case '.':
                        output.Append((char)data[dataPointer]);
                        break;
                    case ',':
                        InputIntDialog intDialog = new InputIntDialog();
                        int i = 0;
                        if (intDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            i = intDialog.num;
                        }
                        data[dataPointer] = i;
                        break;
                    case '[':
                        if (data[dataPointer] == 0)
                        {
                            int loopCount = 1;
                            while (loopCount > 0)
                            {
                                instructionPointer++;
                                if (code[instructionPointer] == '[')
                                    loopCount++;
                                else if (code[instructionPointer] == ']')
                                    loopCount--;
                            }
                        }
                        else
                        {
                            loopStack.Push(instructionPointer);
                        }
                        break;
                    case ']':
                        if (data[dataPointer] != 0)
                        {
                            instructionPointer = loopStack.Peek();
                        }
                        else
                        {
                            loopStack.Pop();
                        }
                        break;
                    default:
                        // 忽略其他字符
                        break;
                }

                instructionPointer++;
            }

            return output.ToString();
        }
        public static string CompileToCSharp(string brainfuckCode)
        {
            StringBuilder cSharpCode = new StringBuilder();
            cSharpCode.AppendLine("using System;");
            cSharpCode.AppendLine("using System.Collections.Generic;");
            cSharpCode.AppendLine("using System.Text;");
            cSharpCode.AppendLine("namespace BrainfuckProgram");
            cSharpCode.AppendLine("{");
            cSharpCode.AppendLine("class Program");
            cSharpCode.AppendLine("{");
            cSharpCode.AppendLine("static void Main(string[] args)");
            cSharpCode.AppendLine("{");
            cSharpCode.AppendLine($"byte[] memory = new byte[{dataLength}];");
            cSharpCode.AppendLine("int pointer = 0;");
            cSharpCode.AppendLine("List<char> output = new List<char>();");
            cSharpCode.AppendLine("int inputIndex = 0;");
            cSharpCode.AppendLine("try");
            cSharpCode.AppendLine("{");

            for (int i = 0; i < brainfuckCode.Length; i++)
            {
                char c = brainfuckCode[i];
                switch (c)
                {
                    case '>':
                        cSharpCode.AppendLine("pointer++;");
                        break;
                    case '<':
                        cSharpCode.AppendLine("pointer--;");
                        break;
                    case '+':
                        cSharpCode.AppendLine("memory[pointer]++;");
                        break;
                    case '-':
                        cSharpCode.AppendLine("memory[pointer]--;");
                        break;
                    case '.':
                        cSharpCode.AppendLine("output.Add((char)memory[pointer]);");
                        break;
                    case ',':
                        cSharpCode.AppendLine("Console.WriteLine(\"请输入ASCII码\");");
                        cSharpCode.AppendLine("memory[pointer] = byte.Parse(Console.ReadLine());");
                        break;
                    case '[':
                        cSharpCode.AppendLine("while (memory[pointer] != 0)");
                        cSharpCode.AppendLine("{");
                        break;
                    case ']':
                        cSharpCode.AppendLine("}");
                        break;
                }
            }

            cSharpCode.AppendLine("}");
            cSharpCode.AppendLine("catch (Exception ex)");
            cSharpCode.AppendLine("{");
            cSharpCode.AppendLine("Console.WriteLine(ex.Message);");
            cSharpCode.AppendLine("}");
            cSharpCode.AppendLine("Console.WriteLine(new string(output.ToArray()));");
            cSharpCode.AppendLine("Console.ReadKey();");
            cSharpCode.AppendLine("}");
            cSharpCode.AppendLine("}");
            cSharpCode.AppendLine("}");

            return cSharpCode.ToString();
        }

        static void CompileToExe(string cSharpCode, string outputFilePath)
        {
            CodeDomProvider codeProvider = CodeDomProvider.CreateProvider("CSharp");
            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.GenerateExecutable = true;
            compilerParams.OutputAssembly = outputFilePath;

            CompilerResults compilerResults = codeProvider.CompileAssemblyFromSource(compilerParams, cSharpCode);

            if (compilerResults.Errors.Count > 0)
            {
                foreach (CompilerError error in compilerResults.Errors)
                {
                    if (!error.IsWarning)
                        throw new Exception(error.ErrorText);
                }
            }
        }

        /// <summary>
        /// BF语句直接成exe
        /// </summary>
        /// <param name="brainfuckCode"></param>
        /// <param name="outputFilePath"></param>
        public static void BFToExe(string brainfuckCode, string outputFilePath)
        {
            CompileToExe(CompileToCSharp(brainfuckCode), outputFilePath);
        }
    }
}
