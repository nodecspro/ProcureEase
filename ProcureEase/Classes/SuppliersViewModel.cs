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
        Suppliers.Clear();

        using (var conn = new MySqlConnection(AppSettings.ConnectionString))
        {
            conn.Open();
            var query = "SELECT * FROM suppliers";
            var cmd = new MySqlCommand(query, conn);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var supplier = new Supplier
                    {
                        SupplierId = (int)reader["supplier_id"],
                        Inn = reader["inn"].ToString(),
                        Kpp = reader["kpp"].ToString(),
                        OrganizationFullName = reader["organization_full_name"].ToString(),
                        Supervisor = reader["supervisor"].ToString(),
                        Email = reader["email"].ToString(),
                        ContactNumber = reader["contact_number"].ToString()
                    };
                    Suppliers.Add(supplier);
                }
            }
        }
    }
}