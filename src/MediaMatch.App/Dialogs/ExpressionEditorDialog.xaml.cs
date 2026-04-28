using MediaMatch.App.ViewModels;
using MediaMatch.Core.Expressions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MediaMatch.App.Dialogs;

/// <summary>
/// ContentDialog for editing expression format strings with live preview and token insertion.
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

            // Return focus to the expression TextBox
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

    // x:Bind helpers — return validation indicator glyph and brush
    public string ValidationGlyph(bool isValid) =>
        isValid ? "\uE73E" : "\uE711";

    public SolidColorBrush ValidationBrush(bool isValid) =>
        new(isValid ? Colors.Green : Colors.Red);
}
