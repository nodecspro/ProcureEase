using System.Configuration;

namespace ProcureEase
{
    public static class AppSettings
    {
        public static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["ProcureEaseDB"].ConnectionString;
    }
}
