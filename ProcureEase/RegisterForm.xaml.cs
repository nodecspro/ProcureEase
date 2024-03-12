#region

using System.Windows;

#endregion

namespace ProcureEase;

/// <summary>
///     Логика взаимодействия для RegisterForm.xaml
/// </summary>
public partial class RegisterForm : Window
{
    public RegisterForm()
    {
        InitializeComponent();
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }

    private void BtnRegister_Click(object sender, RoutedEventArgs e)
    {

    }
}