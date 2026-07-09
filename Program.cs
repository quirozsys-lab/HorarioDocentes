using HorarioDocentes.Data;
using HorarioDocentes.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddRazorPages(options =>
{
    // Todo el sitio requiere haber iniciado sesión, salvo la página de Login
    // (y la de Error, para no bloquear el aviso de error si la sesión expiró).
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
});
builder.Services.AddServerSideBlazor(options =>
{
    // TEMPORAL para depurar: muestra el detalle real de la excepción en el navegador.
    // Puedes quitarlo cuando ya no lo necesites.
    options.DetailedErrors = true;
});
builder.Services.AddMudServices();

builder.Services.AddScoped<ExcelParserService>();
builder.Services.AddScoped<HorarioService>();

var app = builder.Build();

// Crea las tablas si no existen (la base de datos ya debe existir en Neon).
var connString = builder.Configuration.GetConnectionString("DefaultConnection")!;
DbInitializer.Initialize(connString);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Login");
});

app.MapBlazorHub().RequireAuthorization();
app.MapFallbackToPage("/_Host");

// Railway asigna el puerto por variable de entorno PORT; sin esto la app
// escucha en el puerto por defecto y Railway no la encuentra.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();
