using BenchmarkDotNet.Attributes;

using Microsoft.VSDiagnostics;

using System.Threading.Tasks;

namespace PkgdefLanguage.Benchmarks
{
    [CPUUsageDiagnoser]
    public class ValidationBenchmark
    {
        private string[] _documentWithDuplicates;
        private string[] _documentWithInvalidVariables;
        private string[] _documentWithMixedErrors;
        private string[] _validDocument;
        private string[] _documentWithValueTypeErrors;

        [GlobalSetup]
        public void Setup()
        {
            // Document with duplicate registry keys (stress HashSet)
            _documentWithDuplicates = GenerateDocumentWithDuplicates(200);
            // Document with many invalid variable references
            _documentWithInvalidVariables = GenerateDocumentWithInvalidVariables(200);
            // Document with mixed validation errors
            _documentWithMixedErrors = GenerateDocumentWithMixedErrors(200);
            // Valid document (best case scenario)
            _validDocument = GenerateValidDocument(200);
            // Document with value type errors (dword, qword, hex)
            _documentWithValueTypeErrors = GenerateDocumentWithValueTypeErrors(200);
        }

        private string[] GenerateDocumentWithDuplicates(int entryCount)
        {
            var lines = new System.Collections.Generic.List<string>();
            // Create duplicate keys intentionally
            for (int i = 0; i < entryCount; i++)
            {
                // Reuse same key name every 5 entries to trigger duplicate detection
                string keyName = $"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i / 5}]\r\n";
                lines.Add(keyName);
                lines.Add($"\"Property{i}\"=\"Value{i}\"\r\n");
            }

            return lines.ToArray();
        }

        private string[] GenerateDocumentWithInvalidVariables(int entryCount)
        {
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < entryCount; i++)
            {
                // Use invalid variables to stress validation
                lines.Add($"[$InvalidVar{i}$\\Software\\Test]\r\n");
                lines.Add($"\"Path\"=\"$BadVariable{i}$\\path\"\r\n");
                lines.Add($"@=\"$AnotherBadOne{i}$\"\r\n");
            }

            return lines.ToArray();
        }

        private string[] GenerateDocumentWithMixedErrors(int entryCount)
        {
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < entryCount; i++)
            {
                if (i % 4 == 0)
                {
                    // Unclosed registry key
                    lines.Add($"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i}\r\n");
                }
                else if (i % 4 == 1)
                {
                    // Invalid variable
                    lines.Add($"[$BadVar{i}$]\r\n");
                    lines.Add($"\"Property\"=\"value\"\r\n");
                }
                else if (i % 4 == 2)
                {
                    // Quoted @ sign
                    lines.Add($"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i}]\r\n");
                    lines.Add($"\"@\"=\"should be unquoted\"\r\n");
                }
                else
                {
                    // Valid entry
                    lines.Add($"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i}]\r\n");
                    lines.Add($"@=\"$RootFolder$\\path\"\r\n");
                }
            }

            return lines.ToArray();
        }

        private string[] GenerateValidDocument(int entryCount)
        {
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < entryCount; i++)
            {
                lines.Add($"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i}]\r\n");
                lines.Add($"@=\"$RootFolder$\\path\\file{i}\"\r\n");
                lines.Add($"\"Name{i}\"=\"Value{i}\"\r\n");
                lines.Add($"\"Count{i}\"=dword:{i:x8}\r\n");
            }

            return lines.ToArray();
        }

        private string[] GenerateDocumentWithValueTypeErrors(int entryCount)
        {
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < entryCount; i++)
            {
                lines.Add($"[HKEY_LOCAL_MACHINE\\Software\\Test\\Key{i}]\r\n");

                if (i % 3 == 0)
                {
                    // Invalid dword (too short)
                    lines.Add($"\"Count\"=dword:7b\r\n");
                }
                else if (i % 3 == 1)
                {
                    // Invalid qword (invalid characters)
                    lines.Add($"\"LargeNum\"=qword:GGGGGGGGGGGGGGGG\r\n");
                }
                else
                {
                    // Invalid hex array (bad format)
                    lines.Add($"\"Binary\"=hex:0102,03,04\r\n");
                }
            }

            return lines.ToArray();
        }

        [Benchmark]
        public async Task ValidateDocumentWithDuplicatesAsync()
        {
            var doc = Document.FromLines(_documentWithDuplicates);
            await doc.WaitForParsingCompleteAsync();
        }

        [Benchmark]
        public async Task ValidateDocumentWithInvalidVariablesAsync()
        {
            var doc = Document.FromLines(_documentWithInvalidVariables);
            await doc.WaitForParsingCompleteAsync();
        }

        [Benchmark]
        public async Task ValidateDocumentWithMixedErrorsAsync()
        {
            var doc = Document.FromLines(_documentWithMixedErrors);
            await doc.WaitForParsingCompleteAsync();
        }

        [Benchmark(Baseline = true)]
        public async Task ValidateValidDocumentAsync()
        {
            var doc = Document.FromLines(_validDocument);
            await doc.WaitForParsingCompleteAsync();
        }

        [Benchmark]
        public async Task ValidateDocumentWithValueTypeErrorsAsync()
        {
            var doc = Document.FromLines(_documentWithValueTypeErrors);
            await doc.WaitForParsingCompleteAsync();
        }
    }
}