using System;
using System.CodeDom.Compiler;
using System.Text;

namespace BrainfxxkCompiler.Compiler {
    public class FullCSharpCompiler : IBFCompiler {
        public static string CompileBrainfuck(string brainfuckCode) {
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
            cSharpCode.AppendLine($"byte[] memory = new byte[{short.MaxValue}];");
            cSharpCode.AppendLine("int pointer = 0;");
            cSharpCode.AppendLine("List<char> output = new List<char>();");
            cSharpCode.AppendLine("int inputIndex = 0;");
            cSharpCode.AppendLine("try");
            cSharpCode.AppendLine("{");
            for (int i = 0; i < brainfuckCode.Length; i++) {
                char c = brainfuckCode[i];
                switch (c) {
                    case '>': cSharpCode.AppendLine("pointer++;"); break;
                    case '<': cSharpCode.AppendLine("pointer--;"); break;
                    case '+': cSharpCode.AppendLine("memory[pointer]++;"); break;
                    case '-': cSharpCode.AppendLine("memory[pointer]--;"); break;
                    case '.': cSharpCode.AppendLine("output.Add((char)memory[pointer]);"); break;
                    case ',':
                        cSharpCode.AppendLine("Console.WriteLine(\"请输入ASCII码\");");
                        cSharpCode.AppendLine("memory[pointer] = byte.Parse(Console.ReadLine());");
                        break;
                    case '[':
                        cSharpCode.AppendLine("while (memory[pointer] != 0)");
                        cSharpCode.AppendLine("{");
                        break;
                    case ']': cSharpCode.AppendLine("}"); break;
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

        public static void CompileCSharp(string cSharpCode, string outputFilePath) {
            CodeDomProvider codeProvider = CodeDomProvider.CreateProvider("CSharp");
            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.GenerateExecutable = true;
            compilerParams.OutputAssembly = outputFilePath;
            CompilerResults compilerResults = codeProvider.CompileAssemblyFromSource(compilerParams, cSharpCode);
            if (compilerResults.Errors.Count > 0) {
                foreach (CompilerError error in compilerResults.Errors) {
                    if (!error.IsWarning) throw new Exception(error.ErrorText);
                }
            }
        }

        public void CompileToExe(string brainfuckCode, string outputFilePath) {
            CompileCSharp(CompileBrainfuck(brainfuckCode), outputFilePath);
        }
    }
}