#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public class SuppliersViewModel : INotifyPropertyChanged
{
    private WorkType _selectedWorkType;

    public SuppliersViewModel()
    {
        Suppliers = new ObservableCollection<Supplier>();
        WorkTypes = new ObservableCollection<WorkType>();
        LoadSuppliers();
        _ = LoadWorkTypes();
    }

    public ObservableCollection<Supplier> Suppliers { get; set; }
    public ObservableCollection<WorkType> WorkTypes { get; set; }

    public WorkType SelectedWorkType
    {
        get => _selectedWorkType;
        set
        {
            _selectedWorkType = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static MySqlConnection GetConnection()
    {
        return new MySqlConnection(AppSettings.ConnectionString);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void LoadSuppliers()
    {
        var suppliers = new List<Supplier>();
        var requestTypes = new Dictionary<int, string>();

        using (var conn = GetConnection())
        {
            conn.Open();

            // Получение данных о типах заявок
            var requestTypeQuery = "SELECT idRequestType, name FROM request_type";
            var requestTypeCmd = new MySqlCommand(requestTypeQuery, conn);

            using (var reader = requestTypeCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32("idRequestType");
                    var name = reader.IsDBNull(reader.GetOrdinal("name")) ? null : reader.GetString("name");
                    requestTypes[id] = name;
                }
            }

            // Получение данных о поставщиках
            var supplierQuery = "SELECT * FROM suppliers";
            var supplierCmd = new MySqlCommand(supplierQuery, conn);

            using (var reader = supplierCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var supplier = new Supplier
                    {
                        SupplierId = reader.GetInt32("supplier_id"),
                        Inn = reader.IsDBNull(reader.GetOrdinal("inn")) ? null : reader.GetString("inn"),
                        Kpp = reader.IsDBNull(reader.GetOrdinal("kpp")) ? null : reader.GetString("kpp"),
                        OrganizationFullName = reader.IsDBNull(reader.GetOrdinal("organization_full_name"))
                            ? null
                            : reader.GetString("organization_full_name"),
                        Supervisor = reader.IsDBNull(reader.GetOrdinal("supervisor"))
                            ? null
                            : reader.GetString("supervisor"),
                        Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                        ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number"))
                            ? null
                            : reader.GetString("contact_number"),
                        RequestTypeName = reader.IsDBNull(reader.GetOrdinal("request_type_id"))
                            ? "Unknown"
                            : requestTypes.ContainsKey(reader.GetInt32("request_type_id"))
                                ? requestTypes[reader.GetInt32("request_type_id")]
                                : "Unknown"
                    };
                    suppliers.Add(supplier);
                }
            }
        }

        Suppliers.Clear();
        foreach (var supplier in suppliers) Suppliers.Add(supplier);
    }

    private async Task LoadWorkTypes()
    {
        var workTypes = new List<WorkType>();

        using (var conn = GetConnection())
        {
            await conn.OpenAsync(); // Асинхронное открытие соединения

            var query = "SELECT idRequestType, name FROM request_type";
            var cmd = new MySqlCommand(query, conn);

            using (var reader = await cmd.ExecuteReaderAsync()) // Асинхронное чтение данных
            {
                while (await reader.ReadAsync())
                {
                    var workType = new WorkType
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("idRequestType")),
                        Name = reader.GetString(reader.GetOrdinal("name"))
                    };
                    workTypes.Add(workType);
                }
            }
        }

        WorkTypes.Clear();
        foreach (var workType in workTypes) WorkTypes.Add(workType);
    }

    public async Task DeleteOrganizationAsync(int supplierId)
    {
        using (var conn = GetConnection())
        {
            await conn.OpenAsync();

            const string query = "DELETE FROM suppliers WHERE supplier_id = @supplierId";

            using (var command = new MySqlCommand(query, conn))
            {
                command.Parameters.Add("@supplierId", MySqlDbType.Int32).Value = supplierId;
                await command.ExecuteNonQueryAsync();
            }
        }

        var supplierToRemove = Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
        if (supplierToRemove != null) Suppliers.Remove(supplierToRemove);
    }
}