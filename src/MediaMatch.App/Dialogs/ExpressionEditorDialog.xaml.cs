using MediaMatch.App.ViewModels;
using MediaMatch.Core.Expressions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MediaMatch.App.Dialogs;

/// <summary>
/// ContentDialog for editing expression format strings with live preview, clickable
/// examples, category selection, and token insertion (FileBot-style).
/// </summary>
public sealed partial class ExpressionEditorDialog : ContentDialog
{
    public ExpressionEditorViewModel ViewModel { get; }

    /// <summary>The resulting expression after the dialog is closed via Apply.</summary>
    public string Expression => ViewModel.Expression;

    public ExpressionEditorDialog(IExpressionEngine expressionEngine, string? initialExpression = null)
    {
        ViewModel = new ExpressionEditorViewModel(expressionEngine);
        InitializeComponent();

        if (!string.IsNullOrEmpty(initialExpression))
            ViewModel.Expression = initialExpression;
    }

    private void TokenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string token)
        {
            ViewModel.InsertTokenCommand.Execute(token);

            ExpressionTextBox.Focus(FocusState.Programmatic);
            ExpressionTextBox.SelectionStart = ViewModel.CursorPosition;
            ExpressionTextBox.SelectionLength = 0;
        }
    }

    private void ExpressionTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            ViewModel.CursorPosition = textBox.SelectionStart;
    }

    private void ExamplesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView list && list.SelectedItem is ExpressionExample example)
        {
            ViewModel.Expression = example.Expression;
            // Allow the same example to be re-clicked later.
            list.SelectedItem = null;
        }
    }

    private void CategoryRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string category)
            ViewModel.SelectedCategory = category;
    }

    private void TokenFilterRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string filter)
            ViewModel.SelectedTokenFilter = filter;
    }

    // x:Bind helpers — return validation indicator glyph and brush
    public string ValidationGlyph(bool isValid) =>
        isValid ? "\uE73E" : "\uE711";

    public SolidColorBrush ValidationBrush(bool isValid) =>
        new(isValid ? Colors.Green : Colors.Red);
}
