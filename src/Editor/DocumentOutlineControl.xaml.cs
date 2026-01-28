using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PkgdefLanguage
{
    /// <summary>
    /// WPF UserControl for displaying the document outline in the Document Outline tool window.
    /// Shows a hierarchical tree of registry keys for navigation in pkgdef files.
    /// </summary>
    public partial class DocumentOutlineControl : UserControl
    {
        private Document _document;
        private IWpfTextView _textView;
        private IVsTextView _vsTextView;
        private bool _isNavigating;

        public ObservableCollection<OutlineItem> Items { get; } = new ObservableCollection<OutlineItem>();

        public DocumentOutlineControl()
        {
            InitializeComponent();
            OutlineTreeView.ItemsSource = Items;
        }

        /// <summary>
        /// Initializes the control with the document and text view for the pkgdef file.
        /// </summary>
        public void Initialize(Document document, IWpfTextView textView, IVsTextView vsTextView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Unsubscribe from previous document if any
            if (_document != null)
            {
                _document.Processed -= OnDocumentProcessed;
            }

            _document = document;
            _textView = textView;
            _vsTextView = vsTextView;

            if (_document != null)
            {
                _document.Processed += OnDocumentProcessed;

                // Subscribe to caret position changes for sync
                if (_textView != null)
                {
                    _textView.Caret.PositionChanged += OnCaretPositionChanged;
                }

                // Initial population
                RefreshItems();
            }
        }

        /// <summary>
        /// Cleans up event subscriptions when the control is disposed.
        /// </summary>
        public void Cleanup()
        {
            if (_document != null)
            {
                _document.Processed -= OnDocumentProcessed;
            }

            if (_textView != null)
            {
                _textView.Caret.PositionChanged -= OnCaretPositionChanged;
            }

            _document = null;
            _textView = null;
            _vsTextView = null;
            Items.Clear();
        }

        private void OnDocumentProcessed(Document document)
        {
#pragma warning disable VSTHRD110 // Observe result of async calls
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                RefreshItems();
            });
#pragma warning restore VSTHRD110
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (_isNavigating || _document == null)
            {
                return;
            }

            // Find and select the item that contains the current caret position
            int caretPosition = e.NewPosition.BufferPosition.Position;
            SelectItemAtPosition(caretPosition);
        }

        private void SelectItemAtPosition(int position)
        {
            // Find the closest item at or before the caret position
            OutlineItem bestMatch = FindItemAtPosition(Items, position);

            if (bestMatch != null && OutlineTreeView.SelectedItem != bestMatch)
            {
                _isNavigating = true;
                try
                {
                    SelectTreeViewItem(OutlineTreeView, bestMatch);
                }
                finally
                {
                    _isNavigating = false;
                }
            }
        }

        private OutlineItem FindItemAtPosition(IEnumerable<OutlineItem> items, int position)
        {
            OutlineItem result = null;

            foreach (OutlineItem item in items)
            {
                if (item.SpanStart <= position && item.SpanEnd >= position)
                {
                    result = item;

                    // Check children for a more specific match
                    OutlineItem childMatch = FindItemAtPosition(item.Children, position);
                    if (childMatch != null)
                    {
                        result = childMatch;
                    }
                    break;
                }
                else if (item.SpanStart <= position)
                {
                    result = item;
                }
            }

            return result;
        }

        private void RefreshItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Items.Clear();

            if (_document?.Items == null || _document.Items.Count == 0)
            {
                EmptyMessage.Visibility = Visibility.Visible;
                return;
            }

            // Get all Entry items (registry keys) from the document
            List<Entry> entries = _document.Items
                .OfType<Entry>()
                .ToList();

            if (entries.Count == 0)
            {
                EmptyMessage.Visibility = Visibility.Visible;
                return;
            }

            EmptyMessage.Visibility = Visibility.Collapsed;

            // Build the outline items from entries
            BuildOutlineItems(entries);

            // Expand all items
            ExpandAllTreeViewItems();
        }

        private void BuildOutlineItems(List<Entry> entries)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Entry entry in entries)
            {
                string text = GetRegistryKeyDisplayText(entry);
                int lineNumber = GetLineNumber(entry.Span.Start);

                OutlineItem item = new()
                {
                    Text = text,
                    LineNumber = lineNumber,
                    SpanStart = entry.Span.Start,
                    SpanEnd = entry.Span.End,
                    FontWeight = FontWeights.Normal
                };

                Items.Add(item);
            }
        }

        private string GetRegistryKeyDisplayText(Entry entry)
        {
            if (entry?.RegistryKey?.Text == null)
            {
                return string.Empty;
            }

            string text = entry.RegistryKey.Text.Trim();

            // Remove the brackets from the registry key
            if (text.StartsWith("[") && text.EndsWith("]"))
            {
                text = text.Substring(1, text.Length - 2);
            }
            else if (text.StartsWith("[-") && text.EndsWith("]"))
            {
                // Handle deletion entries [-HKEY_...]
                text = text.Substring(2, text.Length - 3) + " (delete)";
            }

            return text;
        }

        private int GetLineNumber(int position)
        {
            if (_vsTextView == null)
            {
                return 0;
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            _vsTextView.GetLineAndColumn(position, out int line, out int _);
            return line;
        }

        private void NavigateToItem(OutlineItem item)
        {
            if (item == null || _vsTextView == null)
            {
                return;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            _isNavigating = true;
            try
            {
                // Navigate to the item's line
                _vsTextView.SetCaretPos(item.LineNumber, 0);
                _vsTextView.CenterLines(item.LineNumber, 1);

                // Ensure the editor has focus
                _vsTextView.SendExplicitFocus();
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void OutlineTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (OutlineTreeView.SelectedItem is OutlineItem item)
            {
                NavigateToItem(item);
            }
        }

        private void OutlineTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (e.Key == Key.Enter && OutlineTreeView.SelectedItem is OutlineItem item)
            {
                NavigateToItem(item);
                e.Handled = true;
            }
        }

        private void OutlineTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Single-click navigation (optional - can be removed if double-click only is preferred)
            // Uncomment the following to enable single-click navigation:
            // if (!_isNavigating && e.NewValue is OutlineItem item)
            // {
            //     NavigateToItem(item);
            // }
        }

        private void ExpandAllTreeViewItems()
        {
            foreach (OutlineItem item in Items)
            {
                ExpandTreeViewItem(OutlineTreeView, item);
            }
        }

        private void ExpandTreeViewItem(ItemsControl container, OutlineItem item)
        {
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsExpanded = true;

                foreach (OutlineItem child in item.Children)
                {
                    ExpandTreeViewItem(treeViewItem, child);
                }
            }
        }

        private void SelectTreeViewItem(ItemsControl container, OutlineItem item)
        {
            // First, try to find the item directly
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsSelected = true;
                treeViewItem.BringIntoView();
                return;
            }

            // Search through all items recursively
            foreach (object containerItem in container.Items)
            {
                if (container.ItemContainerGenerator.ContainerFromItem(containerItem) is TreeViewItem childContainer)
                {
                    if (containerItem == item)
                    {
                        childContainer.IsSelected = true;
                        childContainer.BringIntoView();
                        return;
                    }

                    SelectTreeViewItem(childContainer, item);
                }
            }
        }
    }

    /// <summary>
    /// Represents an item in the document outline tree.
    /// </summary>
    public class OutlineItem
    {
        public string Text { get; set; }
        public int LineNumber { get; set; }
        public int SpanStart { get; set; }
        public int SpanEnd { get; set; }
        public FontWeight FontWeight { get; set; }
        public ObservableCollection<OutlineItem> Children { get; } = new ObservableCollection<OutlineItem>();
    }
}
