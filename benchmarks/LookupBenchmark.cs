using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System.Threading.Tasks;

namespace PkgdefLanguage.Benchmarks
{
    [CPUUsageDiagnoser]
    public class LookupBenchmark
    {
        private string[] GenerateDocument(int lineCount)
        {
            var lines = new string[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                if (i % 10 == 0)
                {
                    lines[i] = $"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i}]\r\n";
                }
                else if (i % 10 == 1)
                {
                    lines[i] = $"; Comment line {i}\r\n";
                }
                else
                {
                    if (i % 3 == 0)
                    {
                        lines[i] = $"\"Property{i}\"=\"$RootFolder$\\Value{i}\"\r\n";
                    }
                    else if (i % 3 == 1)
                    {
                        lines[i] = $"\"Count{i}\"=dword:{i:x8}\r\n";
                    }
                    else
                    {
                        lines[i] = $"@=\"DefaultValue{i}\"\r\n";
                    }
                }
            }

            return lines;
        }

        [Benchmark]
        public async Task FindItemInSmallDocumentAsync()
        {
            var lines = GenerateDocument(100);
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            // Perform lookups at beginning, middle, and end
            if (doc.Items.Count > 0)
            {
                var firstItem = doc.Items[0];
                var midItem = doc.Items[doc.Items.Count / 2];
                var lastItem = doc.Items[doc.Items.Count - 1];
                doc.FindItemFromPosition(firstItem.Span.Start);
                doc.FindItemFromPosition(midItem.Span.Start + midItem.Span.Length / 2);
                doc.FindItemFromPosition(lastItem.Span.End - 1);
            }
        }

        [Benchmark]
        public async Task FindItemInMediumDocumentAsync()
        {
            var lines = GenerateDocument(500);
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            // Perform lookups at beginning, middle, and end
            if (doc.Items.Count > 0)
            {
                var firstItem = doc.Items[0];
                var midItem = doc.Items[doc.Items.Count / 2];
                var lastItem = doc.Items[doc.Items.Count - 1];
                doc.FindItemFromPosition(firstItem.Span.Start);
                doc.FindItemFromPosition(midItem.Span.Start + midItem.Span.Length / 2);
                doc.FindItemFromPosition(lastItem.Span.End - 1);
            }
        }

        [Benchmark(Baseline = true)]
        public async Task FindItemInLargeDocumentAsync()
        {
            var lines = GenerateDocument(2000);
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            // Perform lookups at beginning, middle, and end
            if (doc.Items.Count > 0)
            {
                var firstItem = doc.Items[0];
                var midItem = doc.Items[doc.Items.Count / 2];
                var lastItem = doc.Items[doc.Items.Count - 1];
                doc.FindItemFromPosition(firstItem.Span.Start);
                doc.FindItemFromPosition(midItem.Span.Start + midItem.Span.Length / 2);
                doc.FindItemFromPosition(lastItem.Span.End - 1);
            }
        }

        [Benchmark]
        public async Task FindItemWithReferencesAsync()
        {
            var lines = GenerateDocument(1000);
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            // Find items that contain variable references (more complex lookup)
            int lookupCount = 0;
            for (int i = 0; i < doc.Items.Count && lookupCount < 50; i++)
            {
                var item = doc.Items[i];
                if (item.References.Count > 0)
                {
                    // Search within a reference
                    var reference = item.References[0];
                    doc.FindItemFromPosition(reference.Span.Start + 1);
                    lookupCount++;
                }
            }
        }
    }
}