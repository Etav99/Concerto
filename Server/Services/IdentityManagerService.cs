namespace Concerto.Server.Services;

public class IdentityManagerService
{
	private readonly HttpClient _httpClient;
	private readonly ILogger<IdentityManagerService> _logger;

	public IdentityManagerService(ILogger<IdentityManagerService> logger, HttpClient httpClient)
	{
		_logger = logger;
		_httpClient = httpClient;
	}

	//public async Task AssignUserId(Guid subjectId, long userId)
	//{
	//    // Get admin REST API toke
	//    var url = $"{AppSettings.Oidc.Authority}/protocol/openid-connect/token";
	//    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
	//    var getTokenRequestContent = new Dictionary<string, string>();
	//    getTokenRequestContent.Add("client_id", AppSettings.Oidc.ServerClientId);
	//    getTokenRequestContent.Add("client_secret", AppSettings.Oidc.ServerClientSecret);
	//    getTokenRequestContent.Add("grant_type", "client_credentials");
	//    var getTokenResponse = await _httpClient.PostAsync(url, new FormUrlEncodedContent(getTokenRequestContent));
	//    var token = JsonNode.Parse(await getTokenResponse.Content.ReadAsStringAsync());

	//    // Add token to header
	//    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!["access_token"]!.ToString());

	//    // Logout user to force updated token generation
	//    url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{subjectId}/logout";
	//    var logoutResponse = await _httpClient.PostAsync(url, null);

	//    // Get user
	//    url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{subjectId}";
	//    var userResponse = await _httpClient.GetAsync(url);

	//    // Add userId attribute and update User
	//    var user = JsonNode.Parse(await userResponse.Content.ReadAsStringAsync());
	//    if (user!["attributes"] == null)
	//    {
	//        user!["attributes"] = new JsonArray();
	//    }
	//    user!["attributes"]!.AsArray().Add(new JsonObject { ["UserId"] = userId });
	//    var addAttributeStringContent = new StringContent(user.ToJsonString(), Encoding.UTF8, "application/json");
	//    url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{subjectId}";
	//    var addAttributeResponse = await _httpClient.PutAsync(url, addAttributeStringContent);
	//}
}

