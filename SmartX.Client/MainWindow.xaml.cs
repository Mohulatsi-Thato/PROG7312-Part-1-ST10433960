using System.Windows;
using System.Windows.Input;
using SmartX.Client.Views;

namespace SmartX.Client;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnIngestionTileClick(object sender, MouseButtonEventArgs e)
    {
        var ingestionWindow = new IngestionWindow();
        ingestionWindow.Show();
        Close();
    }
}
