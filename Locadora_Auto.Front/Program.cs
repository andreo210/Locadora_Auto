using FluentValidation;
using Locadora_Auto.Front.Components;
using Locadora_Auto.Front.Extensions;
using Locadora_Auto.Front.Midlleware;
using Locadora_Auto.Front.Models.Layout;
using Locadora_Auto.Front.Models.OAuth;
using Locadora_Auto.Front.Services.Extensions;
using Locadora_Auto.Front.Services.Servicos.Login;
using Locadora_Auto.Front.Services.Servicos.Notificacao;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

//builder.Services.AddScoped<INotificationService, NotificationService>();
//builder.Services.AddScoped<NotificationService>();
//builder.Services.AddScoped<INotificationService>(sp =>
//    sp.GetRequiredService<NotificationService>());

builder.Services.AddServices(config);
//builder.Services.AddAuthorizationCore();
//builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAutenticacao(config);
builder.Services.AddScoped<MenuService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();
    var handler = new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer()
    };
    return new HttpClient
    {
        BaseAddress = new Uri(navigation.BaseUri)
    };
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<TokenRefreshMiddleware>();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/login", async (HttpContext context, ILoginService loginService, [FromForm] LoginRequest request) =>
{
    request.username = request.username.Replace(".", "").Replace("-", "");
    var result = await loginService.Login(request);

    if (result.Principal == null)
        return Results.Json(new { success = false, error = "Usuário ou senha inválidos" });

    var properties = new AuthenticationProperties
    {
        IsPersistent = true,
        AllowRefresh = true
    };

    properties.StoreTokens(new[]
    {
        new AuthenticationToken { Name = "access_token", Value = result.AccessToken },
        new AuthenticationToken { Name = "refresh_token", Value = result.RefreshToken }
    });

    await context.SignInAsync("Cookies", result.Principal, properties);

    // Retorna JSON em vez de redirect
    return Results.Json(new { success = true, redirect = "/" });
})
.DisableAntiforgery();

app.MapGet("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync();
   // context.Response.Cookies.Delete(".AspNetCore.Cookies");
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
