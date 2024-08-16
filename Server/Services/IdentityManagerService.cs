using Concerto.Server.Data.Models;
using Concerto.Server.Settings;
using Concerto.Shared.Constants;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Role = Concerto.Shared.Models.Dto.Role;


namespace Concerto.Server.Services;


public class IdentityManagerService
{
	private readonly HttpClient _httpClient = new HttpClient();
	private readonly ILogger<IdentityManagerService> _logger;

	private static readonly FormUrlEncodedContent _getTokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string> {
		{ "client_id", AppSettings.Oidc.ServerClientId },
		{ "client_secret", AppSettings.Oidc.ServerClientSecret },
		{ "grant_type", "client_credentials" }
	});

	public IdentityManagerService(ILogger<IdentityManagerService> logger)
	{
		_logger = logger;
	}

	public async Task LogoutUser(Guid subjectId)
	{
		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{subjectId}/logout";
		var logoutResponse = await _httpClient.PostAsync(url, null);
	}

	public async Task<IEnumerable<Dto.UserIdentity>> GetUnverifiedUsers()
	{
		return (await GetGroupUsers(Groups.Unverified)).Select(u => u.ToUserIdentity(Role.Unverified));
	}

	public async Task VerifyUser(Guid subjectId)
	{
		await RemoveFromGroup(subjectId.ToString(), Groups.Unverified);
		await LogoutUser(subjectId);
	}

	public async Task<User> UserFromIdentity(Guid subjectId)
	{
		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{subjectId}";
		var getUserResponse = await _httpClient.GetAsync(url);
		var userRepresentation = JsonConvert.DeserializeObject<UserRepresentation>(await getUserResponse.Content.ReadAsStringAsync());

		if (userRepresentation is null)
			throw new Exception("User not found");

		return new User
		{
			Id = subjectId,
			Username = userRepresentation.Username,
			FirstName = userRepresentation.FirstName,
			LastName = userRepresentation.LastName,
		};

	}

	public async Task SetRole(Guid subjectId, Role role)
	{
		switch (role)
		{
			case Role.Unverified:
				throw new NotImplementedException();
			case Role.Admin:
				await AddToGroup(subjectId.ToString(), Groups.Admins);
				await RemoveFromGroup(subjectId.ToString(), Groups.Moderators);
				break;
			case Role.Teacher:
				await AddToGroup(subjectId.ToString(), Groups.Moderators);
				await RemoveFromGroup(subjectId.ToString(), Groups.Admins);
				break;
			case Role.User:
				await RemoveFromGroup(subjectId.ToString(), Groups.Admins);
				await RemoveFromGroup(subjectId.ToString(), Groups.Moderators);
				break;
		}
	}

	public async Task DeleteUser(Guid subjectId)
	{
		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{subjectId}";
		var deleteUserResult = await _httpClient.DeleteAsync(url);
	}
	private async Task GetApiToken()
	{
		var url = $"{AppSettings.Oidc.Authority}/protocol/openid-connect/token";
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		var getTokenResponse = await _httpClient.PostAsync(url, _getTokenRequestContent);
		var token = JsonNode.Parse(await getTokenResponse.Content.ReadAsStringAsync());
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!["access_token"]!.ToString());
	}

	private async Task<string> GetGroupId(string groupName)
	{
		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/groups?search={groupName}";
		var unverifiedGroupSearchResponse = await _httpClient.GetAsync(url);
		unverifiedGroupSearchResponse.EnsureSuccessStatusCode();
		var unverifiedGroupSearchResult = JsonConvert.DeserializeObject<List<GroupRepresentation>>(await unverifiedGroupSearchResponse.Content.ReadAsStringAsync());
		if (unverifiedGroupSearchResult == null || unverifiedGroupSearchResult.Count == 0)
			throw new Exception("Group not found");
		if (unverifiedGroupSearchResult.Count != 1)
			throw new Exception("Multiple groups found");
		return unverifiedGroupSearchResult[0].Id;
	}

	private async Task AddToGroup(string userId, string groupName)
	{
		var groupId = await GetGroupId(groupName);
		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{userId}/groups/{groupId}";
		var response = await _httpClient.PutAsync(url, null);
		response.EnsureSuccessStatusCode();
	}
	private async Task RemoveFromGroup(string userId, string groupName)
	{
		var groupId = await GetGroupId(groupName);
		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users/{userId}/groups/{groupId}";
		var response = await _httpClient.DeleteAsync(url);
		response.EnsureSuccessStatusCode();
	}

	private async Task<string> GetClientId(string clientName)
	{
		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/clients?clientId={clientName}";
		var clientSearchResponse = await _httpClient.GetAsync(url);
		var clientSearchResult = JsonConvert.DeserializeObject<List<GroupRepresentation>>(await clientSearchResponse.Content.ReadAsStringAsync());
		if (clientSearchResult == null || clientSearchResult.Count == 0)
			throw new Exception("Client not found");
		if (clientSearchResult.Count != 1)
			throw new Exception("Multiple clients found");
		return clientSearchResult[0].Id;
	}

	private async Task<IEnumerable<UserRepresentation>> GetGroupUsers(string groupName)
	{
		var groupId = await GetGroupId(groupName);

		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/groups/{groupId}/members";
		var groupUsersResponse = await _httpClient.GetAsync(url);
		groupUsersResponse.EnsureSuccessStatusCode();

		return JsonConvert.DeserializeObject<List<UserRepresentation>>(await groupUsersResponse.Content.ReadAsStringAsync()) ?? Enumerable.Empty<UserRepresentation>();
	}

	public async Task<IEnumerable<Dto.UserIdentity>> GetUsers(Guid subjectId)
	{
		var unverified = (await GetGroupUsers(Groups.Unverified)).Select(u => u.Id).ToHashSet();
		var moderator = (await GetGroupUsers(Groups.Moderators)).Select(u => u.Id).ToHashSet();
		var admins = (await GetGroupUsers(Groups.Admins)).Select(u => u.Id).ToHashSet();

		await GetApiToken();
		var url = $"{AppSettings.Oidc.OidcAdminRestApiBaseUrl}/users";
		var usersResult = await _httpClient.GetAsync(url);
		usersResult.EnsureSuccessStatusCode();

		var users = JsonConvert.DeserializeObject<List<UserRepresentation>>(await usersResult.Content.ReadAsStringAsync());
		if (users == null || !users.Any()) return Enumerable.Empty<Dto.UserIdentity>();

		var userIdentities = new List<Dto.UserIdentity>(users.Count);
		foreach (var user in users)
		{
			if (user.Id == subjectId)
				continue;
			Role role;
			if (unverified.Contains(user.Id))
				role = Role.Unverified;
			else if (admins.Contains(user.Id))
				role = Role.Admin;
			else if (moderator.Contains(user.Id))
				role = Role.Teacher;
			else
				role = Role.User;

			var userIdentity = new Dto.UserIdentity(user.Id, user.Username, user.FirstName, user.LastName, user.Email, user.EmailVerified, role);
			userIdentities.Add(userIdentity);
		}
		return userIdentities;
	}


	private class GroupRepresentation
	{
		public string Id { get; set; } = null!;
		public string Name { get; set; } = null!;
	}
	private class UserRepresentation
	{
		public Guid Id { get; set; }
		public string FirstName { get; set; } = null!;
		public string LastName { get; set; } = null!;
		public string Username { get; set; } = null!;
		public string Email { get; set; } = null!;
		public bool EmailVerified { get; set; }
		public Dto.UserIdentity ToUserIdentity(Role role)
		{
			return new Dto.UserIdentity(Id, Username, FirstName, LastName, Email, EmailVerified, role);
		}
	}
}

