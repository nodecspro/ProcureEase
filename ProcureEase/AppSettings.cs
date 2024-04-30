#region

using System.Configuration;

#endregion

namespace ProcureEase;

public static class AppSettings
{
    public static readonly string ConnectionString =
        ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;
}