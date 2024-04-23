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
        UserRequestsListView.ItemsSource = GetUserRequests(login); 
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
    private static User? LoadUserData(string username)
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

    private List<UserRequest> GetUserRequests(string username)
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();

        // Сначала получите ID пользователя по username
        const string getUserIdQuery = "SELECT user_id FROM users WHERE username = @username";
        using var getUserIdCommand = new MySqlCommand(getUserIdQuery, connection);
        getUserIdCommand.Parameters.AddWithValue("@username", username);
        int userId = Convert.ToInt32(getUserIdCommand.ExecuteScalar());

        // Затем получите заявки пользователя
        const string query = @"SELECT r.request_id, r.request_name, rt.name as request_type, 
                           rs.name as request_status, r.notes, r.file 
                           FROM requests r
                           JOIN request_type rt ON r.request_type_id = rt.idRequestType
                           JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                           WHERE r.user_id = @userId";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@userId", userId);

        var requests = new List<UserRequest>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            requests.Add(new UserRequest
            {
                RequestId = reader.GetInt32("request_id"),
                RequestName = reader.GetString("request_name"),
                RequestTypeId = reader.GetString("request_type"),
                RequestStatusId = reader.GetString("request_status"),
                Notes = reader.GetString("notes"),
                // Обработка информации о файле (например, имени файла)
                FileName = reader.IsDBNull(reader.GetOrdinal("file")) ? null : "file.ext"
            });
        }

        return requests;
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

    public class UserRequest
    {
        public int RequestId { get; set; }
        public string RequestName { get; set; }
        public string RequestTypeId { get; set; }
        public string RequestStatusId { get; set; }
        public string Notes { get; set; }
        public string FileName { get; set; }
    }
}