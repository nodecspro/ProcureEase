#region

using ControlzEx.Theming;
using GalaSoft.MvvmLight.Command;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Xceed.Wpf.AvalonDock.Controls;

#endregion

namespace ProcureEase;

public partial class Main : MetroWindow
{
    private User? currentUser;
    private bool isEditing;
    public ObservableCollection<string> SelectedFiles { get; set; }

    public Main(string login)
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();
        SelectedFiles = new ObservableCollection<string>();
        UsernameTextBlock.Text = login;
        DataContext = this;
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
        ToggleVisibility(RequestsGrid, Visibility.Collapsed);
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

    private async void btnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Office Files|*.doc;*.docx;*.xls;*.xlsx;|Text Files|*.txt|Drawings|*.dwg;*.dxf|All Files|*.*";
        openFileDialog.Multiselect = true;

        if (openFileDialog.ShowDialog() == true)
        {
            List<string> invalidFiles = new List<string>();

            foreach (string filePath in openFileDialog.FileNames)
            {
                string extension = System.IO.Path.GetExtension(filePath).ToLower();

                if (extension == ".doc" || extension == ".docx" ||
                    extension == ".xls" || extension == ".xlsx" ||
                    extension == ".txt" ||
                    extension == ".dwg" || extension == ".dxf")
                {
                    string fileName = System.IO.Path.GetFileName(filePath);
                    SelectedFiles.Add(filePath);
                }
                else
                {
                    invalidFiles.Add(System.IO.Path.GetFileName(filePath));
                }
            }

            if (invalidFiles.Count > 0)
            {
                string message = "Следующие файлы имеют недопустимое расширение и не были добавлены:\n\n";
                message += string.Join("\n", invalidFiles);
                await this.ShowErrorMessageAsync("Недопустимые файлы", message);
            }
        }
    }

    private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
    {
        string file = (string)((Button)sender).DataContext;
        SelectedFiles.Remove(file);
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

        if (SelectedFiles.Count > 0)
        {
            foreach (string filePath in SelectedFiles)
            {
                byte[] fileData = File.ReadAllBytes(filePath);

                var requestFile = new RequestFile
                {
                    FileName = Path.GetFileName(filePath),
                    FileData = fileData
                };

                request.RequestFiles.Add(requestFile);
            }
        }

        int requestId = RequestRepository.AddRequest(request);
        if (requestId > 0)
        {
            RequestRepository.AddRequestFiles(requestId, request.RequestFiles);
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
                        var request = new Request
                        {
                            RequestId = reader.GetInt32("request_id"),
                            RequestName = reader.GetString("request_name"),
                            RequestType = reader.GetString("request_type"),
                            RequestStatus = reader.GetString("request_status"),
                            Notes = reader.GetString("notes")
                        };
                        request.RequestFiles = GetRequestFiles(request.RequestId);

                        requests.Add(request);
                    }
                }

                return requests;
            }
        }
    }

    // Новый метод для получения списка файлов по ID заявки
    private static List<RequestFile> GetRequestFiles(int requestId)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();
            const string query = "SELECT file_name FROM request_files WHERE request_id = @RequestId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RequestId", requestId);
                var files = new List<RequestFile>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        files.Add(new RequestFile { FileName = reader.GetString("file_name") });
                    }
                }
                return files;
            }
        }
    }

    public static int AddRequest(Request request)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();

            const string query = @"INSERT INTO requests (request_name, notes, user_id, request_status_id, request_type_id)
                               VALUES (@RequestName, @Notes, @UserId, @RequestStatusId, @RequestTypeId);
                               SELECT LAST_INSERT_ID();";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RequestName", request.RequestName);
                command.Parameters.AddWithValue("@Notes", request.Notes);
                command.Parameters.AddWithValue("@UserId", request.UserId);
                command.Parameters.AddWithValue("@RequestStatusId", GetRequestStatusId("В обработке"));
                command.Parameters.AddWithValue("@RequestTypeId", GetRequestTypeId(request.RequestType));

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
    }

    public static void AddRequestFiles(int requestId, List<RequestFile> requestFiles)
    {
        using (var connection = new MySqlConnection(AppSettings.ConnectionString))
        {
            connection.Open();

            const string query = @"INSERT INTO request_files (request_id, file_name, file_data)
                               VALUES (@RequestId, @FileName, @FileData)";

            foreach (var requestFile in requestFiles)
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RequestId", requestId);
                    command.Parameters.AddWithValue("@FileName", requestFile.FileName);
                    command.Parameters.AddWithValue("@FileData", requestFile.FileData);

                    command.ExecuteNonQuery();
                }
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
    public List<RequestFile> RequestFiles { get; set; }

    public Request()
    {
        RequestFiles = new List<RequestFile>();
    }
}

public class RequestFile
{
    public int RequestFileId { get; set; }
    public int RequestId { get; set; }
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
}