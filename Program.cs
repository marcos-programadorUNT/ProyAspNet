using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using Azure.Messaging.ServiceBus;
using log4net;
using log4net.Config;
using System.Reflection;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

var builder = WebApplication.CreateBuilder(args);

// OBTENER conexión desde variable de entorno o appsettings.json
var serviceBusConnection =
Environment.GetEnvironmentVariable("ServiceBusConnectionString")
?? builder.Configuration.GetConnectionString("ServiceBusConnectionString")
?? throw new InvalidOperationException("ServiceBusConnectionString no está definida.");


// 1) Cadena de conexión al Storage Account (preferir variable de entorno en Azure)
var storageConn =
    Environment.GetEnvironmentVariable("StorageQueueConnectionString") ??
    builder.Configuration.GetConnectionString("StorageQueueConnectionString") ??
    throw new InvalidOperationException("StorageQueueConnectionString no está definida.");

// 2) Nombre de la cola
var queueName =
    Environment.GetEnvironmentVariable("StorageQueueName") ??
    builder.Configuration.GetConnectionString("StorageQueueName") ??
    throw new InvalidOperationException("StorageQueueName no está definida.");


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es-ES");
    options.SupportedCultures = new[] { new CultureInfo("es-ES") };
    options.SupportedUICultures = new[] { new CultureInfo("es-ES") };
});
builder.Services.AddDbContext<ProductContext>((sp, options) =>
{
    /* //--PARA EL PRIMER SISTEMA REALIZADO----------------------------------------------------
    // Intenta primero obtener la conexión desde variable de entorno (App Service)
    var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

    // Si no existe en el entorno, usa la de appsettings.json (para desarrollo)
    if (string.IsNullOrEmpty(connectionString))
        connectionString = builder.Configuration.GetConnectionString("SqlConnectionString");

    options.UseSqlServer(connectionString); */

    //PARA EL SEGUNDO SISTEMA REALIZADO CON SQSERVER Y MYSQL-----------------------------------
    var cfg = sp.GetRequiredService<IConfiguration>();
    var provider = cfg.GetValue<string>("DatabaseProvider") ?? "SqlServer";

    if (string.Equals(provider, "MySql", StringComparison.OrdinalIgnoreCase))
    {
        var cs = cfg.GetConnectionString("MysqlConnectionString")!;
        // Evita AutoDetect si tu servidor puede estar apagado al compilar/migrar:
        options.UseMySql(cs, new MySqlServerVersion(new Version(8,0,36)), b =>
        {
            b.MigrationsHistoryTable("__EFMigrationsHistory_Product");
        });
        // packages: Pomelo.EntityFrameworkCore.MySql + MySqlConnector
    }
    else
    {
        var cs = cfg.GetConnectionString("SqlConnectionString")!;
        options.UseSqlServer(cs, b =>
        {
            b.MigrationsHistoryTable("__EFMigrationsHistory_Product");
        });
        // package: Microsoft.EntityFrameworkCore.SqlServer
    }



}
);

builder.Services.AddDbContext<AuditableContext>((sp, options) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var provider = cfg.GetValue<string>("DatabaseProvider") ?? "SqlServer";
    if (provider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        var cs = cfg.GetConnectionString("MysqlConnectionString")!;
        options.UseMySql(cs, new MySqlServerVersion(new Version(8,0,36)), b =>
        {
            // 👈 Evita colisión si ambos contextos comparten BD
            b.MigrationsHistoryTable("__EFMigrationsHistory_Audit");
        });
    }
    else
    {
        var cs = cfg.GetConnectionString("SqlConnectionString")!;
        options.UseSqlServer(cs, b =>
        {
            b.MigrationsHistoryTable("__EFMigrationsHistory_Audit");
        });
    }
});

//CÓDIGO SERVICE BUS-------------------------------------------------------------
// Registrar el cliente Service Bus como singleton
/* builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnection));
builder.Services.AddControllers(); */
//-------------------------------------------------------------------------------
//CÓDIGO STORAGE QUEQUE----------------------------------------------------------
builder.Services.AddSingleton(sp =>
{
    var qc = new QueueClient(storageConn, queueName, new QueueClientOptions
    {
        // Evita problemas con caracteres → el SDK codifica/decodifica en Base64
        MessageEncoding = QueueMessageEncoding.Base64
    });
    qc.CreateIfNotExists(); // idempotente
    return qc;
});
builder.Services.AddControllers();
//-------------------------------------------------------------------------------


// Cargar log4net.config
var repo = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(repo, new FileInfo("log4net.config"));

var app = builder.Build();
var log = LogManager.GetLogger(typeof(Program));
app.UseRequestLocalization(); 
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    var productDb = sp.GetRequiredService<ProductContext>();
    await productDb.Database.MigrateAsync();

    var auditDb = sp.GetRequiredService<AuditableContext>();
    await auditDb.Database.MigrateAsync();
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

