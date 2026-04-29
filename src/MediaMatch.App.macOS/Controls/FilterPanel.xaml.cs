using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.macOS.Controls;

public sealed partial class FilterPanel : UserControl
{
    public object? ViewModel
    {
        get => DataContext;
        set => DataContext = value;
    }

    public FilterPanel()
    {
        InitializeComponent();
    }

    private void InfoTab_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is null) return;

        var index = sender switch
        {
            RadioButton rb when rb == MediaInfoTab => 0,
            RadioButton rb when rb == AttributesTab => 1,
            RadioButton rb when rb == TagsTab => 2,
            _ => 0
        };

        var prop = DataContext.GetType().GetProperty("SelectedTabIndex");
        prop?.SetValue(DataContext, index);
    }
}
