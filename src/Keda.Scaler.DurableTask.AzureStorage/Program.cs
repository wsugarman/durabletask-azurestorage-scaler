// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage.Certificates;
using Keda.Scaler.DurableTask.AzureStorage.Clients;
using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Keda.Scaler.DurableTask.AzureStorage.Metadata;
using Keda.Scaler.DurableTask.AzureStorage.TaskHubs;
using Keda.Scaler.DurableTask.AzureStorage.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

const string PolicyName = "default";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container
builder.Services
    .AddScalerMetadata()
    .AddAzureStorageServiceClients()
    .AddDurableTaskScaleManager()
    .AddTlsSupport(PolicyName, builder.Configuration)
    .AddGrpc(o =>
    {
        o.Interceptors.Add<ExceptionInterceptor>();
        o.Interceptors.Add<ScalerMetadataInterceptor>();
    });

#if DEBUG
// Note: gRPC reflection is only used for debugging, and as such it will not be included
// in the final build artifact copied into the scaler image
builder.Services.AddGrpcReflection();
#endif

// Ensure that the client certificate validation defers to the athentication
// provided by Microsoft.AspNetCore.Authentication.Certificate. All other settings
// related to Kestrel will be specified via the configuration object
_ = builder.WebHost.ConfigureKestrel(k => k.ConfigureHttpsDefaults(h => h.AllowAnyClientCertificate()));

// Build the web app and update its middleware pipeline
WebApplication app = builder.Build();

// Configure the HTTP request pipeline
if (app.Configuration.ValidateClientCertificate())
    _ = app.UseAuthentication();

GrpcServiceEndpointConventionBuilder grpcBuilder = app.MapGrpcService<DurableTaskAzureStorageScalerService>();
if (app.Configuration.ValidateClientCertificate())
    _ = grpcBuilder.RequireAuthorization(PolicyName);

#if DEBUG
// The following routes and services should only be available when debugging the scaler
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapGrpcReflectionService();
#endif

app.Run();
