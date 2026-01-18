using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
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
        private static readonly ImageId _errorIcon = KnownMonikers.StatusError.ToImageId();
        private static readonly ImageId _warningIcon = KnownMonikers.StatusWarning.ToImageId();
        private static readonly ImageId _infoIcon = KnownMonikers.StatusInformation.ToImageId();

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

                        // Handle variable references first (highest priority)
                        if (item.Type == ItemType.Reference)
                        {
                            QuickInfoItem variableQuickInfo = CreateVariableQuickInfo(item, triggerPoint.Value);
                            if (variableQuickInfo != null)
                            {
                                return Task.FromResult(variableQuickInfo);
                            }
                        }

                        // Handle validation errors
                        if (item.Errors.Count > 0)
                        {
                            QuickInfoItem errorQuickInfo = CreateErrorQuickInfo(item, triggerPoint.Value);
                            if (errorQuickInfo != null)
                            {
                                return Task.FromResult(errorQuickInfo);
                            }
                        }

                        return Task.FromResult<QuickInfoItem>(null);
                    }

        private QuickInfoItem CreateErrorQuickInfo(ParseItem item, SnapshotPoint triggerPoint)
        {
            if (item.Errors.Count == 0)
            {
                return null;
            }

            // Get the most severe error to determine the icon
            var mostSevereError = item.Errors
                .OrderBy(e => e.Severity)
                .First();

            ImageId icon = mostSevereError.Severity switch
            {
                __VSERRORCATEGORY.EC_ERROR => _errorIcon,
                __VSERRORCATEGORY.EC_WARNING => _warningIcon,
                _ => _infoIcon
            };

            // Build the error message(s)
            var textRuns = new List<ClassifiedTextRun>();

            for (int i = 0; i < item.Errors.Count; i++)
            {
                var error = item.Errors.ElementAt(i);

                if (i > 0)
                {
                    textRuns.Add(new ClassifiedTextRun("text", Environment.NewLine + Environment.NewLine));
                }

                // Error message in plain text (without error code)
                textRuns.Add(new ClassifiedTextRun("text", error.Message));

                // Blank line
                textRuns.Add(new ClassifiedTextRun("text", Environment.NewLine + Environment.NewLine));

                // Error code on its own line, styled to indicate it's a link
                textRuns.Add(new ClassifiedTextRun("text", error.ErrorCode, () =>
                {
                    Process.Start(new ProcessStartInfo(error.HelpLink) { UseShellExecute = true });
                }));
            }

            var textElement = new ClassifiedTextElement(textRuns);

            var containerElement = new ContainerElement(
                ContainerElementStyle.Wrapped,
                new ImageElement(icon),
                textElement);

            ITrackingSpan applicableToSpan = triggerPoint.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeInclusive);

            return new QuickInfoItem(applicableToSpan, containerElement);
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
                    new(PredefinedClassificationTypeNames.Text, variableName, ClassifiedTextRunStyle.Bold),
                    new(PredefinedClassificationTypeNames.Text, Environment.NewLine + Environment.NewLine),
                    // Description in plain text for readability
                    new(PredefinedClassificationTypeNames.Text, description)
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

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
