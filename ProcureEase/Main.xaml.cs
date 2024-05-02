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
        LoadUserData();
        LoadUserRequests();
    }

    public ObservableCollection<string> SelectedFiles { get; }

    private void LoadUserData()
    {
        _currentUser = UserRepository.GetUserByUsername(UsernameTextBlock.Text);
        DataContext = _currentUser;
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
            new MetroDialogSettings { AnimateShow = false, AnimateHide = false });
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

        var validationResult = await ValidateInput(requestName, requestType, requestNotes);
        if (!validationResult.Item1)
        {
            await ShowErrorMessageAsync("Ошибка", validationResult.Item2);
            return;
        }

        if (_currentUser == null)
        {
            // Отображение ошибки или другое уведомление, если пользователь не определен
            await ShowErrorMessageAsync("Ошибка", "Пользователь не определен.");
            return;
        }

        var request = new Request
        {
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = _currentUser.UserId,
            RequestFiles = await ProcessFileAttachments()
        };

        // Добавление запроса и обработка результата
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

    private static Task<(bool, string)> ValidateInput(string requestName, string requestType, string requestNotes)
    {
        if (string.IsNullOrWhiteSpace(requestName) || string.IsNullOrWhiteSpace(requestType))
            return Task.FromResult((false, "Пожалуйста, заполните все обязательные поля."));

        if (!IsValidRequestName(requestName))
            return Task.FromResult((false, "Название заявки содержит недопустимые символы."));

        if (!IsValidRequestNotes(requestNotes))
            return Task.FromResult((false, "Примечания содержат недопустимые символы."));

        return Task.FromResult((true, ""));
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
        EditButton.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
        SaveButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SetEditMode(DependencyObject grid, bool isEditing)
    {
        foreach (var textBox in grid.FindVisualChildren<TextBox>())
            // Проверяем Tag перед изменением свойств
            if (textBox.Tag as string != "NoEdit")
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

    private async void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        var fileName = textBlock.Text;

        var dataGridRow = FindParent<DataGridRow>(textBlock);
        if (dataGridRow != null)
        {
            var request = (Request)dataGridRow.Item;
            var requestId = request.RequestId;

            // Создание и показ диалога для подтверждения
            var mySettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Да",
                NegativeButtonText = "Нет",
                AnimateShow = false,
                AnimateHide = false
            };

            var result = await this.ShowMessageAsync("Подтверждение загрузки",
                $"Вы уверены, что хотите загрузить {fileName}?",
                MessageDialogStyle.AffirmativeAndNegative, mySettings);

            if (result == MessageDialogResult.Affirmative)
            {
                var fileData = await RequestRepository.GetFileDataByRequestIdAndFileNameAsync(requestId, fileName);
                if (fileData != null)
                {
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var fullFilePath = Path.Combine(desktopPath, fileName);

                    try
                    {
                        await File.WriteAllBytesAsync(fullFilePath, fileData);
                        await ShowErrorMessageAsync("Файл сохранён", $"Файл сохранён: {fullFilePath}");
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorMessageAsync("Ошибка при сохранении файла", ex.Message);
                    }
                }
                else
                {
                    await ShowErrorMessageAsync("Ошибка",
                        $"Файл {fileName} не найден в базе данных для заявки с ID {requestId}.");
                }
            }
        }
    }

    private T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);

        if (parentObject == null) return null;

        var parent = parentObject as T;
        if (parent != null)
            return parent;
        return FindParent<T>(parentObject);
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!(DataContext is User userToUpdate))
        {
            await ShowErrorMessageAsync("Ошибка", "Данные пользователя не загружены.");
            return;
        }

        var validator = new UserValidator();
        var validationResults = validator.Validate(userToUpdate);
        if (!validationResults.IsValid)
        {
            HighlightErrors(validationResults.Errors);
            await ShowErrorMessageAsync("Ошибка", "Введенные данные некорректны.");
            return;
        }

        try
        {
            await UserRepository.UpdateUser(userToUpdate);
            ToggleEditing(false);
            await ShowErrorMessageAsync("Успех", "Данные успешно сохранены.");
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync("Ошибка", $"Ошибка сохранения данных: {ex.Message}");
        }
    }

    private void HighlightErrors(Dictionary<string, string> errors)
    {
        foreach (var element in errors.Select(error => FindControlByName(UserDataGrid, error.Key)))
        {
            if (element is TextBox textBox) textBox.BorderBrush = Brushes.Red;
        }
    }

    private static FrameworkElement FindControlByName(DependencyObject parent, string name)
    {
        if (parent is FrameworkElement fe && fe.Name == name)
            return fe;

        for (int i = 0, count = VisualTreeHelper.GetChildrenCount(parent); i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindControlByName(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var color = (Color)ColorConverter.ConvertFromString("#FFABADB3");
        textBox.BorderBrush = new SolidColorBrush(color);
    }
}