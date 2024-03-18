#region

using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;

#endregion

namespace ProcureEase;

public partial class Main : MetroWindow
{
    public Main()
    {
        InitializeComponent();
        ThemeManager.Current.ChangeTheme(this, "Dark.Purple");
    }

    private void CreateRequest_Click(object sender, RoutedEventArgs e)
    {
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}