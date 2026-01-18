using BenchmarkDotNet.Attributes;

using Microsoft.VSDiagnostics;

using System.Threading;
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

        public static void WaitForParsingComplete(this Document document)
        {
            // Spin wait for more accurate timing (no Task.Delay overhead)
            var spinWait = new SpinWait();
            while (document.IsProcessing)
            {
                spinWait.SpinOnce();
            }
        }
    }

    /// <summary>
    /// Test document that exposes synchronous parsing for accurate benchmarking.
    /// </summary>
    public class BenchmarkDocument : Document
    {
        private BenchmarkDocument(string[] lines) : base(lines)
        {
        }

        /// <summary>
        /// Creates a document and waits for initial async processing to complete.
        /// </summary>
        public static BenchmarkDocument Create(string[] lines)
        {
            var doc = new BenchmarkDocument(lines);
            // Wait for the initial async parse triggered by constructor
            doc.WaitForParsingComplete();
            return doc;
        }

        /// <summary>
        /// Synchronously re-parses the document. Call UpdateLines first to change content.
        /// </summary>
        public void ParseSync()
        {
            Parse();
            ValidateDocument();
        }
    }

    [CPUUsageDiagnoser]
    [MemoryDiagnoser]
    public class ParsingBenchmark
    {
        private string[] _smallDocument;
        private string[] _mediumDocument;
        private string[] _largeDocument;

        // Pre-created documents for synchronous parsing benchmarks
        private BenchmarkDocument _smallDoc;
        private BenchmarkDocument _mediumDoc;
        private BenchmarkDocument _largeDoc;

        [GlobalSetup]
        public void Setup()
        {
            // Small document: 50 lines
            _smallDocument = GenerateDocument(50);
            // Medium document: 500 lines
            _mediumDocument = GenerateDocument(500);
            // Large document: 2000 lines (typical large config file)
            _largeDocument = GenerateDocument(2000);

            // Pre-create documents for sync benchmarks
            _smallDoc = BenchmarkDocument.Create(_smallDocument);
            _mediumDoc = BenchmarkDocument.Create(_mediumDocument);
            _largeDoc = BenchmarkDocument.Create(_largeDocument);
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
        public void ParseSmallDocument()
        {
            _smallDoc.UpdateLines(_smallDocument);
            _smallDoc.ParseSync();
        }

        [Benchmark]
        public void ParseMediumDocument()
        {
            _mediumDoc.UpdateLines(_mediumDocument);
            _mediumDoc.ParseSync();
        }

        [Benchmark(Baseline = true)]
        public void ParseLargeDocument()
        {
            _largeDoc.UpdateLines(_largeDocument);
            _largeDoc.ParseSync();
        }
    }
}