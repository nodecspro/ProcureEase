#region

using System.Windows;

#endregion

namespace ProcureEase;

/// <summary>
///     Логика взаимодействия для MainForm.xaml
/// </summary>
public partial class MainForm : Window
{
    public MainForm()
    {
        InitializeComponent();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}