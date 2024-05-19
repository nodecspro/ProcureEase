#region

using System.Windows;
using MahApps.Metro.Controls;

#endregion

namespace ProcureEase;

public partial class AddInvitationCodeWindow : MetroWindow
{
    public AddInvitationCodeWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}