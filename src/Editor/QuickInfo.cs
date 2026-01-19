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

                // Handle variable references - show both errors AND variable info if applicable
                if (item.Type == ItemType.Reference)
                {
                    QuickInfoItem referenceQuickInfo = CreateReferenceQuickInfo(item, triggerPoint.Value);
                    if (referenceQuickInfo != null)
                    {
                        return Task.FromResult(referenceQuickInfo);
                    }
                }

                // Handle validation errors for non-reference items
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

            var textRuns = new List<ClassifiedTextRun>();

            for (int i = 0; i < item.Errors.Count; i++)
            {
                var error = item.Errors.ElementAt(i);

                if (i > 0)
                {
                    textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, Environment.NewLine + Environment.NewLine));
                }

                // Prefix based on severity
                string prefix = error.Severity switch
                {
                    __VSERRORCATEGORY.EC_ERROR => "Error: ",
                    __VSERRORCATEGORY.EC_WARNING => "Warning: ",
                    _ => "Info: "
                };

                textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, prefix, ClassifiedTextRunStyle.Bold));
                textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, error.Message));
                textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, Environment.NewLine + Environment.NewLine));
                textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, error.ErrorCode, () =>
                {
                    Process.Start(new ProcessStartInfo(error.HelpLink) { UseShellExecute = true });
                }));
            }

            var containerElement = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(textRuns));

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

        private QuickInfoItem CreateReferenceQuickInfo(ParseItem item, SnapshotPoint triggerPoint)
        {
            // Extract the variable name (remove $)
            string variableName = item.Text.Trim('$');
            bool hasVariableInfo = PredefinedVariables.Variables.TryGetValue(variableName, out string description);
            bool hasErrors = item.Errors.Count > 0;

            if (!hasVariableInfo && !hasErrors)
            {
                return null;
            }

            var textRuns = new List<ClassifiedTextRun>();

            // Add variable information first (if known variable)
            if (hasVariableInfo)
            {
                textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, variableName, ClassifiedTextRunStyle.Bold));
                textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, Environment.NewLine + Environment.NewLine));
                textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, description));
            }

            // Add error information below (if any)
            if (hasErrors)
            {
                // Add separator if we have variable info above
                if (hasVariableInfo)
                {
                    textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, Environment.NewLine + Environment.NewLine));
                }

                for (int i = 0; i < item.Errors.Count; i++)
                {
                    var error = item.Errors.ElementAt(i);

                    if (i > 0)
                    {
                        textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, Environment.NewLine + Environment.NewLine));
                    }

                    // Prefix based on severity
                    string prefix = error.Severity switch
                    {
                        __VSERRORCATEGORY.EC_ERROR => "Error: ",
                        __VSERRORCATEGORY.EC_WARNING => "Warning: ",
                        _ => "Info: "
                    };

                    textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, prefix, ClassifiedTextRunStyle.Bold));
                    textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, error.Message));
                    textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, Environment.NewLine + Environment.NewLine));
                    textRuns.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, error.ErrorCode, () =>
                    {
                        Process.Start(new ProcessStartInfo(error.HelpLink) { UseShellExecute = true });
                    }));
                }
            }

            var containerElement = new ContainerElement(
                ContainerElementStyle.Stacked,
                new ClassifiedTextElement(textRuns));

            ITrackingSpan applicableToSpan = triggerPoint.Snapshot.CreateTrackingSpan(item.Span, SpanTrackingMode.EdgeInclusive);

            return new QuickInfoItem(applicableToSpan, containerElement);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
