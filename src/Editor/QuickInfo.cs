using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PkgdefLanguage
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name(Constants.LanguageName + " QuickInfo Provider")]
    [ContentType(Constants.LanguageName)]
    internal sealed class QuickInfoProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new QuickInfoSource(textBuffer));
        }
    }

    internal sealed class QuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _textBuffer;
        private static readonly ImageId _variableIcon = KnownMonikers.LocalVariable.ToImageId();
        private static readonly ImageId _registryKeyIcon = KnownMonikers.Registry.ToImageId();
        private static readonly Regex _regexRef = new(@"\$[\w]+\$?", RegexOptions.Compiled);

        public QuickInfoSource(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);

            if (!triggerPoint.HasValue)
            {
                return Task.FromResult<QuickInfoItem>(null);
            }

            Document document = _textBuffer.GetDocument();
            if (document == null)
            {
                return Task.FromResult<QuickInfoItem>(null);
            }

            ParseItem item = document.FindItemFromPosition(triggerPoint.Value.Position);

            if (item == null)
            {
                return Task.FromResult<QuickInfoItem>(null);
            }

            // Handle variable references
            if (item.Type == ItemType.Reference)
            {
                QuickInfoItem variableQuickInfo = CreateVariableQuickInfo(item, triggerPoint.Value);
                if (variableQuickInfo != null)
                {
                    return Task.FromResult(variableQuickInfo);
                }
            }
        }

            return Task.FromResult<QuickInfoItem>(null);
        }

        private QuickInfoItem CreateVariableQuickInfo(ParseItem item, SnapshotPoint triggerPoint)
        {
            // Extract the variable name (remove $)
            string variableName = item.Text.Trim('$');

            if (PredefinedVariables.Variables.TryGetValue(variableName, out string description))
            {
                // Create a clean, elegant tooltip with minimal colors
                var textRuns = new List<ClassifiedTextRun>
                {
                    // Variable name in emphasized text (not overly colorful)
                    new ClassifiedTextRun("text", variableName, ClassifiedTextRunStyle.Bold),
                    new ClassifiedTextRun("text", Environment.NewLine + Environment.NewLine),
                    // Description in plain text for readability
                    new ClassifiedTextRun("text", description)
                };

                var textElement = new ClassifiedTextElement(textRuns);

                var containerElement = new ContainerElement(
                    ContainerElementStyle.Stacked,
                    textElement);

                ITrackingSpan applicableToSpan = triggerPoint.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeInclusive);

                return new QuickInfoItem(applicableToSpan, containerElement);
            }

            return null;
        }

        private string ResolveVariablesInText(string text)
        {
            string resolved = text;

            // Find all variable references and replace them with their values or descriptions
            foreach (Match match in _regexRef.Matches(text))
            {
                string variableName = match.Value.Trim('$');

                if (PredefinedVariables.Variables.TryGetValue(variableName, out string description))
                {
                    // For the tooltip, we'll show a placeholder instead of the actual value
                    // since we don't have runtime values, just descriptions
                    resolved = resolved.Replace(match.Value, $"[{variableName}]");
                }
            }

            return resolved;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
