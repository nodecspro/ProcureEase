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
        SortDataGribById();
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

                break;
            case 2:
                // Менеджер
                YourPurchaseRequestsTextBlock.Text = "Заявки на закупку";
                CreateRequestButton.Visibility = Visibility.Hidden;
                DeleteRequestButtonDetailsGrid.Visibility = Visibility.Hidden;
                EditButtonDetailsGrid.Visibility = Visibility.Hidden;
                AddFileButtonDetailsGrid.Visibility = Visibility.Hidden;
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
                CreateRequestButton.Visibility = Visibility.Hidden;
                DeleteRequestButtonDetailsGrid.Visibility = Visibility.Hidden;
                EditButtonDetailsGrid.Visibility = Visibility.Hidden;
                AcceptRequestSuppliersButtonDetailsGrid.Visibility = Visibility.Visible;
                UserRequestsDataGrid.Height = 380;
                StatusFieldRequestGrid.MinWidth = 150;
                StatusFieldRequestGrid.MaxWidth = 150;
                break;
        }
    }

    private void SortDataGribById()
    {
        var idColumn = UserRequestsDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "ID");
        if (idColumn != null)
            UserRequestsDataGrid.Items.SortDescriptions.Add(new SortDescription(idColumn.SortMemberPath,
                ListSortDirection.Ascending));
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
        // Создание и настройка диалога выбора файлов
        var openFileDialog = new OpenFileDialog
        {
            // Установка фильтра для отображения определённых типов файлов в диалоге
            Filter = "Office Files|*.doc;*.docx;*.xls;*.xlsx|Text Files|*.txt|Drawings|*.dwg;*.dxf|All Files|*.*",
            // Разрешение выбора нескольких файлов
            Multiselect = true
        };

        // Отображение диалога выбора файлов и проверка на успешное закрытие с выбранными файлами
        if (openFileDialog.ShowDialog() != true) return;

        // Создание списка допустимых расширений файлов
        var validExtensions = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".txt", ".dwg", ".dxf" };

        // Разделение выбранных файлов на допустимые и недопустимые по расширению
        var validFiles = new List<string>();
        var invalidFiles = new List<string>();

        foreach (var filePath in openFileDialog.FileNames)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (validExtensions.Contains(extension))
            {
                if (SelectedFiles.Contains(filePath))
                {
                    // Если файл уже существует, отобразить предупреждение
                    var result = MessageBox.Show(
                        $"Файл \"{Path.GetFileName(filePath)}\" уже существует в списке.\n\n" +
                        "Хотите перезаписать его?",
                        "Дубликат файла",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Перезаписать файл, удалив предыдущий экземпляр
                        SelectedFiles.Remove(filePath);
                        validFiles.Add(filePath);
                    }
                    // Если выбрано "No", не добавлять файл
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

        // Добавление допустимых файлов в коллекцию выбранных файлов
        foreach (var validFile in validFiles)
            SelectedFiles.Add(validFile);

        // Если нет недопустимых файлов, прекратить выполнение функции
        if (invalidFiles.Count == 0) return;

        // Создание сообщения об ошибках для недопустимых файлов
        var message =
            $"Следующие файлы имеют недопустимое расширение и не были добавлены:\n\n{string.Join("\n", invalidFiles.Select(Path.GetFileName))}";

        // Отображение сообщения об ошибке
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
        // Считывание данных из формы
        var requestName = RequestNameTextBox.Text; // Имя заявки
        var requestType = (RequestTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(); // Тип заявки
        var requestNotes = RequestNotesTextBox.Text; // Примечания к заявке

        // Валидация введённых данных
        var validationResult = await ValidateInput(requestName, requestType, requestNotes);
        if (!validationResult.Item1) // Если валидация не пройдена
        {
            // Отображение сообщения об ошибке
            await ShowErrorMessageAsync("Ошибка", validationResult.Item2);
            return;
        }

        // Проверка наличия данных о текущем пользователе
        if (_currentUser == null)
        {
            // Отображение ошибки, если пользователь не определён
            await ShowErrorMessageAsync("Ошибка", "Пользователь не определен.");
            return;
        }

        // Создание объекта заявки с данными из формы
        var request = new Request
        {
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = _currentUser.UserId, // ID пользователя
            RequestFiles = await ProcessFileAttachments(SelectedFiles) // Обработка прикреплённых файлов
        };

        // Добавление заявки через API или базу данных
        if (await AddRequestWithFiles(request))
            // Если процесс добавления успешен, очистка списка выбранных файлов
            SelectedFiles.Clear();
        else
            // Если процесс добавления не успешен, отображение сообщения об ошибке
            await ShowErrorMessageAsync("Ошибка", "Не удалось добавить заявку. Пожалуйста, попробуйте еще раз.");
    }

// Асинхронный метод для добавления заявки с файлами
    private async Task<bool> AddRequestWithFiles(Request request)
    {
        // Добавление заявки в репозиторий и получение её уникального идентификатора (ID)
        var requestId = await RequestRepository.AddRequestAsync(request);
        // Проверка успешности добавления заявки: если ID меньше или равен 0, возвращаем false
        if (requestId <= 0) return false;

        // Добавление файлов для соответствующей заявки
        await RequestRepository.AddRequestFilesAsync(requestId, request.RequestFiles);

        // Очистка формы после успешного добавления заявки
        ClearRequestForm();
        // Отображение таблицы заявок (переключение видимости элементов интерфейса)
        ShowSingleGrid(RequestsGrid);
        // Загрузка и обновление списка заявок
        LoadUserRequests();

        // Возвращаем true, указывая на успешное выполнение операции
        return true;
    }

// Асинхронный метод для обработки прикреплённых файлов
    private static async Task<ObservableCollection<RequestFile>> ProcessFileAttachments(
        ObservableCollection<string> nameList, int maxDegreeOfParallelism = 4)
    {
        // Проверка на наличие выбранных файлов
        if (nameList.Count == 0)
            return new ObservableCollection<RequestFile>(); // Если файлы не выбраны, возвращаем пустой список

        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var resultBag = new ConcurrentBag<RequestFile>();

        var tasks = nameList.Select(async filePath =>
        {
            await semaphore.WaitAsync();
            try
            {
                var fileData = await File.ReadAllBytesAsync(filePath);
                resultBag.Add(new RequestFile
                {
                    FileName = Path.GetFileName(filePath),
                    FileData = fileData
                });
            }
            catch (Exception ex)
            {
                // Здесь можно логировать ошибку или предпринять другие действия
                Console.WriteLine($"Failed to read file {filePath}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return new ObservableCollection<RequestFile>(resultBag.OrderBy(rf => nameList.IndexOf(rf.FileName)));
    }

    private static Task<(bool, string)> ValidateInput(string requestName, string? requestType, string requestNotes)
    {
        if (string.IsNullOrWhiteSpace(requestName) || string.IsNullOrWhiteSpace(requestType))
            return Task.FromResult((false, "Пожалуйста, заполните все обязательные поля."));

        return !IsValidRequestName(requestName)
            ? Task.FromResult((false, "Название заявки содержит недопустимые символы."))
            : Task.FromResult(!IsValidRequestNotes(requestNotes)
                ? (false, "Примечания содержат недопустимые символы.")
                : (true, ""));
    }

    private void ShowSingleGrid(UIElement gridToShow)
    {
        var allGrids = new List<UIElement> { RequestsGrid, UserDataGrid, NewRequestGrid, DetailsGrid };

        foreach (var grid in allGrids) grid.Visibility = grid == gridToShow ? Visibility.Visible : Visibility.Collapsed;
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
                textBox.BorderThickness = isEditing ? new Thickness(0, 0, 0, 1) : new Thickness(0);
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
            if (element is TextBox textBox)
                textBox.BorderBrush = Brushes.Red;
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

    private void UserRequestsDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (UserRequestsDataGrid.SelectedItem is Request selectedRequest)
        {
            RequestsGrid.Visibility = Visibility.Hidden;
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
        // Сохранить исходные данные
        _originalName = NameTextBox.Text;
        _originalNotes = NotesTextBox.Text;

        // Сделать поля редактируемыми
        NameTextBox.IsReadOnly = false;
        NameTextBox.BorderThickness = new Thickness(0, 0, 0, 1);
        NotesTextBox.IsReadOnly = false;
        NotesTextBox.BorderThickness = new Thickness(0, 0, 0, 1);

        // Показать новые кнопки и скрыть стандартные
        SaveButtonDetailsGrid.Visibility = Visibility.Visible;
        CancelButtonDetailsGrid.Visibility = Visibility.Visible;
        AddFileButtonDetailsGrid.Visibility = Visibility.Visible;

        // Скрыть кнопки "Изменить" и "Удалить заявку"
        EditButtonDetailsGrid.Visibility = Visibility.Hidden;
        DeleteRequestButtonDetailsGrid.Visibility = Visibility.Hidden;
    }

    private async void DeleteRequestButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var requestId = int.Parse(button.Tag.ToString());

        // Определение настроек диалога
        var mySettings = new MetroDialogSettings
        {
            AffirmativeButtonText = "Да",
            NegativeButtonText = "Нет",
            AnimateShow = false,
            AnimateHide = false
        };

        // Диалог подтверждения с пользовательскими настройками
        var confirmResult = await this.ShowMessageAsync("Подтверждение удаления",
            "Вы уверены, что хотите удалить эту заявку и все связанные файлы?",
            MessageDialogStyle.AffirmativeAndNegative, mySettings);

        if (confirmResult == MessageDialogResult.Affirmative)
        {
            var result = await RequestRepository.DeleteRequestWithFilesAsync(requestId);
            if (result)
            {
                await ShowErrorMessageAsync("Удаление завершено", "Заявка и все связанные файлы успешно удалены.");
                ShowSingleGrid(RequestsGrid); // Предполагается, что это обновляет или скрывает интерфейс
                LoadUserRequests(); // Обновление списка заявок
            }
            else
            {
                await ShowErrorMessageAsync("Ошибка удаления", "Произошла ошибка при удалении заявки.");
            }
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
        // Проверяем, что Roleid текущего пользователя равен 3
        if (_currentUser.RoleId != 3)
        {
            // Показываем сообщение об ошибке
            await ShowErrorMessageAsync("Ошибка", "Вы не являетесь автором этой заявки.");
            return; // Прекращаем выполнение метода
        }

        var button = sender as Button;
        var fileName = button?.Tag?.ToString();

        if (fileName == null)
        {
            await ShowErrorMessageAsync("Ошибка", "Невозможно определить имя файла для удаления.");
            return;
        }
        
        // Создание и показ диалога для подтверждения
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

        // Если пользователь не подтвердил удаление, прерываем выполнение метода
        if (result != MessageDialogResult.Affirmative)
        {
            return;
        }

        DependencyObject parent = button;
        Request request = null;

        // Поднимаемся по дереву элементов, пока не найдем DataContext типа Request
        while (parent != null && request == null)
        {
            parent = VisualTreeHelper.GetParent(parent) ?? LogicalTreeHelper.GetParent(parent);
            if (parent is FrameworkElement frameworkElement)
                request = frameworkElement.DataContext as Request;
        }

        if (request != null)
        {
            var requestId = request.RequestId;
            var fileToRemove = request.RequestFiles.FirstOrDefault(f => f.FileName == fileName);

            if (fileToRemove != null && RequestRepository.DeleteFileFromDatabase(requestId, fileName))
            {
                request.RequestFiles.Remove(fileToRemove);
                RefreshFilesListUi();
            }
            else
            {
                await ShowErrorMessageAsync("Ошибка", "Ошибка при удалении файла из базы данных.");
            }
        }
        else
        {
            await ShowErrorMessageAsync("Ошибка", "Невозможно определить заявку для удаления файла.");
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
        var button = (Button)sender;
        var requestId = int.Parse(button.Tag.ToString());

        var requestName = NameTextBox.Text.Trim();
        var requestType = StatusTextBox.Text;
        var requestNotes = NotesTextBox.Text.Trim();

        var validationResult = await ValidateInput(requestName, requestType, requestNotes);
        if (!validationResult.Item1) // Если валидация не пройдена
        {
            // Отображение сообщения об ошибке
            await ShowErrorMessageAsync("Ошибка", validationResult.Item2);
            return;
        }

        var updatedRequest = new Request
        {
            RequestId = requestId,
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = _currentUser.UserId // ID пользователя
        };

        try
        {
            // Обновляем данные заявки
            await RequestRepository.UpdateRequestAsync(updatedRequest);

            // Проверяем, есть ли выбранные файлы для добавления
            if (SelectedFilesDetailsGrid.Count > 0)
            {
                // Обработка прикреплённых файлов
                var attachedFiles = await ProcessFileAttachments(SelectedFilesDetailsGrid);

                if (attachedFiles.Any())
                {
                    // Добавляем новые файлы к заявке
                    await RequestRepository.AddRequestFilesAsync(requestId, attachedFiles);

                    // Получаем текущий объект заявки из UI
                    if (DetailsGrid.DataContext is Request currentRequest)
                    {
                        // Добавляем файлы в локальный список файлов заявки
                        foreach (var file in attachedFiles) currentRequest.RequestFiles.Add(file);

                        DisableEditing();
                        // Обновляем UI
                        RefreshFilesListUi();
                    }
                }
            }

            await ShowErrorMessageAsync("Успех", "Изменения успешно сохранены.");
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync("Ошибка", $"Ошибка при сохранении изменений: {ex.Message}");
        }
    }

    private void CancelButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        DisableEditing();
    }

    private void DisableEditing()
    {
        // Восстановление исходных значений при отмене редактирования полей
        NameTextBox.Text = _originalName;
        NotesTextBox.Text = _originalNotes;
        SelectedFilesDetailsGrid.Clear();

        // Скрыть кнопки "Сохранить", "Отмена" и "Добавить файл"
        SaveButtonDetailsGrid.Visibility = Visibility.Hidden;
        CancelButtonDetailsGrid.Visibility = Visibility.Hidden;
        AddFileButtonDetailsGrid.Visibility = Visibility.Hidden;

        if (_currentUser.RoleId == 3)
        {
            // Вернуть поля в неизменяемое состояние
            NameTextBox.IsReadOnly = true;
            NameTextBox.BorderThickness = new Thickness(0);
            NotesTextBox.IsReadOnly = false;
            NotesTextBox.BorderThickness = new Thickness(0);

            // Показать кнопки "Изменить" и "Удалить заявку"
            EditButtonDetailsGrid.Visibility = Visibility.Visible;
            DeleteRequestButtonDetailsGrid.Visibility = Visibility.Visible;
        }
        else
        {
            // Показать кнопки "Изменить" и "Удалить заявку"
            EditButtonDetailsGrid.Visibility = Visibility.Hidden;
            DeleteRequestButtonDetailsGrid.Visibility = Visibility.Hidden;
        }
    }

    private async void AddFileButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        // Создание и настройка диалога выбора файлов
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Office Files|*.doc;*.docx;*.xls;*.xlsx;|Text Files|*.txt|Drawings|*.dwg;*.dxf|All Files|*.*",
            Multiselect = true
        };

        // Отображение диалога выбора файлов и проверка на успешное закрытие с выбранными файлами
        if (openFileDialog.ShowDialog() != true) return;

        // Создание списка допустимых расширений файлов
        var validExtensions = new HashSet<string> { ".doc", ".docx", ".xls", ".xlsx", ".txt", ".dwg", ".dxf" };

        // Разделение выбранных файлов на допустимые и недопустимые по расширению
        var validFiles = openFileDialog.FileNames
            .Where(filePath => validExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant())).ToList();
        var invalidFiles = openFileDialog.FileNames.Except(validFiles).ToList();

        // Добавление допустимых файлов в коллекцию выбранных файлов
        foreach (var validFile in validFiles) SelectedFilesDetailsGrid.Add(validFile);

        // Если нет недопустимых файлов, прекратить выполнение функции
        if (invalidFiles.Count == 0) return;

        // Создание сообщения об ошибках для недопустимых файлов
        var message =
            $"Следующие файлы имеют недопустимое расширение и не были добавлены:\n\n{string.Join("\n", invalidFiles.Select(Path.GetFileName))}";
        // Отображение сообщения об ошибке
        await ShowErrorMessageAsync("Недопустимые файлы", message);
    }

    private async void AcceptRequestManagerButtonDetailsGrid_OnClick(object sender, RoutedEventArgs e)
    {
        const int acceptedStatusId = 4;

        if (sender is not Button button)
        {
            await ShowErrorMessageAsync("Ошибка", "Ошибка в источнике события.");
            return;
        }

        if (!int.TryParse(button.Tag.ToString(), out var requestId))
        {
            await ShowErrorMessageAsync("Ошибка", "Некорректный идентификатор заявки.");
            return;
        }

        try
        {
            var isUpdated = await RequestRepository.ChangeRequestStatus(requestId, acceptedStatusId);
            if (isUpdated)
            {
                await ShowErrorMessageAsync("Успех", "Заявка принята!");
                ShowSingleGrid(RequestsGrid);
                LoadUserRequests();
            }
            else
            {
                await ShowErrorMessageAsync("Ошибка", "Заявка не найдена!");
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

        if (!(sender is Button button))
        {
            await ShowErrorMessageAsync("Ошибка", "Ошибка в источнике события.");
            return;
        }

        if (!int.TryParse(button.Tag.ToString(), out var requestId))
        {
            await ShowErrorMessageAsync("Ошибка", "Некорректный идентификатор заявки.");
            return;
        }

        string input = null;
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

            // Если пользователь нажал "Отмена" или закрыл окно
            if (input == null) return; // Просто выходим из метода, ничего не делаем

            if (string.IsNullOrWhiteSpace(input))
                await ShowErrorMessageAsync("Ошибка", "Поле причины отклонения не может быть пустым.");
        } while (string.IsNullOrWhiteSpace(input));

        try
        {
            // Обновляем статус и причину отклонения заявки в базе данных
            var isUpdated = await RequestRepository.ChangeRequestStatusAndReason(requestId, rejectedStatusId, input);
            if (isUpdated)
                await ShowErrorMessageAsync("Успех", "Заявка успешно отклонена.");
            else
                await ShowErrorMessageAsync("Ошибка", "Заявка не найдена.");
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync("Исключение", $"Произошла ошибка: {ex.Message}");
        }
    }
}