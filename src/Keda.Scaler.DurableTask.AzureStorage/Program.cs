// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Keda.Scaler.DurableTask.AzureStorage.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services
    .AddDurableTaskScaler()
    .AddTlsSupport()
    .AddGrpcReflection()
    .AddGrpc(o =>
    {
        o.EnableDetailedErrors = true;
        o.Interceptors.Add<ExceptionInterceptor>();
    });

// Configure the web server for TLS if necessary and build the app
WebApplication app = builder
    .ConfigureKestrelTls()
    .Build();

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();
app.MapGrpcService<DurableTaskAzureStorageScalerService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// Only enable reflection endpoints when developing
if (app.Environment.IsDevelopment())
    _ = app.MapGrpcReflectionService();

app.Run();
