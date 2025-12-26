using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainfxxkCompiler.Interpreter {
    public class BFInterpreter : IBFInterpreter {
        public const int DataLengthConst = short.MaxValue;

        public int DataLength => DataLengthConst;

        /// <summary>
        /// 一次性执行完整个BF并且直接返回数据和结果
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string Run(string code, out int[] data) {
            int dataPointer = 0;
            int instructionPointer = 0;
            data = new int[DataLength]; // Brainfuck的数据存储区域，默认大小为3000
            Stack<int> loopStack = new Stack<int>();
            StringBuilder output = new StringBuilder();
            while (instructionPointer < code.Length) {
                char currentInstruction = code[instructionPointer];
                switch (currentInstruction) {
                    case '>': dataPointer++; break;
                    case '<': dataPointer--; break;
                    case '+': data[dataPointer]++; break;
                    case '-': data[dataPointer]--; break;
                    case '.': output.Append((char)data[dataPointer]); break;
                    case ',':
                        InputIntDialog intDialog = new InputIntDialog();
                        int i = 0;
                        if (intDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                            i = intDialog.num;
                        }
                        data[dataPointer] = i;
                        break;
                    case '[':
                        if (data[dataPointer] == 0) {
                            int loopCount = 1;
                            while (loopCount > 0) {
                                instructionPointer++;
                                if (code[instructionPointer] == '[')
                                    loopCount++;
                                else if (code[instructionPointer] == ']') loopCount--;
                            }
                        }
                        else {
                            loopStack.Push(instructionPointer);
                        }
                        break;
                    case ']':
                        if (data[dataPointer] != 0) {
                            instructionPointer = loopStack.Peek();
                        }
                        else {
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
    }
}