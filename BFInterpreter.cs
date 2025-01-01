using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrainfxxkCompiler
{
    public class BFInterpreter
    {
        private int dataLength;
        /// <summary>
        /// 数据长度
        /// </summary>
        public int DataLength
        {
            get => dataLength;
        }

        private int pointer;
        /// <summary>
        /// 指针位置
        /// </summary>
        public int Pointer
        {
            get => pointer;
        }

        private string code;

        public BFInterpreter(int dataLength, string code)
        {
            this.dataLength = dataLength;
            this.pointer = 0;
            this.code = code;
        }
    }
}
