using BenchmarkDotNet.Attributes;

using Microsoft.VSDiagnostics;

using System.Threading.Tasks;

namespace PkgdefLanguage.Benchmarks
{
    public static class DocumentExtensions
    {
        public static async Task WaitForParsingCompleteAsync(this Document document)
        {
            while (document.IsProcessing)
            {
                await Task.Delay(2);
            }
        }
    }

    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class ParsingBenchmark
    {
        private string[] _smallDocument;
        private string[] _mediumDocument;
        private string[] _largeDocument;
        [GlobalSetup]
        public void Setup()
        {
            // Small document: 50 lines
            _smallDocument = GenerateDocument(50);
            // Medium document: 500 lines
            _mediumDocument = GenerateDocument(500);
            // Large document: 2000 lines (typical large config file)
            _largeDocument = GenerateDocument(2000);
        }

        private string[] GenerateDocument(int lineCount)
        {
            var lines = new string[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                if (i % 10 == 0)
                {
                    // Registry key every 10 lines
                    lines[i] = $"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i}]\r\n";
                }
                else if (i % 10 == 1)
                {
                    // Comment line
                    lines[i] = $"; This is a comment line {i}\r\n";
                }
                else
                {
                    // Property lines with various formats
                    if (i % 3 == 0)
                    {
                        lines[i] = $"\"Property{i}\"=\"Value{i}\"\r\n";
                    }
                    else if (i % 3 == 1)
                    {
                        lines[i] = $"\"Count{i}\"=dword:{i:x8}\r\n";
                    }
                    else
                    {
                        lines[i] = $"@=\"$RootFolder$\\path\\to\\file{i}\"\r\n";
                    }
                }
            }

            return lines;
        }

        [Benchmark]
        public async Task ParseSmallDocumentAsync()
        {
            var doc = Document.FromLines(_smallDocument);
            await doc.WaitForParsingCompleteAsync();
        }

        [Benchmark]
        public async Task ParseMediumDocumentAsync()
        {
            var doc = Document.FromLines(_mediumDocument);
            await doc.WaitForParsingCompleteAsync();
        }

        [Benchmark(Baseline = true)]
        public async Task ParseLargeDocumentAsync()
        {
            var doc = Document.FromLines(_largeDocument);
            await doc.WaitForParsingCompleteAsync();
        }
    }
}