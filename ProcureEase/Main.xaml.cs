﻿#region

using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class Main : MetroWindow
{
    private const string Database = "procureease";
    private const string Server = "localhost";
    private const string Uid = "root";
    private const string Password = "";
    private readonly string _login; // Хранит значение логина

    public Main(string login)
    {
        _login = login; // Сохранить значение логина
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        // Загрузить имя пользователя из базы данных
        UsernameTextBlock.Text = GetUserByUsername(_login);
    }

    private string GetUserByUsername(string username)
    {
        var connectionString = $"server={Server};database={Database};uid={Uid};password={Password}";

        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        var query = "SELECT username FROM users WHERE username = @username"; // Измените запрос при необходимости

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@username", username);

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? reader.GetString("username")
            : string.Empty; // Вернуть пустую строку, если имя пользователя не найдено
    }

    private void UsernameTextBlock_Click(object sender, RoutedEventArgs e)
    {
        // Переключение видимости сеток
        RequestsGrid.Visibility =
            RequestsGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        UserDataGrid.Visibility =
            UserDataGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CreateRequest_Click(object sender, RoutedEventArgs e)
    {
        // Обработчик события для кнопки создания запроса
    }

    private void OnMainWindowClosed(object sender, EventArgs e)
    {
        // Закрытие всех окон при закрытии основного окна
        foreach (var window in Application.Current.Windows) ((Window)window).Close();
    }
}