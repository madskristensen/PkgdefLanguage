using BenchmarkDotNet.Running;

using Microsoft.VisualStudio.TestTools.UnitTesting;
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace PkgdefLanguage.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var _ = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
