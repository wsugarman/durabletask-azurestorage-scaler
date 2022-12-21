// Copyright © William Sugarman.
// Licensed under the MIT License.

using Keda.Scaler.DurableTask.AzureStorage;
using Keda.Scaler.DurableTask.AzureStorage.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services
    .AddDurableTaskScaler()
    .AddGrpc(o =>
    {
        o.EnableDetailedErrors = true;
        o.Interceptors.Add<ExceptionInterceptor>();
    });

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<DurableTaskAzureStorageScalerService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
