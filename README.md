# KEDA Durable Task External Scaler for Azure Storage
[![Scaler Build](https://github.com/wsugarman/durabletask-azurestorage-external-scaler/actions/workflows/scaler-ci.yml/badge.svg)](https://github.com/wsugarman/durabletask-azurestorage-external-scaler/actions/workflows/scaler-ci.yml) [![Scaler Code Coverage](https://codecov.io/gh/wsugarman/durabletask-azurestorage-external-scaler/branch/main/graph/badge.svg)](https://codecov.io/gh/wsugarman/durabletask-azurestorage-external-scaler) [![GitHub License](https://img.shields.io/github/license/wsugarman/durabletask-azurestorage-external-scaler?label=License)](https://github.com/wsugarman/durabletask-azurestorage-external-scaler/blob/main/LICENSE)

A KEDA external scaler for [Durable Task Framework (DTFx)](https://github.com/Azure/durabletask) and [Azure Durable Function](https://github.com/Azure/azure-functions-durable-extension) applications in Kubernetes that rely on the Azure Storage provider.

## Trigger Specification
This specification describes the `external` trigger for applications that use the Durable Task Azure Storage provider.

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-external-scaler.keda:4050
        connectionFromEnv: STORAGE_CONNECTIONSTRING_ENV_NAME
        scaleIncrement: 2
        maxMessageLatencyMilliseconds: 750
        taskHubName: mytaskhub
```

### Parameter List
- **`accountName`** - Optional name of the Azure Storage account used by the Durable Task Framework (DTFx). This value is only required by when `useAAdPodIdentity` is `true`
- **`cloud`** - Optional name of the cloud environment that contains the Azure Storage account. Must be a known Azure cloud environment (note that private clouds such as Azure Stack Hub or Air Gapped clouds are not yet supported). Defaults to the public cloud `AzurePublicCloud`. Possible values include:
  - `AzurePublicCloud`
  - `AzureUSGovernmentCloud`
  - `AzureChinaCloud`
  - `AzureGermanCloud`
- **`connection`** - Optional connection string for the Azure Storage account that may be used as an alternative to `connectionFromEnv`
- **`connectionFromEnv`** - Optional name of the environment variable your deployment uses to get the connection string. Defaults to `AzureWebJobsStorage`
- **`scaleIncrement`** - Optional ratio by which replicas are increased or decreased at a time (e.g. a value of `2` indicates that under load the number of replicas are doubled, and halved in lieu of pending work). Unlike other scalers, the scaler for the Durable Task Framework (DTFx) emulates the behavior found in Azure App Services and incrementally scales up and down by a constant amount. Must be greater than 1. Defaults to `1.5`
- **`maxMessageLatencyMilliseconds`** - Optional maximum amount that a message should sit in the work item or control queues. See [here](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-perf-and-scale#internal-queue-triggers) for more information about the back-end architecture. Cannot be larger than `1000` or 1 second. Defaults to `1000`
- **`taskHubName`** - Optional name of the Durable Task Framework (DTFx) task hub. This name is used when determining the name of blob containers, tables, and queues related to the application. Defaults to `TestHubName`
- **`useAAdPodIdentity`** - Optionally indicates that AAD pod identity should be used to authenticate between the scaler and the Azure Storage account. If `true`, `Account` must be specified and an AAD pod identity must be bound to the deployment. Defaults to `false`

## Authentication
The scaler supports authentication using either an [Azure Storage connection string](https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string) or [AAD pod identity](https://github.com/Azure/aad-pod-identity).

### Connection Strings
Connection strings may be specified using an environment variable exposed to the deployment using the parameter `connectionFromEnv`. By default, the scaler will look for an environment variable called `AzureWebJobsStorage`. For example:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-external-scaler.keda:4050 # Required. Address of the external scaler service
        connectionFromEnv: <variable> # Optional. By default 'AzureWebJobsStorage'
```

Connection strings may also be specified directly via the `connection` parameter:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-external-scaler.keda:4050 # Required. Address of the external scaler service
        connection: <connection> # Optional. Defaults to connectionFromEnv
```

### Identity-Based Connection
Unfortunately, KEDA external scalers do not support the use of [`TriggerAuthentication`](https://keda.sh/docs/2.5/concepts/authentication/#re-use-credentials-and-delegate-auth-with-triggerauthentication), but the scaler can still leverage an identity-based connection. To use an identity, the scaler deployment must include an [AAD Pod Binding](https://azure.github.io/aad-pod-identity/docs/demo/standard_walkthrough/#5-deploy-azureidentitybinding). Be sure to only bind a single identity, as it may lead to inconsistencies at runtime!

An example specification that uses an identity-based connection can be seen below:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-external-scaler.keda:4050 # Required. Address of the external scaler service
        accountName: <name>      # Optional. Required for pod identity
        cloud: <cloud>           # Optional. Defaults to AzurePublicCloud
        useAAdPodIdentity: true  # Optional. Must be true for pod identity. Defaults to false
```

## Helm
Coming soon

## Licenses
The external scaler is licensed under the [MIT](https://github.com/wsugarman/durabletask-azurestorage-external-scaler/blob/main/LICENSE) license. The storm icon was created by [Evon](https://thenounproject.com/evonmbon/) and is licensed royalty-free through [The Noun Project](https://thenounproject.com/).
