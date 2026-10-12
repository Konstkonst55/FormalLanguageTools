using System.Windows;
using System.Windows.Controls;
using FormalLanguageTools.ArithmeticParser.DataClasses;

namespace FormalLanguageTools.App.Views;

public partial class SyntaxTreeWindow : Window
{
    public SyntaxTreeWindow(SyntaxNode root, string expression, int lineNumber)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(expression);
        InitializeComponent();
        Title = $"Дерево разбора - строка {lineNumber}";
        ExpressionDescription.Text = $"Строка {lineNumber}: {expression}";
        SyntaxTree.Items.Add(CreateTreeItem(root));
    }

    private static TreeViewItem CreateTreeItem(SyntaxNode node)
    {
        var item = new TreeViewItem
        {
            Header = node.DisplayText,
            IsExpanded = true,
            Tag = node
        };

        foreach (var child in node.Children)
        {
            item.Items.Add(CreateTreeItem(child));
        }

        return item;
    }
}
