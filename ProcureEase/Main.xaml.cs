#region

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;
using Xceed.Wpf.AvalonDock.Controls;

#endregion

namespace ProcureEase;

public partial class Main : MetroWindow
{
    private User? currentUser;
    private bool isEditing;

    public Main(string login)
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();
        UsernameTextBlock.Text = login;
        LoadUserData();
        LoadUserRequests();
    }

    private void LoadUserData()
    {
        currentUser = UserRepository.GetUserByUsername(UsernameTextBlock.Text);
        UserDataGrid.DataContext = currentUser;
    }

    private void LoadUserRequests()
    {
        UserRequestsListView.ItemsSource = RequestRepository.GetUserRequests(currentUser.UserId);
    }

    private void UsernameTextBlock_Click(object sender, RoutedEventArgs e)
    {
        ToggleVisibility(NewRequestGrid, Visibility.Collapsed);
        ToggleVisibility(RequestsGrid);
        ToggleVisibility(UserDataGrid);
    }

    private void CreateRequest_Click(object sender, RoutedEventArgs e)
    {
        ShowSingleGrid(NewRequestGrid);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSingleGrid(RequestsGrid);
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEditing();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleEditing(false);
        LoadUserData();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSingleGrid(RequestsGrid);
    }

    private async Task ShowErrorMessageAsync(string title, string message)
    {
        var dialogSettings = new MetroDialogSettings { AnimateShow = false };
        await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, dialogSettings);
    }

    private bool IsValidRequestName(string requestName)
    {
        string pattern = @"^[a-zA-Zа-яА-Я0-9\s\-\.,#№()]+$";
        return Regex.IsMatch(requestName, pattern);
    }

    private bool IsValidRequestNotes(string requestNotes)
    {
        string pattern = @"^[a-zA-Zа-яА-Я0-9\s\-\.,#№()]+$";
        return string.IsNullOrEmpty(requestNotes) || (Regex.IsMatch(requestNotes, pattern));
    }

    private async void SaveRequestButton_Click(object sender, RoutedEventArgs e)
    {
        string requestName = RequestNameTextBox.Text;
        string requestType = (RequestTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        string requestNotes = RequestNotesTextBox.Text;

        if (string.IsNullOrWhiteSpace(requestName) || string.IsNullOrWhiteSpace(requestType))
        {
            await ShowErrorMessageAsync("Ошибка", "Пожалуйста, заполните все обязательные поля.");
            return;
        }

        if (!IsValidRequestName(requestName))
        {
            await ShowErrorMessageAsync("Ошибка", "Название заявки содержит недопустимые символы.");
            return;
        }

        if (!IsValidRequestNotes(requestNotes))
        {
            await ShowErrorMessageAsync("Ошибка", "Примечания содержат недопустимые символы.");
            return;
        }

        var request = new Request
        {
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = currentUser.UserId
        };

        if (RequestRepository.AddRequest(request))
        {
            ClearRequestForm();
            ShowSingleGrid(RequestsGrid);
            LoadUserRequests();
        }
        else
        {
            await ShowErrorMessageAsync("Ошибка", "Не удалось добавить заявку. Пожалуйста, попробуйте еще раз.");
        }
    }

    private void ToggleVisibility(Grid grid)
    {
        grid.Visibility = grid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ToggleVisibility(Grid grid, Visibility visibility)
    {
        grid.Visibility = visibility;
    }

    private void ShowSingleGrid(Grid gridToShow)
    {
        RequestsGrid.Visibility = Visibility.Collapsed;
        UserDataGrid.Visibility = Visibility.Collapsed;
        NewRequestGrid.Visibility = Visibility.Collapsed;

        gridToShow.Visibility = Visibility.Visible;
    }

    private void ToggleEditing(bool isEditing = true)
    {
        this.isEditing = isEditing;
        SetEditMode(UserDataGrid, isEditing);
        EditButton.Content = isEditing ? "Сохранить" : "Изменить";
        EditButton.Background = isEditing ? Brushes.Green : Brushes.Transparent;
        CancelButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetEditMode(Grid grid, bool isEditing)
    {
        foreach (var textBox in grid.FindVisualChildren

<TextBox>())
        {
            textBox.IsReadOnly = !isEditing;
            textBox.BorderThickness = isEditing ? new Thickness(1) : new Thickness(0);
            textBox.BorderBrush = isEditing
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFABADB3"))
                : Brushes.Transparent;
        }
    }

    private void ClearRequestForm()
    {
        RequestNameTextBox.Text = string.Empty;
        RequestTypeComboBox.SelectedIndex = -1;
        RequestNotesTextBox.Text = string.Empty;
    }
}

public static class UserRepository
{
    public static User? GetUserByUsername(string username)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();
            const string query = "SELECT * FROM users WHERE username = @username";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            UserId = reader.GetInt32("user_id"),
                            Username = reader.GetString("username"),
                            Email = reader.GetString("email"),
                            FirstName = reader.GetString("first_name"),
                            LastName = reader.GetString("last_name"),
                            Patronymic = reader.IsDBNull(reader.GetOrdinal("patronymic")) ? null : reader.GetString("patronymic"),
                            PhoneNumber = reader.GetString("phone_number")
                        };
                    }
                }
            }
        }

        return null;
    }
}

public static class RequestRepository
{
    public static List
    <Request> GetUserRequests(int userId)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();

            const string query = @"SELECT r.request_id, r.request_name, rt.name as request_type, 
                               rs.name as request_status, r.notes
                               FROM requests r
                               JOIN request_type rt ON r.request_type_id = rt.idRequestType
                               JOIN request_status rs ON r.request_status_id = rs.idRequestStatus
                               WHERE r.user_id = @userId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

                var requests = new List
        <Request>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        requests.Add(new Request
                        {
                            RequestId = reader.GetInt32("request_id"),
                            RequestName = reader.GetString("request_name"),
                            RequestType = reader.GetString("request_type"),
                            RequestStatus = reader.GetString("request_status"),
                            Notes = reader.GetString("notes")
                        });
                    }
                }

                return requests;
            }
        }
    }

    public static bool AddRequest(Request request)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();

            const string query = @"INSERT INTO requests (request_name, notes, user_id, request_status_id, request_type_id)
                               VALUES (@RequestName, @Notes, @UserId, @RequestStatusId, @RequestTypeId)";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RequestName", request.RequestName);
                command.Parameters.AddWithValue("@Notes", request.Notes);
                command.Parameters.AddWithValue("@UserId", request.UserId);
                command.Parameters.AddWithValue("@RequestStatusId", GetRequestStatusId("В обработке"));
                command.Parameters.AddWithValue("@RequestTypeId", GetRequestTypeId(request.RequestType));

                return command.ExecuteNonQuery() > 0;
            }
        }
    }

    private static int GetRequestStatusId(string requestStatusName)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();

            const string query = "SELECT idRequestStatus FROM request_status WHERE name = @RequestStatusName";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RequestStatusName", requestStatusName);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }

    private static int GetRequestTypeId(string requestTypeName)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();

            const string query = "SELECT idRequestType FROM request_type WHERE name = @RequestTypeName";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RequestTypeName", requestTypeName);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }
}

public class User
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Patronymic { get; set; }
    public string? PhoneNumber { get; set; }
}

public class Request
{
    public int RequestId { get; set; }
    public string? RequestName { get; set; }
    public string? RequestType { get; set; }
    public string? RequestStatus { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }
}