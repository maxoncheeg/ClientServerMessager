using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientServerMessager.Server.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        textBox.SelectionStart = textBox.Text.Length;
        textBox.ScrollToEnd();
    }
}