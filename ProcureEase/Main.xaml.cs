﻿#region

using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
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

    private void LoadUserRequests()
    {
        if (_currentUser != null)
            UserRequestsListView.ItemsSource = RequestRepository.GetUserRequests(_currentUser.UserId);
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

        var invalidFiles = new List<string>();

        foreach (var filePath in openFileDialog.FileNames)
        {
            var extension = Path.GetExtension(filePath);

            if (extension is ".doc" or ".docx" or ".xls" or ".xlsx" or ".txt" or ".dwg" or ".dxf")
                SelectedFiles.Add(filePath);
            else
                invalidFiles.Add(Path.GetFileName(filePath));
        }

        if (invalidFiles.Count <= 0) return;
        var message =
            $"Следующие файлы имеют недопустимое расширение и не были добавлены:\n\n{string.Join("\n", invalidFiles)}";
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

        if (_currentUser == null) return;

        var request = new Request
        {
            RequestName = requestName,
            RequestType = requestType,
            Notes = requestNotes,
            UserId = _currentUser.UserId
        };

        if (SelectedFiles.Count > 0)
        {
            var tasks = SelectedFiles.Select(async filePath =>
            {
                var fileData = await File.ReadAllBytesAsync(filePath);
                return new RequestFile
                {
                    FileName = Path.GetFileName(filePath),
                    FileData = fileData
                };
            });

            request.RequestFiles.AddRange(await Task.WhenAll(tasks));
        }

        var requestId = RequestRepository.AddRequest(request);
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
        _ = ShowErrorMessageAsync("File Clicked", $"You clicked on file: {fileName}");
    }
}