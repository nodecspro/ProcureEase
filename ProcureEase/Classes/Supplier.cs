namespace ProcureEase.Classes;

public class Supplier
{
    public int SupplierId { get; set; }
    public string Inn { get; set; }
    public string Kpp { get; set; }
    public string OrganizationFullName { get; set; }
    public string Supervisor { get; set; }
    public string Email { get; set; }
    public string ContactNumber { get; set; }
    public int? RequestTypeId { get; set; }
}