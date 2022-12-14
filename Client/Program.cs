global using Dto = Concerto.Shared.Models.Dto;
using Blazored.LocalStorage;
using Concerto.Client;
using Concerto.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using static System.Net.WebRequestMethods;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);


IAppSettingsClient appSettingsClient = new AppSettingsClient(new HttpClient() { BaseAddress = baseAddress });
var clientAppSettings = await appSettingsClient.GetClientAppSettingsAsync();

builder.Services.AddHttpClient("WebAPI",
		client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services
	.AddHttpClient<IForumClient, ForumClient>(client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services
	.AddHttpClient<ICourseClient, CourseClient>(client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services
	.AddHttpClient<ISessionClient, SessionClient>(client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services
	.AddHttpClient<IStorageClient, StorageClient>(client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services
	.AddHttpClient<IUserClient, UserClient>(client => client.BaseAddress = baseAddress)
	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
	.CreateClient("WebAPI"));

builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICourseService, CourseService>();
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

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
	.CreateLogger<Program>();

await host.RunAsync();