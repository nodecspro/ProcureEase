#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public class InvitationCodesViewModel : INotifyPropertyChanged
{
    private ObservableCollection<InvitationCode> _invitationCodes;

    public InvitationCodesViewModel()
    {
        InvitationCodes = new ObservableCollection<InvitationCode>();
        LoadData();
    }


    public ObservableCollection<InvitationCode> InvitationCodes
    {
        get => _invitationCodes;
        set
        {
            _invitationCodes = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void LoadData()
    {
        var connectionString = AppSettings.ConnectionString;

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            // Запрос для получения данных из invitation_codes
            var query = @"
                SELECT ic.code, ic.role_id, r.role_name, ic.organization_id, o.organization_full_name, ic.expiration_date
                FROM invitation_codes ic
                JOIN roles r ON ic.role_id = r.role_id
                JOIN suppliers o ON ic.organization_id = o.supplier_id";

            var command = new MySqlCommand(query, connection);
            var reader = command.ExecuteReader();

            while (reader.Read())
                InvitationCodes.Add(new InvitationCode
                {
                    Code = reader.GetString(0),
                    RoleId = reader.GetInt32(1),
                    RoleName = reader.GetString(2), // Получение названия роли
                    OrganizationId = reader.GetInt32(3),
                    OrganizationName = reader.GetString(4), // Получение названия организации
                    ExpirationDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                });
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}