using IVEBA_Web_App.Models;
using IVEBA_Web_App.Services.ArchivoME13;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog desde appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Leer configuraci�n de Serilog desde appsettings.json
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    // Registrar Serilog como el proveedor de logging
    builder.Host.UseSerilog();

    // Agregar servicios
    builder.Services.AddRazorPages();
    builder.Services.AddSingleton<iGeneracionArchivoME13, GeneracionArchivoME13>();
    builder.Services.Configure<DTO_IVEBA_Web_AppConfiguraciones>(builder.Configuration.GetSection("IVEBA_Web_AppConfiguraciones"));

    var app = builder.Build();

    // Configurar el pipeline de solicitudes HTTP
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();
    app.MapRazorPages();

    // Correr la aplicaci�n
    app.Run();
}
catch (Exception ex)
{
    // Registrar errores de inicializaci�n
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    // Cerrar el log al finalizar la aplicaci�n
    Log.CloseAndFlush();
}
