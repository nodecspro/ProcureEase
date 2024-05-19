#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public class AddInvitationCodeViewModel : INotifyPropertyChanged
{
    private string _code;
    private ObservableCollection<string> _expirationTimes;
    private bool _isAdding;
    private ObservableCollection<Organization> _organizations;
    private ObservableCollection<Role> _roles;
    private string _selectedExpirationTime;
    private Organization _selectedOrganization;
    private Role _selectedRole;

    public AddInvitationCodeViewModel()
    {
        Roles = new ObservableCollection<Role>();
        Organizations = new ObservableCollection<Organization>();
        ExpirationTimes = new ObservableCollection<string> { "1 час", "3 часа", "5 часов", "12 часов", "24 часа" };
        LoadRolesAndOrganizations();
        GenerateUniqueCode();
        AddCommand = new RelayCommand(AddInvitationCode);
        GenerateNewCodeCommand = new RelayCommand(GenerateUniqueCode);
        _isAdding = false;
    }

    public ObservableCollection<Role> Roles
    {
        get => _roles;
        set
        {
            _roles = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Organization> Organizations
    {
        get => _organizations;
        set
        {
            _organizations = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> ExpirationTimes
    {
        get => _expirationTimes;
        set
        {
            _expirationTimes = value;
            OnPropertyChanged();
        }
    }

    public Role SelectedRole
    {
        get => _selectedRole;
        set
        {
            _selectedRole = value;
            OnPropertyChanged();
        }
    }

    public Organization SelectedOrganization
    {
        get => _selectedOrganization;
        set
        {
            _selectedOrganization = value;
            OnPropertyChanged();
        }
    }

    public string SelectedExpirationTime
    {
        get => _selectedExpirationTime;
        set
        {
            _selectedExpirationTime = value;
            OnPropertyChanged();
        }
    }

    public string Code
    {
        get => _code;
        set
        {
            _code = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddCommand { get; }
    public ICommand GenerateNewCodeCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void LoadRolesAndOrganizations()
    {
        var connectionString = AppSettings.ConnectionString;

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            // Загрузка ролей
            var roleQuery = "SELECT role_id, role_name FROM roles";
            var roleCommand = new MySqlCommand(roleQuery, connection);
            var roleReader = roleCommand.ExecuteReader();
            while (roleReader.Read())
                Roles.Add(new Role
                {
                    RoleId = roleReader.GetInt32(0),
                    RoleName = roleReader.GetString(1)
                });
            roleReader.Close();

            // Загрузка организаций
            var organizationQuery = "SELECT supplier_id, organization_full_name FROM suppliers";
            var organizationCommand = new MySqlCommand(organizationQuery, connection);
            var organizationReader = organizationCommand.ExecuteReader();
            while (organizationReader.Read())
                Organizations.Add(new Organization
                {
                    OrganizationId = organizationReader.GetInt32(0),
                    OrganizationFullName = organizationReader.GetString(1)
                });
        }
    }

    private void AddInvitationCode(object parameter)
    {
        if (_isAdding)
            return;

        _isAdding = true;

        var window = parameter as Window;

        // Проверка на пустые поля
        if (SelectedRole == null)
        {
            ShowMessage("Ошибка", "Пожалуйста, выберите роль.");
            _isAdding = false;
            return;
        }

        if (SelectedOrganization == null)
        {
            ShowMessage("Ошибка", "Пожалуйста, выберите организацию.");
            _isAdding = false;
            return;
        }

        if (string.IsNullOrEmpty(SelectedExpirationTime))
        {
            ShowMessage("Ошибка", "Пожалуйста, выберите время истечения.");
            _isAdding = false;
            return;
        }

        // Проверка на уникальность кода
        if (IsCodeExists(Code))
        {
            ShowMessage("Ошибка", "Этот код уже существует. Сгенерируйте новый код.");
            _isAdding = false;
            return;
        }

        var expirationDate = CalculateExpirationDate(SelectedExpirationTime);

        var connectionString = AppSettings.ConnectionString;

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            var query =
                "INSERT INTO invitation_codes (code, role_id, organization_id, expiration_date) VALUES (@Code, @RoleId, @OrganizationId, @ExpirationDate)";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Code", Code);
            command.Parameters.AddWithValue("@RoleId", SelectedRole.RoleId);
            command.Parameters.AddWithValue("@OrganizationId", SelectedOrganization.OrganizationId);
            command.Parameters.AddWithValue("@ExpirationDate", expirationDate);

            command.ExecuteNonQuery();
        }

        // Закрыть окно после добавления
        window.DialogResult = true;

        _isAdding = false;
    }

    private DateTime CalculateExpirationDate(string expirationTime)
    {
        return expirationTime switch
        {
            "1 час" => DateTime.Now.AddHours(1),
            "3 часа" => DateTime.Now.AddHours(3),
            "5 часов" => DateTime.Now.AddHours(5),
            "12 часов" => DateTime.Now.AddHours(12),
            "24 часа" => DateTime.Now.AddHours(24),
            _ => DateTime.Now
        };
    }

    private bool IsCodeExists(string code)
    {
        var connectionString = AppSettings.ConnectionString;

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            var query = "SELECT COUNT(*) FROM invitation_codes WHERE code = @Code";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Code", code);

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    private void GenerateUniqueCode(object parameter = null)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();

        string newCode;
        do
        {
            newCode = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        } while (IsCodeExists(newCode));

        Code = newCode;
    }

    private void ShowMessage(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; }
}

public class Organization
{
    public int OrganizationId { get; set; }
    public string OrganizationFullName { get; set; }
}

public class RelayCommand : ICommand
{
    private readonly Func<object, bool> _canExecute;
    private readonly Action<object> _execute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}