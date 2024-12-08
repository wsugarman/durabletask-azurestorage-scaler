// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.HealthChecks;
using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Keda.Scaler.DurableTask.AzureStorage.Security;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container
builder.Services
    .AddScoped<IScalerMetadataAccessor, ScalerMetadataAccessor>()
    .AddAzureStorageServiceClients()
    .AddDurableTaskScaleManager()
    .AddKubernetesHealthCheck(builder.Configuration)
    .AddTlsSupport("default", builder.Configuration)
    .AddGrpc(o =>
    {
        o.Interceptors.Add<ExceptionInterceptor>();
        o.Interceptors.Add<ScalerMetadataInterceptor>();
    });

// Note: gRPC reflection is only used for debugging, and as such it will not be included
// in the final build artifact copied into the scaler image
#if DEBUG
builder.Services.AddGrpcReflection();
#endif

// Configure the web server for TLS if necessary and build the app
WebApplication app = builder
    .ConfigureKestrelTls()
    .Build();

// Only add the health check if TLS is being enforced,
// as the only health check available concerns the TLS certificate
if (app.Configuration.EnforceTls())
    app.ConfigureKubernetesHealthCheck();

// Configure the HTTP request pipeline
if (app.Configuration.EnforceMutualTls())
    _ = app.UseAuthentication();

GrpcServiceEndpointConventionBuilder grpcBuilder = app.MapGrpcService<DurableTaskAzureStorageScalerService>();
if (app.Configuration.EnforceMutualTls())
    _ = grpcBuilder.RequireAuthorization("default");

// The following routes and services should only be available when debugging the scaler
#if DEBUG
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapGrpcReflectionService();
#endif

app.Run();
