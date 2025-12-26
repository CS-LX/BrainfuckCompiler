namespace BrainfxxkCompiler.Interpreter {
    public interface IBFInterpreter {
        int DataLength { get; }
        string Run(string code, out int[] data);
    }
}