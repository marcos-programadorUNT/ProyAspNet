using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using Azure.Messaging.ServiceBus;
using log4net;
using log4net.Config;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// OBTENER conexión desde variable de entorno o appsettings.json
var serviceBusConnection =
Environment.GetEnvironmentVariable("ServiceBusConnectionString")
?? builder.Configuration.GetConnectionString("ServiceBusConnectionString")
?? throw new InvalidOperationException("ServiceBusConnectionString no está definida.");


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es-ES");
    options.SupportedCultures = new[] { new CultureInfo("es-ES") };
    options.SupportedUICultures = new[] { new CultureInfo("es-ES") };
});
builder.Services.AddDbContext<ProductContext>(options =>
{
    // Intenta primero obtener la conexión desde variable de entorno (App Service)
    var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

    // Si no existe en el entorno, usa la de appsettings.json (para desarrollo)
    if (string.IsNullOrEmpty(connectionString))
        connectionString = builder.Configuration.GetConnectionString("SqlConnectionString");

    options.UseSqlServer(connectionString);
}
);
// Registrar el cliente Service Bus como singleton
builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnection));
builder.Services.AddControllers();

// Cargar log4net.config
var repo = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(repo, new FileInfo("log4net.config"));

var app = builder.Build();
var log = LogManager.GetLogger(typeof(Program));
app.UseRequestLocalization(); 
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductContext>();
    dbContext.Database.Migrate();
}   

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) 
{
    app.MapOpenApi();
}
app.MapGet("/ping", () =>
{
    log.Info("Ping recibido");
    log.Warn("Advertencia de prueba");
    return Results.Ok("pong");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

