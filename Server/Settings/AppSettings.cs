using Concerto.Server.Extensions;

namespace Concerto.Server.Settings;

public static class AppSettings
{
    public static class Environment
    {
        public static bool Docker => EnvironmentHelper.GetVariable("ASPNETCORE_DOCKER").Equals("true");
        public static bool Remote => EnvironmentHelper.GetVariable("ASPNETCORE_REMOTE").Equals("true");
    }
    
    public static class Jwt
    {
        public static string MetadataAddress => EnvironmentHelper.GetVariable("JWT_METADATA_ADDRESS");
        public static string Authority => EnvironmentHelper.GetVariable("JWT_AUTHORITY");
        public static string Audience => EnvironmentHelper.GetVariable("JWT_AUDIENCE");
        public static bool AcceptAnyServerCertificateValidator => EnvironmentHelper.GetVariable("JWT_ACCEPT_ANY_SERVER_CERTIFICATE_VALIDATOR").Equals("true");
    }

    public static class Database
    {
        public static string DbString => EnvironmentHelper.GetVariable("DB_STRING");
    }


}
