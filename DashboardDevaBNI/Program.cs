using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DinkToPdf.Contracts;
using DinkToPdf;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

string logDirectory = Environment.GetEnvironmentVariable("DEVA_LOGGING__DIRECTORY") ?? "LOGS";
string serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "dashboard-deva";
string endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://192.168.65.155:4317";
string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development";
string serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "1.0.0";

Log.Logger = new LoggerConfiguration()
.WriteTo.File(
    new CompactJsonFormatter(), $"{logDirectory}/log-.txt", rollingInterval: RollingInterval.Day
)
.WriteTo.OpenTelemetry(options =>
{
    options.Endpoint = endpoint;
	options.Protocol = OtlpProtocol.Grpc;
	options.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField | IncludedData.SourceContextAttribute;
    options.ResourceAttributes = new Dictionary<string, object>
    {
		{"service.name", serviceName},
        {"service.version", serviceVersion},
        {"index", 10},
        {"flag", true},
        {"value", 3.14},
        {"deployment.environment", environment},
        {"service.instance.id", serviceName},
        {"service.namespace", serviceName}
    };
})
.WriteTo.Console(new CompactJsonFormatter())
.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
	
    builder.Configuration.AddEnvironmentVariables(prefix: "DEVA_");
	
	builder.Services.AddSerilog();
	
	builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddAttributes(new Dictionary<string, object>
        {
            {"service.name", serviceName},
            {"service.version", serviceVersion},
            {"deployment.environment", environment},
            {"service.instance.id", serviceName},
            {"service.namespace", serviceName}
        }))
        .WithMetrics(metrics =>
	    {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();
            metrics.AddRuntimeInstrumentation();
            metrics.AddProcessInstrumentation();
            metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(endpoint);
                    options.ExportProcessorType = ExportProcessorType.Simple;
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
        })
        .WithTracing(tracing =>
	    {
		    tracing.AddAspNetCoreInstrumentation();
		    tracing.AddHttpClientInstrumentation();
            tracing.AddEntityFrameworkCoreInstrumentation();
            tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(endpoint);
                    options.ExportProcessorType = ExportProcessorType.Simple;
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
	    });

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(int.Parse(GetConfig.AppSetting["AppSettings:Login:SessionDuration"]));
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        //options.Cookie.HttpOnly = true;
        //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    builder.Services.AddDbContext<DbDashboardDevaBniContext>();

    var app = builder.Build();
    // app.Use(async (context, next) =>
    // {

    //     context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    //     context.Response.Headers.Add("Content-Security-Policy", "font-src 'self' http://* data:; default-src ;    style-src 'self' http:// 'unsafe-inline'; script-src 'self' http://* 'unsafe-inline' 'unsafe-eval'; img-src 'self' http://* data:;");
    //     context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    //     context.Response.Headers.Add("Permissions-Policy", "fullscreen=()");
    //     context.Response.Headers.Add("Cross-Origin-Opener-Policy", "unsafe-none");
    //     context.Response.Headers.Add("Cross-Origin-Resource-Policy", "same-origin");
    //     context.Response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
    //     context.Response.Headers.Remove("Server");
    //     context.Response.Headers.Remove("X-Powered-By");
    //     await next();
    // });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSession();
    app.UseRouting();

    app.UseSerilogRequestLogging();

    app.UseAuthorization();
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Login}/{id?}");

    app.Run();

    //Scaffold-DbContext "Data Source=192.168.142.15,1470;Initial Catalog=dbDashboardDevaBNI;User ID=GOVDEVADEV;Password=BN15qlDEVA./;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -NoOnConfiguring -OutputDir Models -force
    //dotnet ef dbcontext Scaffold Server Scaffold-DbContext "Data Source=192.168.231.66;Initial Catalog=dbDashboardDevaBNI;User ID=sa;Password=sa;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer  -OutputDir Models -force
} catch(Exception ex)
{
    Log.Error(ex, "error running app");
} finally {
    Log.CloseAndFlush();
}