using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMatch.App.macOS.Controls;

public sealed partial class SfvPanel : UserControl
{
    public object? ViewModel
    {
        get => DataContext;
        set => DataContext = value;
    }

    public SfvPanel()
    {
        InitializeComponent();
    }

    private void AlgorithmRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is null) return;

        var index = sender switch
        {
            RadioButton rb when rb == CRC32Radio => 0,
            RadioButton rb when rb == MD5Radio => 1,
            RadioButton rb when rb == SHA1Radio => 2,
            RadioButton rb when rb == SHA256Radio => 3,
            _ => 0
        };

        // Use reflection to set SelectedAlgorithmIndex on the ViewModel
        var prop = DataContext.GetType().GetProperty("SelectedAlgorithmIndex");
        prop?.SetValue(DataContext, index);
    }
}
