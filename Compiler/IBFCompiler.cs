using BrainfxxkCompiler.Settings;

namespace BrainfxxkCompiler.Compiler {
    public interface IBFCompiler {
        void CompileToExe(string brainfuckCode, string outputFilePath, CompileSettings settings);
    }
}