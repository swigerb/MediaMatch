using MediaMatch.App.Linux.ViewModels;
using MediaMatch.Core.Expressions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MediaMatch.App.Linux.Dialogs;

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

        ViewModel.PropertyChanged += (_, e) => UpdateValidationUI();
        UpdateValidationUI();
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

    private void UpdateValidationUI()
    {
        var isValid = ViewModel.IsValid;
        ValidationIcon.Glyph = isValid ? "\uE73E" : "\uE711";
        var brush = new SolidColorBrush(isValid ? Colors.Green : Colors.Red);
        ValidationIcon.Foreground = brush;
        ValidationText.Foreground = brush;
        ValidationText.Text = ViewModel.ValidationMessage;
        PreviewText.Text = ViewModel.Preview;
    }
}
