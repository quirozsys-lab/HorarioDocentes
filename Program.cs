using HorarioDocentes.Data;
using HorarioDocentes.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Railway asigna el puerto por variable de entorno PORT; sin esto la app
// escucha en el puerto por defecto y Railway no la encuentra.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();
