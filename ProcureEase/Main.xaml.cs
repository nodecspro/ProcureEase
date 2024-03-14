#region

using System.Windows;

#endregion

namespace ProcureEase;

/// <summary>
///     Логика взаимодействия для Main.xaml
/// </summary>
public partial class Main : Window
{
    public Main()
    {
        InitializeComponent();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}