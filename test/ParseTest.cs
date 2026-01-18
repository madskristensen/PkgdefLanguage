using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Linq;
using System.Threading.Tasks;

namespace PkgdefLanguage.Test
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>")]
    public class ParseTest
    {
        [TestMethod]
        public async Task SingleEntry()
        {
            var lines = new[] { "[test]\r\n",
                                 "@=\"test\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);

            Entry entry = entries.First();
            Assert.HasCount(1, entry.Properties);
            Assert.AreEqual("@", entry.Properties.First().Name.Text);
            Assert.AreEqual(16, entry.Span.End);
        }

        [TestMethod]
        public async Task EqualsInPropertyValue()
        {
            var lines = new[] { "[test]\r\n",
                                 "@=\"test=1234\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);

            Entry entry = entries.First();
            Assert.AreEqual("\"test=1234\"", entry.Properties.First().Value.Text);
        }

        [TestMethod]
        public async Task MultipleEntries()
        {
            var lines = new[] { "[test]\r\n",
                         "@=\"test\"",
                         "\"test\"=\"test\"",
                         "[test]\r\n",
                         "@=\"test\""
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();


            Assert.HasCount(2, entries);

            Entry first = entries.First();
            Entry second = entries.Last();

            Assert.HasCount(2, first.Properties);
            Assert.AreEqual(29, first.Span.End);
            Assert.AreEqual("@", second.Properties.First().Name.Text);
            Assert.AreEqual(45, second.Span.End);
        }

        [TestMethod]
        public async Task Comment()
        {
            var lines = new[] { "; comment\r\n",
                                "\r\n",
                                ";comment"
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            Assert.HasCount(2, doc.Items);
            Assert.AreEqual(2, doc.Items.Where(i => i.Type == ItemType.Comment).Count());
        }

        [TestMethod]
        public async Task InvalidPropertyName()
        {
            var lines = new[] { "[test]\r\n",
                         "\"@\"=\"test\"",
            };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            ParseItem prop = entries[0].Properties[0].Name;

            Assert.IsFalse(prop.IsValid);
        }

        [TestMethod]
        public async Task VariableSpanCorrect()
        {
            var lines = new[] { "[$rootkey$]" };
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            ParseItem reference = entries[0].RegistryKey.References.FirstOrDefault();
            Assert.AreEqual("$rootkey$", reference.Text);
            Assert.AreEqual(1, reference.Span.Start);
            Assert.AreEqual(10, reference.Span.End);
        }

        [TestMethod]
        public async Task NestedRegistryKeys()
        {
            var lines = new[] {
                        "[$RootKey$\\Software\\MyCompany]\r\n",
                        "@=\"value1\"",
                        "[$RootKey$\\Software\\MyCompany\\SubKey]\r\n",
                        "@=\"value2\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(2, entries);
            Assert.AreEqual("[$RootKey$\\Software\\MyCompany]\r\n", entries[0].RegistryKey.Text);
            Assert.AreEqual("[$RootKey$\\Software\\MyCompany\\SubKey]\r\n", entries[1].RegistryKey.Text);
        }

        [TestMethod]
        public async Task MultipleVariablesInOneLine()
        {
            var lines = new[] { "[$rootkey$\\Path\\$var1$\\$var2$]" };
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            var references = entries[0].RegistryKey.References;
            Assert.HasCount(3, references);
            Assert.AreEqual("$rootkey$", references[0].Text);
            Assert.AreEqual("$var1$", references[1].Text);
            Assert.AreEqual("$var2$", references[2].Text);
        }

        [TestMethod]
        public async Task DWordValue()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"Count\"=dword:0000007b"
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(1, entries[0].Properties);
            Assert.AreEqual("dword:0000007b", entries[0].Properties[0].Value.Text);
            Assert.AreEqual(ItemType.Literal, entries[0].Properties[0].Value.Type);
        }

        [TestMethod]
        public async Task QWordValue()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"LargeNumber\"=qword:00000000ffffffff"
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(1, entries[0].Properties);
            Assert.AreEqual("qword:00000000ffffffff", entries[0].Properties[0].Value.Text);
            Assert.AreEqual(ItemType.Literal, entries[0].Properties[0].Value.Type);
        }

        [TestMethod]
        public async Task HexBinaryValue()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"Binary\"=hex:01,02,03,04,ff"
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(1, entries[0].Properties);
            Assert.AreEqual("hex:01,02,03,04,ff", entries[0].Properties[0].Value.Text);
            Assert.AreEqual(ItemType.Literal, entries[0].Properties[0].Value.Type);
        }

        [TestMethod]
        public async Task MalformedRegistryKey_NoClosingBracket()
        {
            var lines = new[] {
                        "[test\r\n",
                        "@=\"value\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            // Should still create an entry with the malformed key
            Assert.HasCount(1, entries);
            Assert.AreEqual("[test\r\n", entries[0].RegistryKey.Text);
        }

        [TestMethod]
        public async Task MalformedProperty_NoValue()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"Name\"="
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            // Property without value shouldn't be parsed as a property
            Assert.HasCount(0, entries[0].Properties);
        }

        [TestMethod]
        public async Task MalformedProperty_NoEquals()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"Name\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            // Property without equals shouldn't be parsed as a property
            Assert.HasCount(0, entries[0].Properties);
        }

        [TestMethod]
        public async Task PreprocessorDirective()
        {
            var lines = new[] {
                        "#include \"common.pkgdef\"\r\n",
                        "[test]\r\n",
                        "@=\"value\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();

            var preprocessors = doc.Items.Where(i => i.Type == ItemType.Preprocessor).ToList();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, preprocessors);
            Assert.AreEqual("#include \"common.pkgdef\"\r\n", preprocessors[0].Text);
            Assert.HasCount(1, entries);
        }

        [TestMethod]
        public async Task MultipleCommentStyles()
        {
            var lines = new[] {
                        "; Semicolon comment\r\n",
                        "// Double slash comment\r\n",
                        "[test]\r\n",
                        "@=\"value\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var comments = doc.Items.Where(i => i.Type == ItemType.Comment).ToList();

            Assert.HasCount(2, comments);
            Assert.StartsWith(";", comments[0].Text);
            Assert.StartsWith("//", comments[1].Text);
        }

        [TestMethod]
        public async Task EmptyLines()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "@=\"value\"\r\n",
                        "\r\n",
                        "\r\n",
                        "[test2]\r\n",
                        "@=\"value2\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(2, entries);
            Assert.AreEqual("@", entries[0].Properties[0].Name.Text);
            Assert.AreEqual("@", entries[1].Properties[0].Name.Text);
        }

        [TestMethod]
        public async Task VariableWithoutClosingDollar()
        {
            var lines = new[] { "[$rootkey" };
            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            // Variable without closing $ should still be detected
            var references = entries[0].RegistryKey.References;
            Assert.HasCount(1, references);
            Assert.AreEqual("$rootkey", references[0].Text);
        }

        [TestMethod]
        public async Task StringValueWithSpecialCharacters()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"Path\"=\"C:\\\\Program Files\\\\MyApp\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(1, entries[0].Properties);
            Assert.AreEqual("\"C:\\\\Program Files\\\\MyApp\"", entries[0].Properties[0].Value.Text);
        }

        [TestMethod]
        public async Task MultiplePropertiesInEntry()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "@=\"default\"\r\n",
                        "\"Name1\"=\"value1\"\r\n",
                        "\"Name2\"=dword:00000001\r\n",
                        "\"Name3\"=\"value3\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(4, entries[0].Properties);
            Assert.AreEqual("@", entries[0].Properties[0].Name.Text);
            Assert.AreEqual("\"Name1\"", entries[0].Properties[1].Name.Text);
            Assert.AreEqual("\"Name2\"", entries[0].Properties[2].Name.Text);
            Assert.AreEqual("\"Name3\"", entries[0].Properties[3].Name.Text);
        }

        [TestMethod]
        public async Task GuidInValue()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"CLSID\"=\"{12345678-1234-1234-1234-123456789012}\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(1, entries[0].Properties);
            Assert.Contains("{12345678-1234-1234-1234-123456789012}", entries[0].Properties[0].Value.Text);
        }

        [TestMethod]
        public async Task WhitespaceAroundEquals()
        {
            var lines = new[] {
                        "[test]\r\n",
                        "\"Name\"  =  \"value\""
                    };

            var doc = Document.FromLines(lines);
            await doc.WaitForParsingCompleteAsync();
            var entries = doc.Items.OfType<Entry>().ToList();

            Assert.HasCount(1, entries);
            Assert.HasCount(1, entries[0].Properties);
            Assert.AreEqual("\"Name\"", entries[0].Properties[0].Name.Text);
            Assert.AreEqual("\"value\"", entries[0].Properties[0].Value.Text);
        }

                [TestMethod]
                public async Task CaseSensitiveVariables()
                {
                    var lines = new[] {
                                "[$RootKey$\\Test]\r\n",
                                "@=\"value1\"",
                                "[$rootkey$\\Test2]\r\n",
                                "@=\"value2\""
                            };

                    var doc = Document.FromLines(lines);
                    await doc.WaitForParsingCompleteAsync();
                    var entries = doc.Items.OfType<Entry>().ToList();

                    Assert.HasCount(2, entries);
                    // Both $RootKey$ and $rootkey$ should be recognized
                    Assert.HasCount(1, entries[0].RegistryKey.References);
                    Assert.HasCount(1, entries[1].RegistryKey.References);
                }

                [TestMethod]
                public async Task IncompleteProperty_QuotedName()
                {
                    var lines = new[] {
                                "[test]\r\n",
                                "\"PropertyName\""
                            };

                    var doc = Document.FromLines(lines);
                    await doc.WaitForParsingCompleteAsync();
                    var entries = doc.Items.OfType<Entry>().ToList();

                    Assert.HasCount(1, entries);
                    // The quoted string should be parsed as a String type for colorization
                    var stringItems = doc.Items.Where(i => i.Type == ItemType.String).ToList();
                    Assert.HasCount(1, stringItems);
                    Assert.AreEqual("\"PropertyName\"", stringItems[0].Text.Trim());
                }

                [TestMethod]
                public async Task IncompleteProperty_AtSign()
                {
                    var lines = new[] {
                                "[test]\r\n",
                                "@"
                            };

                        var doc = Document.FromLines(lines);
                        await doc.WaitForParsingCompleteAsync();
                        var entries = doc.Items.OfType<Entry>().ToList();

                        Assert.HasCount(1, entries);
                        // The @ should be parsed as a Literal type for colorization
                        var literalItems = doc.Items.Where(i => i.Type == ItemType.Literal).ToList();
                        Assert.HasCount(1, literalItems);
                        Assert.AreEqual("@", literalItems[0].Text.Trim());
                    }

                    [TestMethod]
                    public async Task ValidDWordValue()
                    {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"Count\"=dword:0000007b"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsTrue(entries[0].Properties[0].Value.IsValid, "Valid dword value should not have errors");
                        }

                        [TestMethod]
                        public async Task InvalidDWordValue_TooShort()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"Count\"=dword:7b"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsFalse(entries[0].Properties[0].Value.IsValid, "Short dword value should have error");
                            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL009"));
                        }

                        [TestMethod]
                        public async Task InvalidDWordValue_InvalidChars()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"Count\"=dword:GGGGGGGG"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsFalse(entries[0].Properties[0].Value.IsValid, "Invalid dword characters should have error");
                            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL009"));
                        }

                        [TestMethod]
                        public async Task ValidQWordValue()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"LargeNumber\"=qword:00000000ffffffff"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsTrue(entries[0].Properties[0].Value.IsValid, "Valid qword value should not have errors");
                        }

                        [TestMethod]
                        public async Task InvalidQWordValue_TooLong()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"LargeNumber\"=qword:00000000ffffffff00"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsFalse(entries[0].Properties[0].Value.IsValid, "Long qword value should have error");
                            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL010"));
                        }

                        [TestMethod]
                        public async Task ValidHexArrayValue()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"Binary\"=hex:01,02,03,04,ff"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsTrue(entries[0].Properties[0].Value.IsValid, "Valid hex array should not have errors");
                        }

                        [TestMethod]
                        public async Task ValidHexWithTypeValue()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"String\"=hex(2):48,00,65,00,6c,00,6c,00,6f,00"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsTrue(entries[0].Properties[0].Value.IsValid, "Valid hex(2) value should not have errors");
                        }

                        [TestMethod]
                        public async Task InvalidHexArrayValue_BadFormat()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"Binary\"=hex:0102,03,04"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                            Assert.HasCount(1, entries);
                            Assert.IsFalse(entries[0].Properties[0].Value.IsValid, "Malformed hex array should have error");
                            Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL011"));
                        }

                        [TestMethod]
                        public async Task InvalidHexArrayValue_InvalidChars()
                        {
                            var lines = new[] {
                                        "[test]\r\n",
                                        "\"Binary\"=hex:GG,02,03"
                                    };

                            var doc = Document.FromLines(lines);
                            await doc.WaitForParsingCompleteAsync();
                            var entries = doc.Items.OfType<Entry>().ToList();

                                                    Assert.HasCount(1, entries);
                                                    Assert.IsFalse(entries[0].Properties[0].Value.IsValid, "Invalid hex characters should have error");
                                                    Assert.IsTrue(entries[0].Properties[0].Value.Errors.Any(e => e.ErrorCode == "PL011"));
                                                }

                                                [TestMethod]
                                                public async Task ForwardSlashInRegistryKey()
                                                {
                                                    var lines = new[] {
                                                                "[HKEY_LOCAL_MACHINE/Software/MyApp]\r\n",
                                                                "@=\"value\""
                                                            };

                                                    var doc = Document.FromLines(lines);
                                                    await doc.WaitForParsingCompleteAsync();
                                                    var entries = doc.Items.OfType<Entry>().ToList();

                                                    Assert.HasCount(1, entries);
                                                    Assert.IsFalse(entries[0].RegistryKey.IsValid, "Registry key with forward slashes should have error");
                                                    Assert.IsTrue(entries[0].RegistryKey.Errors.Any(e => e.ErrorCode == "PL003"));
                                                }
                                            }
                                        }
