using Blazored.LocalStorage;
using Concerto.Client;
using Concerto.Shared.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

IAppSettingsClient appSettingsClient = new AppSettingsClient(new HttpClient { BaseAddress = baseAddress });
var clientAppSettings = await appSettingsClient.GetClientAppSettingsAsync();

// Add HTTP Client with base address and authorization handler 
builder.Services.AddHttpClient("WebAPI", client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
// Register it as scoped, each service will use the same HTTP client provided by DI
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebAPI"));


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
		options.ProviderOptions.Authority = clientAppSettings.AuthorityUrl;
		options.ProviderOptions.ClientId = "concerto-client";
		options.ProviderOptions.ResponseType = "code";
		options.ProviderOptions.PostLogoutRedirectUri = clientAppSettings.PostLogoutUrl;
		options.ProviderOptions.DefaultScopes.Add("roles");
		options.AuthenticationPaths.RemoteRegisterPath = $"{clientAppSettings.AuthorityUrl}/login-actions/registration";
		options.UserOptions.RoleClaim = "role";
	})
	.AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, RemoteUserAccount, CustomAccountFactory>();


await builder.Build().RunAsync();