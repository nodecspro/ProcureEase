#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;

#endregion

namespace ProcureEase.Classes;

public class SuppliersViewModel : INotifyPropertyChanged
{
    public SuppliersViewModel()
    {
        Suppliers = new ObservableCollection<Supplier>();
        LoadSuppliers();
    }

    public ObservableCollection<Supplier> Suppliers { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void LoadSuppliers()
    {
        var connectionString = AppSettings.ConnectionString;

        var suppliers = new List<Supplier>();
        var requestTypes = new Dictionary<int, string>();

        using (var conn = new MySqlConnection(connectionString))
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
}