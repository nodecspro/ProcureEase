#region

using System.Collections.Concurrent;
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
    private string? _originalName;
    private string? _originalNotes;

    public Main(string login)
    {
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();
        UsernameTextBlock.Text = login;
        LoadUserData();
        LoadUserRequests();
        SortDataGridById();
        DataContext = this;
    }

    public ObservableCollection<string> SelectedFiles { get; } = new();
    public ObservableCollection<string> SelectedFilesDetailsGrid { get; } = new();

    private void ConfigureUIBasedOnUserRole(User user)
    {
        // Проверяем роль пользователя и настраиваем интерфейс
        switch (user.RoleId)
        {
            case 1:
                // Администратор
                YourPurchaseRequestsTextBlock.Text = "Заявки на закупку";
                CreateRequestButton.Visibility = Visibility.Collapsed;
                OrganizationButton.Visibility = Visibility.Visible;
                InvitationCodeButton.Visibility = Visibility.Visible;
                UserRequestsDataGrid.Height = 380;
                break;
            case 2:
                // Менеджер
                YourPurchaseRequestsTextBlock.Text = "Заявки ожидающие рассмотрения";
                CreateRequestButton.Visibility = Visibility.Collapsed;
                DeleteRequestButtonDetailsGrid.Visibility = Visibility.Collapsed;
                EditButtonDetailsGrid.Visibility = Visibility.Collapsed;
                AddFileButtonDetailsGrid.Visibility = Visibility.Collapsed;
                AcceptRequestManagerButtonDetailsGrid.Visibility = Visibility.Visible;
                RejectRequestManagerButtonDetailsGrid.Visibility = Visibility.Visible;
                UserRequestsDataGrid.Height = 380;
                break;
            case 3:
                // Заказчик

                break;
            case 4:
                // Поставщик
                YourPurchaseRequestsTextBlock.Text = "Доступные заявки на закупку";
                CreateRequestButton.Visibility = Visibility.Collapsed;
                DeleteRequestButtonDetailsGrid.Visibility = Visibility.Collapsed;
                EditButtonDetailsGrid.Visibility = Visibility.Collapsed;
                AcceptRequestSuppliersButtonDetailsGrid.Visibility = Visibility.Visible;
                UserRequestsDataGrid.Height = 380;
                StatusFieldRequestGrid.MinWidth = 150;
                StatusFieldRequestGrid.MaxWidth = 150;
                break;
        }
    }

    private void SortDataGridById()
    {
        var idColumn = FindIdColumn(UserRequestsDataGrid, "ID");
        if (idColumn != null)
        {
            ApplySort(UserRequestsDataGrid, idColumn.SortMemberPath, ListSortDirection.Ascending);
        }
    }

    private DataGridColumn FindIdColumn(DataGrid dataGrid, string headerName)
    {
        return dataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == headerName);
    }

    private void ApplySort(DataGrid dataGrid, string sortMemberPath, ListSortDirection direction)
    {
        dataGrid.Items.SortDescriptions.Clear();
        dataGrid.Items.SortDescriptions.Add(new SortDescription(sortMemberPath, direction));
        dataGrid.Items.Refresh();
    }

    private void LoadUserData()
    {
        _currentUser = UserRepository.GetUserByUsername(UsernameTextBlock.Text);
        ConfigureUIBasedOnUserRole(_currentUser);
        DataContext = _currentUser;
        UserDataGrid.DataContext = _currentUser;
    }

    private async void LoadUserRequests()
    {
        if (_currentUser == null) return;

        var requests = await RequestRepository.GetUserRequestsAsync(_currentUser);

        // Сортируем и обновляем данные в DataGrid
        UserRequestsDataGrid.ItemsSource = requests.OrderBy(r => r.RequestId).ToList();
        UserRequestsDataGrid.Items.Refresh();
    }

    private void UsernameTextBlock_Click(object sender, RoutedEventArgs e)
    {
        DisableEditing();
        ShowSingleGrid(UserDataGrid);
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

// Обработчик события клика по кнопке для открытия файла
    private async void btnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = CreateOpenFileDialog();

        if (openFileDialog.ShowDialog() != true) return;

        var (validFiles, invalidFiles) = ValidateFiles(openFileDialog.FileNames);

        AddValidFiles(validFiles);

        if (invalidFiles.Count > 0)
        {
            await ShowInvalidFilesMessageAsync(invalidFiles);
        }
    }

    private (List<string> validFiles, List<string> invalidFiles) ValidateFiles(IEnumerable<string> fileNames)
    {
        var validExtensions = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".txt", ".dwg", ".dxf" };
        var validFiles = new List<string>();
        var invalidFiles = new List<string>();

        foreach (var filePath in fileNames)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (validExtensions.Contains(extension))
            {
                if (SelectedFiles.Contains(filePath))
                {
                    var result = ShowDuplicateFileWarning(filePath);
                    if (result == MessageBoxResult.Yes)
                    {
                        SelectedFiles.Remove(filePath);
                        validFiles.Add(filePath);
                    }
                }
                else
                {
                    validFiles.Add(filePath);
                }
            }
            else
            {
                invalidFiles.Add(filePath);
            }
        }

        return (validFiles, invalidFiles);
    }

    private MessageBoxResult ShowDuplicateFileWarning(string filePath)
    {
        return MessageBox.Show(
            $"Файл \"{Path.GetFileName(filePath)}\" уже существует в списке.\n\n" +
            "Хотите перезаписать его?",
            "Дубликат файла",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
    }

    private void AddValidFiles(IEnumerable<string> validFiles)
    {
        foreach (var validFile in validFiles)
        {
            SelectedFiles.Add(validFile);
        }
    }

    private async Task ShowInvalidFilesMessageAsync(IEnumerable<string> invalidFiles)
    {
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
        DisableEditing();
        ShowSingleGrid(RequestsGrid);
        LoadUserRequests();
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

// Обработчик клика по кнопке "Сохранить заявку"
    private async void SaveRequestButton_Click(object sender, RoutedEventArgs e)
    {
        var (requestName, requestType, requestNotes) = ReadFormData();

        var validationResult = await ValidateInput(requestName, requestType, requestNotes);
        if (!validationResult.Item1)
        {
            await ShowErrorMessageAsync("Ошибка", validationResult.Item2);
            return;
        }

        if (_currentUser == null)
        {
            await ShowErrorMessageAsync("Ошибка", "Пользователь не определен.");
            return;
        }

        var request = CreateRequest(requestName, requestType, requestNotes, _currentUser.UserId,
            await ProcessFileAttachments(SelectedFiles));

        if (await AddRequestWithFiles(request))
        {
            SelectedFiles.Clear();
        }
        else
        {
            await ShowErrorMessageAsync("Ошибка", "Не удалось добавить заявку. Пожалуйста, попробуйте еще раз.");
        }
    }

    private (string requestName, string? requestType, string requestNotes) ReadFormData()
    {
        var requestName = RequestNameTextBox.Text;
        var requestType = (RequestTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        var requestNotes = RequestNotesTextBox.Text;
        return (requestName, requestType, requestNotes);
    }

    private Request CreateRequest(string requestName, string? requestType, string requestNotes, int userId,
        ObservableCollection<RequestFile> requestFiles)
    {
        return new Request
        {
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = userId,
            RequestFiles = requestFiles
        };
    }

// Асинхронный метод для добавления заявки с файлами
    private async Task<bool> AddRequestWithFiles(Request request)
    {
        var requestId = await RequestRepository.AddRequestAsync(request);
        if (requestId <= 0) return false;

        await AddFilesToRequestAsync(requestId, request.RequestFiles);

        PerformPostRequestActions();
        return true;
    }

    private async Task AddFilesToRequestAsync(int requestId, ObservableCollection<RequestFile> requestFiles)
    {
        await RequestRepository.AddRequestFilesAsync(requestId, requestFiles);
    }

    private void PerformPostRequestActions()
    {
        ClearRequestForm();
        ShowSingleGrid(RequestsGrid);
        LoadUserRequests();
    }

    // Асинхронный метод для обработки прикреплённых файлов
    private static async Task<ObservableCollection<RequestFile>> ProcessFileAttachments(
        ObservableCollection<string> nameList, int maxDegreeOfParallelism = 4)
    {
        if (nameList.Count == 0)
            return new ObservableCollection<RequestFile>();

        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var requestFiles = new List<RequestFile>();

        var tasks = nameList.Select(filePath => ProcessFileAsync(filePath, semaphore, requestFiles));

        await Task.WhenAll(tasks);

        return new ObservableCollection<RequestFile>(requestFiles.OrderBy(rf => nameList.IndexOf(rf.FileName)));
    }

    private static async Task ProcessFileAsync(string filePath, SemaphoreSlim semaphore, List<RequestFile> requestFiles)
    {
        await semaphore.WaitAsync();
        try
        {
            var fileData = await File.ReadAllBytesAsync(filePath);
            lock (requestFiles)
            {
                requestFiles.Add(new RequestFile
                {
                    FileName = Path.GetFileName(filePath),
                    FileData = fileData
                });
            }
        }
        catch (Exception ex)
        {
            // Log the error or handle it appropriately
            Console.WriteLine($"Failed to read file {filePath}: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static Task<(bool, string)> ValidateInput(string requestName, string? requestType, string requestNotes)
    {
        if (string.IsNullOrWhiteSpace(requestName) || string.IsNullOrWhiteSpace(requestType))
        {
            return Task.FromResult((false, "Пожалуйста, заполните все обязательные поля."));
        }

        if (!IsValidRequestName(requestName))
        {
            return Task.FromResult((false, "Название заявки содержит недопустимые символы."));
        }

        if (!IsValidRequestNotes(requestNotes))
        {
            return Task.FromResult((false, "Примечания содержат недопустимые символы."));
        }

        return Task.FromResult((true, ""));
    }

    private void ShowSingleGrid(UIElement gridToShow)
    {
        var allGrids = new List<UIElement>
        {
            RequestsGrid, UserDataGrid, NewRequestGrid, DetailsGrid, OrganizationGrid, InvitationCodesGrid,
            AddOrganizationGrid
        };

        foreach (var grid in allGrids) grid.Visibility = grid == gridToShow ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ToggleEditing(bool isEditing = true)
    {
        SetEditMode(UserDataGrid, isEditing);
        EditButton.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
        SaveButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        ExitButton.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void SetEditMode(DependencyObject grid, bool isEditing)
    {
        var borderBrush = isEditing
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFABADB3"))
            : Brushes.Transparent;
        var borderThickness = isEditing ? new Thickness(0, 0, 0, 1) : new Thickness(0);

        foreach (var textBox in grid.FindVisualChildren<TextBox>())
        {
            if (textBox.Tag as string != "NoEdit")
            {
                SetTextBoxEditMode(textBox, isEditing, borderBrush, borderThickness);
            }
        }
    }

    private static void SetTextBoxEditMode(TextBox textBox, bool isEditing, Brush borderBrush,
        Thickness borderThickness)
    {
        textBox.IsReadOnly = !isEditing;
        textBox.BorderThickness = borderThickness;
        textBox.BorderBrush = borderBrush;
    }

    private void ClearRequestForm()
    {
        RequestNameTextBox.Clear();
        RequestTypeComboBox.SelectedIndex = -1;
        RequestNotesTextBox.Clear();
    }

    private async void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            var fileName = textBlock.Text;
            var dataGridRow = FindParent<DataGridRow>(textBlock);

            if (dataGridRow != null && dataGridRow.Item is Request request)
            {
                if (await ConfirmFileDownloadAsync(fileName))
                {
                    await DownloadFileAsync(request.RequestId, fileName);
                }
            }
        }
    }

    private async Task<bool> ConfirmFileDownloadAsync(string fileName)
    {
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

        return result == MessageDialogResult.Affirmative;
    }

    private async Task DownloadFileAsync(int requestId, string fileName)
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

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;

        while (current != null)
        {
            var parentObject = VisualTreeHelper.GetParent(current);

            if (parentObject is T parent)
                return parent;

            current = parentObject;
        }

        return null;
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        LoadUserData();
        if (!(DataContext is User userToUpdate))
        {
            await ShowErrorMessageAsync("Ошибка", "Данные пользователя не загружены.");
            return;
        }

        if (!ValidateUser(userToUpdate)) return;

        await SaveUserAsync(userToUpdate);
    }

    private bool ValidateUser(User user)
    {
        var validator = new UserValidator();
        var validationResults = validator.Validate(user);
        if (!validationResults.IsValid)
        {
            HighlightErrors(validationResults.Errors);
            ShowErrorMessageAsync("Ошибка", "Введенные данные некорректны.").ConfigureAwait(false);
            return false;
        }

        return true;
    }

    private async Task SaveUserAsync(User user)
    {
        try
        {
            await UserRepository.UpdateUser(user);
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
            if (element is TextBox textBox)
                textBox.BorderBrush = Brushes.Red;
    }

    private static FrameworkElement FindControlByName(DependencyObject parent, string name)
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is FrameworkElement fe && fe.Name == name)
            {
                return fe;
            }

            for (int i = 0, count = VisualTreeHelper.GetChildrenCount(current); i < count; i++)
            {
                queue.Enqueue(VisualTreeHelper.GetChild(current, i));
            }
        }

        return null;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var color = (Color)ColorConverter.ConvertFromString("#FFABADB3");
        textBox.BorderBrush = new SolidColorBrush(color);
    }

    private void UserRequestsDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (UserRequestsDataGrid.SelectedItem is Request selectedRequest)
        {
            RequestsGrid.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Visible;
            DetailsGrid.DataContext = selectedRequest;
            RefreshFilesListUi();
        }
    }

    private void EditButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        EnableEditing();
    }

    private void EnableEditing()
    {
        SaveOriginalData();
        SetEditableFields(true);
        ToggleButtonsVisibility(true);
    }

    private void SaveOriginalData()
    {
        _originalName = NameTextBox.Text;
        _originalNotes = NotesTextBox.Text;
    }

    private void SetEditableFields(bool isEditable)
    {
        var thickness = isEditable ? new Thickness(0, 0, 0, 1) : new Thickness(0);
        NameTextBox.IsReadOnly = !isEditable;
        NameTextBox.BorderThickness = thickness;
        NotesTextBox.IsReadOnly = !isEditable;
        NotesTextBox.BorderThickness = thickness;
    }

    private void ToggleButtonsVisibility(bool isEditing)
    {
        SaveButtonDetailsGrid.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        CancelButtonDetailsGrid.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        AddFileButtonDetailsGrid.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;

        EditButtonDetailsGrid.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
        DeleteRequestButtonDetailsGrid.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void DeleteRequestButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag.ToString(), out var requestId))
        {
            if (await ConfirmDeletionAsync("Подтверждение удаления",
                    "Вы уверены, что хотите удалить эту заявку и все связанные файлы?"))
            {
                await DeleteRequestAsync(requestId);
            }
        }
    }

    private async Task<bool> ConfirmDeletionAsync(string title, string message)
    {
        var mySettings = new MetroDialogSettings
        {
            AffirmativeButtonText = "Да",
            NegativeButtonText = "Нет",
            AnimateShow = false,
            AnimateHide = false
        };

        var confirmResult =
            await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, mySettings);
        return confirmResult == MessageDialogResult.Affirmative;
    }

    private async Task DeleteRequestAsync(int requestId)
    {
        try
        {
            var result = await RequestRepository.DeleteRequestWithFilesAsync(requestId);
            if (result)
            {
                await ShowErrorMessageAsync("Удаление завершено", "Заявка и все связанные файлы успешно удалены.");
                ShowSingleGrid(RequestsGrid); // Обновление или скрытие интерфейса
                LoadUserRequests(); // Обновление списка заявок
            }
            else
            {
                await ShowErrorMessageAsync("Ошибка удаления", "Произошла ошибка при удалении заявки.");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync("Ошибка", $"Произошла ошибка: {ex.Message}");
        }
    }

    private void BackButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        DisableEditing();
        // Показываем другой Grid, например RequestsGrid
        ShowSingleGrid(RequestsGrid);
        LoadUserRequests();
    }

    private async void DeleteFileButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentUser.RoleId != 3)
        {
            await ShowErrorMessageAsync("Ошибка", "Вы не являетесь автором этой заявки.");
            return;
        }

        if (!(sender is Button button) || button.Tag == null)
        {
            await ShowErrorMessageAsync("Ошибка", "Невозможно определить имя файла для удаления.");
            return;
        }

        var fileName = button.Tag.ToString();

        if (!await ConfirmFileDeletionAsync(fileName)) return;

        var request = FindParentRequest(button);

        if (request != null)
            await DeleteFileAsync(request, fileName);
        else
            await ShowErrorMessageAsync("Ошибка", "Невозможно определить заявку для удаления файла.");
    }

    private async Task<bool> ConfirmFileDeletionAsync(string fileName)
    {
        var mySettings = new MetroDialogSettings
        {
            AffirmativeButtonText = "Да",
            NegativeButtonText = "Нет",
            AnimateShow = false,
            AnimateHide = false
        };

        var result = await this.ShowMessageAsync("Подтверждение загрузки",
            $"Вы уверены, что удалить файл {fileName}?",
            MessageDialogStyle.AffirmativeAndNegative, mySettings);

        return result == MessageDialogResult.Affirmative;
    }

    private static Request FindParentRequest(DependencyObject child)
    {
        var parent = child;

        while (parent != null)
        {
            parent = VisualTreeHelper.GetParent(parent) ?? LogicalTreeHelper.GetParent(parent);
            if (parent is FrameworkElement frameworkElement && frameworkElement.DataContext is Request request)
                return request;
        }

        return null;
    }

    private async Task DeleteFileAsync(Request request, string fileName)
    {
        var fileToRemove = request.RequestFiles.FirstOrDefault(f => f.FileName == fileName);

        if (fileToRemove != null && RequestRepository.DeleteFileFromDatabase(request.RequestId, fileName))
        {
            request.RequestFiles.Remove(fileToRemove);
            RefreshFilesListUi();
        }
        else
        {
            await ShowErrorMessageAsync("Ошибка", "Ошибка при удалении файла из базы данных.");
        }
    }

    private void RefreshFilesListUi()
    {
        if (DetailsGrid.DataContext is Request currentRequest)
        {
            RequestFilesDetailsGrid.ItemsSource = currentRequest.RequestFiles;
            RequestFilesDetailsGrid.Items.Refresh();
        }
    }

    private void DeleteNewFileButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        var file = (string)((Button)sender).DataContext;
        SelectedFilesDetailsGrid.Remove(file);
    }

    private async void SaveButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag.ToString(), out var requestId))
        {
            var requestName = NameTextBox.Text.Trim();
            var requestType = StatusTextBox.Text;
            var requestNotes = NotesTextBox.Text.Trim();

            var validationResult = await ValidateInput(requestName, requestType, requestNotes);
            if (!validationResult.Item1)
            {
                await ShowErrorMessageAsync("Ошибка", validationResult.Item2);
                return;
            }

            var updatedRequest = CreateUpdatedRequest(requestId, requestName, requestType, requestNotes);

            try
            {
                await RequestRepository.UpdateRequestAsync(updatedRequest);
                _originalName = requestName;
                _originalNotes = requestNotes;
                await HandleFileAttachmentsAsync(requestId);
                await ShowErrorMessageAsync("Успех", "Изменения успешно сохранены.");
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Ошибка", $"Ошибка при сохранении изменений: {ex.Message}");
            }
        }
    }

    private Request CreateUpdatedRequest(int requestId, string requestName, string requestType, string requestNotes)
    {
        return new Request
        {
            RequestId = requestId,
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = _currentUser.UserId
        };
    }

    private async Task HandleFileAttachmentsAsync(int requestId)
    {
        if (SelectedFilesDetailsGrid.Count > 0)
        {
            var attachedFiles = await ProcessFileAttachments(SelectedFilesDetailsGrid);

            if (attachedFiles.Any())
            {
                await RequestRepository.AddRequestFilesAsync(requestId, attachedFiles);

                if (DetailsGrid.DataContext is Request currentRequest)
                {
                    foreach (var file in attachedFiles) currentRequest.RequestFiles.Add(file);
                }
            }
        }

        DisableEditing();
        RefreshFilesListUi();
    }

    private void CancelButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        DisableEditing();
    }

    private void DisableEditing()
    {
        RestoreOriginalValues();
        SetVisibilityForButtons();
        SetReadOnlyFields(_currentUser.RoleId == 3);
    }

    private void RestoreOriginalValues()
    {
        NameTextBox.Text = _originalName;
        NotesTextBox.Text = _originalNotes;
        SelectedFilesDetailsGrid.Clear();
    }

    private void SetVisibilityForButtons()
    {
        SaveButtonDetailsGrid.Visibility = Visibility.Collapsed;
        CancelButtonDetailsGrid.Visibility = Visibility.Collapsed;
        AddFileButtonDetailsGrid.Visibility = Visibility.Collapsed;
        EditButtonDetailsGrid.Visibility = _currentUser.RoleId == 3 ? Visibility.Visible : Visibility.Collapsed;
        DeleteRequestButtonDetailsGrid.Visibility =
            _currentUser.RoleId == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetReadOnlyFields(bool isReadOnly)
    {
        NameTextBox.IsReadOnly = isReadOnly;
        NameTextBox.BorderThickness = new Thickness(0);

        NotesTextBox.IsReadOnly = isReadOnly;
        NotesTextBox.BorderThickness = new Thickness(0);
    }

    private async void AddFileButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = CreateOpenFileDialog();
        if (openFileDialog.ShowDialog() != true) return;

        var (validFiles, invalidFiles) = FilterFilesByExtension(openFileDialog.FileNames);

        AddValidFilesToCollection(validFiles);

        if (invalidFiles.Any())
        {
            var message = CreateInvalidFilesMessage(invalidFiles);
            await ShowErrorMessageAsync("Недопустимые файлы", message);
        }
    }

    private OpenFileDialog CreateOpenFileDialog()
    {
        return new OpenFileDialog
        {
            Filter = "Office Files|*.doc;*.docx;*.xls;*.xlsx;|Text Files|*.txt|Drawings|*.dwg;*.dxf|All Files|*.*",
            Multiselect = true
        };
    }

    private (List<string> validFiles, List<string> invalidFiles) FilterFilesByExtension(IEnumerable<string> fileNames)
    {
        var validExtensions = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".txt", ".dwg", ".dxf" };
        var validFiles = fileNames
            .Where(filePath => validExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant())).ToList();
        var invalidFiles = fileNames.Except(validFiles).ToList();
        return (validFiles, invalidFiles);
    }

    private void AddValidFilesToCollection(IEnumerable<string> validFiles)
    {
        foreach (var validFile in validFiles) SelectedFilesDetailsGrid.Add(validFile);
    }

    private string CreateInvalidFilesMessage(IEnumerable<string> invalidFiles)
    {
        return
            $"Следующие файлы имеют недопустимое расширение и не были добавлены:\n\n{string.Join("\n", invalidFiles.Select(Path.GetFileName))}";
    }

    private async void AcceptRequestManagerButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        const int acceptedStatusId = 4;

        if (sender is not Button button || !int.TryParse(button.Tag.ToString(), out var requestId))
        {
            await ShowErrorMessageAsync("Ошибка", "Ошибка в источнике события или некорректный идентификатор заявки.");
            return;
        }

        await UpdateRequestStatusAsync(requestId, acceptedStatusId, "Заявка принята!", "Заявка не найдена!");
    }

    private async Task UpdateRequestStatusAsync(int requestId, int statusId, string successMessage,
        string notFoundMessage)
    {
        try
        {
            var isUpdated = await RequestRepository.ChangeRequestStatus(requestId, statusId);
            var message = isUpdated ? successMessage : notFoundMessage;
            await ShowErrorMessageAsync(isUpdated ? "Успех" : "Ошибка", message);

            if (isUpdated)
            {
                ShowSingleGrid(RequestsGrid);
                LoadUserRequests();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync("Исключение", $"Произошла ошибка: {ex.Message}");
        }
    }

    private async void RejectRequestManagerButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        const int rejectedStatusId = 3;
        const string dialogTitle = "Причина отклонения";
        const string dialogMessage = "Введите причину отклонения заявки:";

        if (!(sender is Button button) || !int.TryParse(button.Tag.ToString(), out var requestId))
        {
            await ShowErrorMessageAsync("Ошибка", "Ошибка в источнике события или некорректный идентификатор заявки.");
            return;
        }

        string input;
        do
        {
            input = await this.ShowInputAsync(dialogTitle, dialogMessage, new MetroDialogSettings
            {
                AffirmativeButtonText = "Отклонить заявку",
                NegativeButtonText = "Отмена",
                AnimateShow = false,
                AnimateHide = false,
                DefaultButtonFocus = MessageDialogResult.Affirmative
            });

            if (input == null) return;

            if (string.IsNullOrWhiteSpace(input))
                await ShowErrorMessageAsync("Ошибка", "Поле причины отклонения не может быть пустым.");
        } while (string.IsNullOrWhiteSpace(input));

        await UpdateRequestStatusAsync(requestId, rejectedStatusId, input);
    }

    private async Task UpdateRequestStatusAsync(int requestId, int statusId, string reason)
    {
        try
        {
            var isUpdated = await RequestRepository.ChangeRequestStatusAndReason(requestId, statusId, reason);
            var message = isUpdated ? "Заявка успешно отклонена." : "Заявка не найдена.";
            await ShowErrorMessageAsync(isUpdated ? "Успех" : "Ошибка", message);
            LoadUserRequests();
            ShowSingleGrid(RequestsGrid);
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync("Исключение", $"Произошла ошибка: {ex.Message}");
        }
    }

    private void ExitButton_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.MainWindow?.Show();
        Hide();
    }

    private void OrganizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        LoadOrganizationGrid();
    }

    private async void LoadOrganizationGrid()
    {
        ShowSingleGrid(OrganizationGrid);
        var viewModel = new SuppliersViewModel();
        DataContext = viewModel;
        viewModel.LoadSuppliers();
    }

    private async void InvitationCodeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShowSingleGrid(InvitationCodesGrid);
        var viewModel = new InvitationCodesViewModel();
        DataContext = viewModel;
        viewModel.LoadData();
    }

    private void AddOrganizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Скрываем основной Grid и показываем Grid для добавления организации
        ShowSingleGrid(AddOrganizationGrid);
    }

    private void CancelOrganizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Скрываем Grid для добавления организации и показываем основной Grid
        LoadOrganizationGrid();
    }

    private void SaveOrganizationButton_OnClick(object sender, RoutedEventArgs e)
    {
        var fields = new Dictionary<string, string>
        {
            { "INN", InnTextBox.Text },
            { "KPP", KppTextBox.Text },
            { "FullName", FullNameTextBox.Text.Trim() },
            { "Supervisor", SupervisorTextBox.Text.Trim() },
            { "Email", EmailTextBox.Text },
            { "ContactNumber", new string(ContactNumberTextBox.Text.Where(char.IsDigit).ToArray()) }
        };

        if (fields.Values.Any(string.IsNullOrWhiteSpace))
        {
            MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SuppliersRepository.SaveOrganizationToDatabase(
            fields["INN"], fields["KPP"], fields["FullName"], fields["Supervisor"], fields["ContactNumber"],
            fields["Email"]
        );

        ClearTextBoxes(InnTextBox, KppTextBox, FullNameTextBox, SupervisorTextBox, EmailTextBox, ContactNumberTextBox);

        LoadOrganizationGrid();
    }

    private static void ClearTextBoxes(params TextBox[] textBoxes)
    {
        foreach (var textBox in textBoxes) textBox.Clear();
    }

    private void AddInvitationCodeButton_OnClick(object sender, RoutedEventArgs e)
    {
        var addWindow = new AddInvitationCodeWindow
        {
            DataContext = new AddInvitationCodeViewModel()
        };

        if (addWindow.ShowDialog() == true)
        {
            // Обновить коды приглашений после добавления нового
            var viewModel = DataContext as InvitationCodesViewModel;
            if (viewModel != null)
                viewModel.LoadData();
        }
    }

    private void RemoveOldInvitationCodeButton_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as InvitationCodesViewModel;
        if (viewModel != null) viewModel.RemoveOldInvitationCodes();
    }

    private void DataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
            var dataGridCell = hitTestResult?.VisualHit as DataGridCell ??
                               FindVisualParent<DataGridCell>(hitTestResult.VisualHit);

            if (dataGridCell == null) return;

            dataGridCell.Focus(); // Устанавливаем фокус на ячейку
            var contextMenu = dataGrid.ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.PlacementTarget = dataGrid;
                contextMenu.IsOpen = true;
            }
        }
    }

    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = child;

        while (parentObject != null)
        {
            parentObject = VisualTreeHelper.GetParent(parentObject);
            if (parentObject is T parent) return parent;
        }

        return null;
    }

    private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dataGrid = InvitationCodesDataGrid;
        if (dataGrid.CurrentCell != null)
        {
            var cellInfo = dataGrid.CurrentCell;
            if (cellInfo.Column?.GetCellContent(cellInfo.Item) is TextBlock cellContent)
                Clipboard.SetText(cellContent.Text);
        }
    }

    private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var dataGrid = InvitationCodesDataGrid;
        if (dataGrid.CurrentCell != null && dataGrid.CurrentCell.Item is InvitationCode selectedCode)
        {
            var viewModel = DataContext as InvitationCodesViewModel;
            if (viewModel != null) viewModel.DeleteInvitationCode(selectedCode);
        }
    }
}