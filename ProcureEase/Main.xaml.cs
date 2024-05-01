#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using ProcureEase.Classes;
using Xceed.Wpf.AvalonDock.Controls;

#endregion

namespace ProcureEase;

public partial class Main
{
    private static readonly Regex RequestNameRegex = new(@"^[a-zA-Zа-яА-ЯёЁ0-9\s\-\.,#№()]+$");
    private User? _currentUser;

    public Main(string login)
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        // Установка сортировки по умолчанию по столбцу "ID"
        var idColumn = UserRequestsDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "ID");
        if (idColumn != null)
            UserRequestsDataGrid.Items.SortDescriptions.Add(new SortDescription(idColumn.SortMemberPath,
                ListSortDirection.Ascending));

        SelectedFiles = new ObservableCollection<string>();
        UsernameTextBlock.Text = login;
        DataContext = this;
        LoadUserData();
        LoadUserRequests();
    }

    public ObservableCollection<string> SelectedFiles { get; }

    private void LoadUserData()
    {
        _currentUser = UserRepository.GetUserByUsername(UsernameTextBlock.Text);
        UserDataGrid.DataContext = _currentUser;
    }

    private async void LoadUserRequests()
    {
        if (_currentUser != null)
            try
            {
                var requests = await RequestRepository.GetUserRequestsAsync(_currentUser.UserId);
                UserRequestsDataGrid.ItemsSource = requests;
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine("Failed to load user requests: " + ex.Message);
            }
    }

    private void UsernameTextBlock_Click(object sender, RoutedEventArgs e)
    {
        ToggleVisibility(NewRequestGrid, Visibility.Collapsed);
        ToggleVisibility(RequestsGrid, Visibility.Collapsed);
        if (UserDataGrid.Visibility == Visibility.Collapsed) ToggleVisibility(UserDataGrid, Visibility.Visible);
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
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Office Files|*.doc;*.docx;*.xls;*.xlsx;|Text Files|*.txt|Drawings|*.dwg;*.dxf|All Files|*.*",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() != true) return;

        var validExtensions = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".txt", ".dwg", ".dxf" };

        var (validFiles, invalidFiles) = openFileDialog.FileNames
            .Partition(filePath => validExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant()));

        foreach (var validFile in validFiles)
            // Replace this line with the appropriate method to add to your SelectedFiles collection
            SelectedFiles.Add(validFile);

        if (invalidFiles.Count == 0) return;
        var message =
            $"Следующие файлы имеют недопустимое расширение и не были добавлены:\n\n{string.Join("\n", invalidFiles.Select(Path.GetFileName))}";
        await ShowErrorMessageAsync("Недопустимые файлы", message);
    }

    private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
    {
        var file = (string)((Button)sender).DataContext;
        SelectedFiles.Remove(file);
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSingleGrid(RequestsGrid);
    }

    private async Task ShowErrorMessageAsync(string title, string message)
    {
        await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative,
            new MetroDialogSettings { AnimateShow = false });
    }

    private static bool IsValidRequestName(string requestName)
    {
        return !string.IsNullOrEmpty(requestName) && RequestNameRegex.IsMatch(requestName);
    }

    private static bool IsValidRequestNotes(string requestNotes)
    {
        return string.IsNullOrEmpty(requestNotes) || RequestNameRegex.IsMatch(requestNotes);
    }

    private async void SaveRequestButton_Click(object sender, RoutedEventArgs e)
    {
        var requestName = RequestNameTextBox.Text;
        var requestType = (RequestTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        var requestNotes = RequestNotesTextBox.Text;

        // Validate input
        if (requestType != null && !await ValidateInput(requestName, requestType, requestNotes)) return;

        if (_currentUser == null) return;

        var request = new Request
        {
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = _currentUser.UserId,
            // Handle file attachments
            RequestFiles = await ProcessFileAttachments()
        };

        // Add request and handle the result
        if (!await AddRequestWithFiles(request))
            await ShowErrorMessageAsync("Ошибка", "Не удалось добавить заявку. Пожалуйста, попробуйте еще раз.");
    }

    private async Task<bool> AddRequestWithFiles(Request request)
    {
        var requestId = await RequestRepository.AddRequestAsync(request);
        if (requestId <= 0) return false;

        await RequestRepository.AddRequestFilesAsync(requestId, request.RequestFiles);
        ClearRequestForm();
        ShowSingleGrid(RequestsGrid);
        LoadUserRequests();
        return true;
    }

    private async Task<List<RequestFile>> ProcessFileAttachments()
    {
        if (SelectedFiles.Count == 0) return new List<RequestFile>();

        var tasks = SelectedFiles.Select(filePath => File.ReadAllBytesAsync(filePath)).ToList();
        var fileBytes = await Task.WhenAll(tasks);

        return SelectedFiles.Select((filePath, index) => new RequestFile
        {
            FileName = Path.GetFileName(filePath),
            FileData = fileBytes[index]
        }).ToList();
    }

    private async Task<bool> ValidateInput(string requestName, string requestType, string requestNotes)
    {
        if (string.IsNullOrWhiteSpace(requestName) || string.IsNullOrWhiteSpace(requestType))
        {
            await ShowErrorMessageAsync("Ошибка", "Пожалуйста, заполните все обязательные поля.");
            return false;
        }

        if (!IsValidRequestName(requestName))
        {
            await ShowErrorMessageAsync("Ошибка", "Название заявки содержит недопустимые символы.");
            return false;
        }

        if (!IsValidRequestNotes(requestNotes))
        {
            await ShowErrorMessageAsync("Ошибка", "Примечания содержат недопустимые символы.");
            return false;
        }

        return true;
    }

    private static void ToggleVisibility(UIElement grid, Visibility visibility)
    {
        grid.Visibility = visibility;
    }

    private void ShowSingleGrid(UIElement gridToShow)
    {
        RequestsGrid.Visibility = gridToShow == RequestsGrid ? Visibility.Visible : Visibility.Collapsed;
        UserDataGrid.Visibility = gridToShow == UserDataGrid ? Visibility.Visible : Visibility.Collapsed;
        NewRequestGrid.Visibility = gridToShow == NewRequestGrid ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ToggleEditing(bool isEditing = true)
    {
        SetEditMode(UserDataGrid, isEditing);
        EditButton.Content = isEditing ? "Сохранить" : "Изменить";
        EditButton.Background = isEditing ? Brushes.Green : Brushes.Transparent;
        CancelButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SetEditMode(DependencyObject grid, bool isEditing)
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
        RequestNameTextBox.Clear();
        RequestTypeComboBox.SelectedIndex = -1;
        RequestNotesTextBox.Clear();
    }

    private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        var fileName = textBlock.Text;

        // Получение объекта DataGridRow из нажатого TextBlock
        var dataGridRow = FindParent<DataGridRow>(textBlock);
        if (dataGridRow != null)
        {
            // Получение объекта, связанного с этой строкой
            var request = (Request)dataGridRow.Item;

            // Извлечение ID заявки
            var requestId = request.RequestId;

            // Проверка существования файла
            var filePath = Path.Combine("путь_к_папке_с_файлами", fileName);
            var fileExists = File.Exists(filePath);

            // Вывод сообщения с ID заявки и именем файла
            var message = fileExists
                ? $"File exists: {fileName} (Request ID: {requestId})"
                : $"File does not exist: {fileName} (Request ID: {requestId})";
            _ = ShowErrorMessageAsync("File Clicked", message);
        }
    }

// Вспомогательный метод для поиска родительского элемента заданного типа
    private T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);

        if (parentObject == null) return null;

        var parent = parentObject as T;
        if (parent != null)
            return parent;
        return FindParent<T>(parentObject);
    }
}