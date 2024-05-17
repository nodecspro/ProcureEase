namespace ProcureEase.Classes;

public class InvitationCode
{
    public string Code { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.Now;
}