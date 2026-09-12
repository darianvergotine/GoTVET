using System.Windows;
using System.Windows.Input;

namespace GoTVET;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.ViewModel;
    }

    private void Papers_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedPaper is not null)
        {
            viewModel.DownloadCommand.Execute(viewModel.SelectedPaper);
        }
    }
}
