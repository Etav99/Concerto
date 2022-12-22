using Blazored.LocalStorage;
using Concerto.Client;
using Concerto.Client.Services;
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
builder.Services.AddHttpClient("WebAPI", client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
// Register it as scoped, each service will use the same HTTP client provided by DI
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WebAPI"));


AppSettingsService appSettingsService = new AppSettingsService(new HttpClient { BaseAddress = baseAddress });
await appSettingsService.FetchAppSetings();
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


await builder.Build().RunAsync();