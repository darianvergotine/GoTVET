using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinForms = System.Windows.Forms;

namespace GoTVET;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.ViewModel;
        Loaded += (_, _) =>
        {
            BuildPresetSwatches();
            RefreshThemeSwatch();
        };
        ThemeService.Changed += (_, _) => Dispatcher.Invoke(RefreshThemeSwatch);
    }

    private void Papers_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedPaper is not null)
        {
            viewModel.DownloadCommand.Execute(viewModel.SelectedPaper);
        }
    }

    private void ThemeButton_OnClick(object sender, RoutedEventArgs e) =>
        ThemePopup.IsOpen = true;

    private void CustomTheme_OnClick(object sender, RoutedEventArgs e)
    {
        var current = ThemeService.CurrentPrimary;
        using var dialog = new WinForms.ColorDialog
        {
            AllowFullOpen = true,
            FullOpen = true,
            AnyColor = true,
            Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B)
        };

        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            var chosen = dialog.Color;
            ThemeService.Apply(Color.FromRgb(chosen.R, chosen.G, chosen.B));
            ThemePopup.IsOpen = false;
        }
    }

    private void ResetTheme_OnClick(object sender, RoutedEventArgs e)
    {
        ThemeService.Reset();
        ThemePopup.IsOpen = false;
    }

    private void BuildPresetSwatches()
    {
        ThemePresets.Children.Clear();
        foreach (var hex in ThemeService.Presets)
        {
            var color = ThemeService.Parse(hex);
            var swatch = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(color),
                BorderBrush = (Brush)FindResource("Line"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand,
                ToolTip = hex,
                Tag = hex
            };
            swatch.MouseLeftButtonUp += (_, _) =>
            {
                ThemeService.ApplyHex(hex);
                ThemePopup.IsOpen = false;
            };
            ThemePresets.Children.Add(swatch);
        }
    }

    private void RefreshThemeSwatch() =>
        ThemeSwatch.Background = new SolidColorBrush(ThemeService.CurrentPrimary);
}
