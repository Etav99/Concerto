﻿using Concerto.Server.Extensions;

namespace Concerto.Server.Settings;

public static class AppSettings
{
    public static class Environment
    {
        public static bool Docker => EnvironmentHelper.GetVariable("ASPNETCORE_DOCKER").Equals("true");
        public static bool Remote => EnvironmentHelper.GetVariable("ASPNETCORE_REMOTE").Equals("true");
    }
    
    public static class Oidc
    {
        public static string MetadataAddress => EnvironmentHelper.GetVariable("OIDC_METADATA_ADDRESS");
        public static string Authority => EnvironmentHelper.GetVariable("OIDC_AUTHORITY");
        public static string ClientAuthority => EnvironmentHelper.GetVariable("OIDC_CLIENT_AUTHORITY");
        public static string ClientPostLogoutRedirectUrl => EnvironmentHelper.GetVariable("OIDC_CLIENT_POST_LOGOUT_REDIRECT_URL");
        public static string Audience => EnvironmentHelper.GetVariable("OIDC_AUDIENCE");
        public static bool AcceptAnyServerCertificateValidator => EnvironmentHelper.GetVariable("OIDC_ACCEPT_ANY_SERVER_CERTIFICATE_VALIDATOR").Equals("true");

        public static string ServerClientId = EnvironmentHelper.GetVariable("SERVER_CLIENT_ID");
        public static string ServerClientSecret = EnvironmentHelper.GetVariable("SERVER_CLIENT_SECRET");
        public static string OidcAdminRestApiBaseUrl = EnvironmentHelper.GetVariable("OIDC_ADMIN_REST_API_BASE");


        
    }

    public static class Database
    {
        public static string DbString => EnvironmentHelper.GetVariable("DB_STRING");
    }


}
