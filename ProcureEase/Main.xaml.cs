#region

using System.Configuration;
using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase;

public partial class Main : MetroWindow
{
    private static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;

    public Main(string login)
    {
        // Сохранить значение логина
        InitializeComponent();
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        ThemeManager.Current.SyncTheme();

        // Загрузить имя пользователя из базы данных
        UsernameTextBlock.Text = GetUserByUsername(login);
    }

    private static string GetUserByUsername(string username)
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();

        const string query = "SELECT username FROM users WHERE username = @username"; // Измените запрос при необходимости

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