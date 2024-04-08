#region

using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;
using Xceed.Wpf.AvalonDock.Controls;

#endregion

namespace ProcureEase;

public partial class Main : MetroWindow
{
    private static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;

    private User currentUser;
    private bool isEditing;

    public Main(string login)
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();
        UsernameTextBlock.Text = GetUserByUsername(login);
    }

    // Use prepared statements for better performance and security
    private static string GetUserByUsername(string username)
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        const string query = "SELECT username FROM users WHERE username = @username";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", username);
        return command.ExecuteScalar()?.ToString() ?? string.Empty;
    }

    // Load user data and bind to DataGrid
    private void LoadUserData()
    {
        currentUser = LoadUserData(UsernameTextBlock.Text);
        UserDataGrid.DataContext = currentUser;
    }

    // Load user data using prepared statements
    private static User LoadUserData(string username)
    {
        if (string.IsNullOrEmpty(username)) return null;

        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        const string query = "SELECT * FROM users WHERE username = @username";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", username);

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new User
            {
                Username = reader.GetString("username"),
                Email = reader.GetString("email"),
                FirstName = reader.GetString("first_name"),
                LastName = reader.GetString("last_name"),
                Patronymic = reader.IsDBNull(reader.GetOrdinal("patronymic")) ? null : reader.GetString("patronymic"),
                PhoneNumber = reader.GetString("phone_number")
            }
            : null;
    }

    // Toggle visibility between RequestsGrid and UserDataGrid
    private void UsernameTextBlock_Click(object sender, RoutedEventArgs e)
    {
        RequestsGrid.Visibility =
            RequestsGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        UserDataGrid.Visibility =
            UserDataGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (UserDataGrid.Visibility == Visibility.Visible) LoadUserData();
    }

    private void CreateRequest_Click(object sender, RoutedEventArgs e)
    {
        // Handler for request creation button (not implemented)
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        // Close all windows when main window is closed
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }

    // Enable/disable editing of user data
    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        isEditing = !isEditing;
        foreach (var textBox in UserDataGrid.FindVisualChildren<TextBox>())
        {
            textBox.IsReadOnly = !isEditing;
            textBox.BorderBrush = isEditing
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFABADB3"))
                : Brushes.Transparent;
            textBox.BorderThickness = isEditing ? new Thickness(1) : new Thickness(0);
        }

        EditButton.Content = isEditing ? "Сохранить" : "Изменить";
        EditButton.Background = isEditing ? Brushes.Green : Brushes.Transparent;
        CancelButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
    }

    // Cancel editing and reset user data
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset TextBox values (not implemented, requires reloading data)
        isEditing = false;
        foreach (var textBox in UserDataGrid.FindVisualChildren<TextBox>())
        {
            textBox.IsReadOnly = true;
            textBox.BorderBrush = Brushes.Transparent;
            textBox.BorderThickness = new Thickness(0);
        }

        EditButton.Content = "Изменить";
        EditButton.Background = Brushes.Transparent;
        CancelButton.Visibility = Visibility.Collapsed;
    }

    // Show RequestsGrid and hide UserDataGrid
    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        RequestsGrid.Visibility = Visibility.Visible;
        UserDataGrid.Visibility = Visibility.Collapsed;
    }

    public class User
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public string PhoneNumber { get; set; }
    }
}