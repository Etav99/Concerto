using Blazored.LocalStorage;
using Concerto.Client;
using Concerto.Client.Extensions;
using Concerto.Client.Services;
using Concerto.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
// Add HTTP Client with base address and authorization handler 
if (builder.HostEnvironment.Environment == "DevelopmentStandalone")
{
	baseAddress = new Uri("https://localhost:7001/concerto/app/");
	builder.Services.AddHttpClient("WebAPI", client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler(sp => {
		var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
			.ConfigureHandler(new[] { baseAddress.ToString() });
		return handler;
	});
}
else
{
	builder.Services.AddHttpClient("WebAPI", client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
}



// Register it as scoped, each service will use the same HTTP client provided by DI
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebAPI"));
AppSettingsService appSettingsService = new AppSettingsService(new HttpClient { BaseAddress = baseAddress });
await appSettingsService.FetchAppSettings();
var appSettings = appSettingsService.AppSettings;
builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>(sp => appSettingsService);
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IForumClient, ForumClient>();
builder.Services.AddScoped<IStorageClient, StorageClient>();
builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBreadcrumbsService, BreadcrumbsService>();
// builder.Services.AddScoped<ClientNotificationService, ClientNotificationService>();


builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddOidcAuthentication(options =>
	{
		options.ProviderOptions.Authority = appSettings.AuthorityUrl;
		options.ProviderOptions.ClientId = "concerto-client";
		options.ProviderOptions.ResponseType = "code";
		options.ProviderOptions.PostLogoutRedirectUri = appSettings.PostLogoutUrl;
		options.ProviderOptions.DefaultScopes.Add("roles");
		options.AuthenticationPaths.RemoteRegisterPath = $"{appSettings.AuthorityUrl}/login-actions/registration";
		options.UserOptions.RoleClaim = "role";
	})
	.AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, RemoteUserAccount, CustomAccountFactory>();

builder.Services.AddAuthorizationCore(options =>
{
	options.AddPolicy(AuthorizationPolicies.IsAuthenticated.Name, AuthorizationPolicies.IsAuthenticated.Policy());
	options.AddPolicy(AuthorizationPolicies.IsVerified.Name, AuthorizationPolicies.IsVerified.Policy());
	options.AddPolicy(AuthorizationPolicies.IsNotVerified.Name, AuthorizationPolicies.IsNotVerified.Policy());
	options.AddPolicy(AuthorizationPolicies.IsAdmin.Name, AuthorizationPolicies.IsAdmin.Policy());
	options.AddPolicy(AuthorizationPolicies.IsTeacher.Name, AuthorizationPolicies.IsTeacher.Policy());
	options.DefaultPolicy = AuthorizationPolicies.IsVerified.Policy();
});


await builder.Build().RunAsync();