# KEDA Durable Task External Scaler for Azure Storage
[![Scaler Build](https://github.com/wsugarman/durabletask-azurestorage-scaler/actions/workflows/scaler-ci.yml/badge.svg)](https://github.com/wsugarman/durabletask-azurestorage-scaler/actions/workflows/scaler-ci.yml) [![Scaler Code Coverage](https://codecov.io/gh/wsugarman/durabletask-azurestorage-scaler/branch/main/graph/badge.svg)](https://codecov.io/gh/wsugarman/durabletask-azurestorage-scaler) [![GitHub License](https://img.shields.io/github/license/wsugarman/durabletask-azurestorage-scaler?label=License)](https://github.com/wsugarman/durabletask-azurestorage-scaler/blob/main/LICENSE)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler?ref=badge_shield)

A KEDA external scaler for [Durable Task Framework (DTFx)](https://github.com/Azure/durabletask) and [Azure Durable Function](https://github.com/Azure/azure-functions-durable-extension) applications in Kubernetes that rely on the Azure Storage backend.

## Trigger Specification
This specification describes the `external` trigger for applications that use the Durable Task Azure Storage provider.

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-scaler.keda:4370
        connectionFromEnv: STORAGE_CONNECTIONSTRING_ENV_NAME
        maxActivitiesPerWorker: 5
        maxOrchestrationsPerWorker: 2
        taskHubName: mytaskhub
```

### Parameter List
- **`accountName`** - Optional name of the Azure Storage account used by the Durable Task Framework (DTFx). This value is only required when `useManagedIdentity` is `true`
- **`activeDirectoryEndpoint`** - Optional host authority for Azure Active Directory (AAD). This value is only required when `cloud` is `Private`. Otherwise, the value is automatically derived for well-known cloud environments
- **`clientId`** - Optional identity used when authenticating via managed identity. This value can only be specified when `useManagedIdentity` is `true`
- **`cloud`** - Optional name of the cloud environment that contains the Azure Storage account. Must be a known Azure cloud environment, or `Private` for Azure Stack Hub or air-gapped clouds. If `Private` is specified, both `endpointSuffix` and `activeDirectoryEndpoint` must be specified. Defaults to the `AzurePublicCloud`. Possible values include:
  - `AzurePublicCloud`
  - `AzureUSGovernmentCloud`
  - `AzureChinaCloud`
  - `AzureGermanCloud`
  - `Private`
- **`connection`** - Optional connection string for the Azure Storage account that may be used as an alternative to `connectionFromEnv`
- **`connectionFromEnv`** - Optional name of the environment variable your deployment uses to get the connection string. Defaults to `AzureWebJobsStorage`
- **`endpointSuffix`** - Optional suffix for the Azure Storage service URLs. This value is only required when `cloud` is `Private`. Otherwise, the value is automatically derived for well-known cloud environments
- **`maxActivitiesPerWorker`** - Optional maximum number of activity work items that a single worker may process at any time. This is equivalent to `MaxConcurrentActivityFunctions`in Azure Durable Functions and `MaxConcurrentTaskActivityWorkItems` in the Durable Task Framework (DTFx). Must be greater than 0. Defaults to `10`
- **`maxOrchestrationsPerWorker`** - Optional maximum number of orchestration work items that a single worker may process at any time. This is equivalent to `MaxConcurrentOrchestratorFunctions` in Azure Durable Functions and `MaxConcurrentTaskOrchestrationWorkItems` in the Durable Task Framework (DTFx). Must be greater than 0. Defaults to `5`
- **`taskHubName`** - Optional name of the Durable Task Framework (DTFx) task hub. This name is used when determining the name of blob containers, tables, and queues related to the application. Defaults to `TestHubName`
- **`useManagedIdentity`** - Optionally indicates that AAD pod identity or workload identity should be used to authenticate between the scaler and the Azure Storage account. If `true`, `Account` must be specified, and the appropriate annotations, bindings, and/or labels must be configured for the deployment. Defaults to `false`

## Authentication
The scaler supports authentication using either an [Azure Storage connection string](https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string), [AAD pod identity](https://github.com/Azure/aad-pod-identity), or [Azure AD Workload Identity](https://azure.github.io/azure-workload-identity/docs/).

### Connection Strings
Connection strings may be specified using an environment variable exposed to the deployment using the parameter `connectionFromEnv`. By default, the scaler will look for an environment variable called `AzureWebJobsStorage`. For example:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-scaler.keda:4370 # Required. Address of the external scaler service
        connectionFromEnv: <variable> # Optional. By default 'AzureWebJobsStorage'
```

Connection strings may also be specified directly via the `connection` parameter:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-scaler.keda:4370 # Required. Address of the external scaler service
        connection: <connection> # Optional. Defaults to connectionFromEnv
```

### Identity-Based Connection
KEDA external scalers do not support the use of [`TriggerAuthentication`](https://keda.sh/docs/2.5/concepts/authentication/#re-use-credentials-and-delegate-auth-with-triggerauthentication), but the scaler can still leverage an identity-based connection. To use an identity, the scaler deployment must include an [AAD Pod Binding](https://azure.github.io/aad-pod-identity/docs/demo/standard_walkthrough/#5-deploy-azureidentitybinding) or [Workload Identity Service Account labels](https://azure.github.io/azure-workload-identity/docs/topics/service-account-labels-and-annotations.html). If there are multiple identities, be sure to specify the `clientId` parameter if not already specified for Workload Identity.

An example specification that uses an identity-based connection can be seen below:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: durabletask-azurestorage-scaler.keda:4370 # Required. Address of the external scaler service
        accountName: <name>      # Optional. Required for pod identity
        clientId: <client-id>    # Optional. Recommended if there are multiple identities
        cloud: <cloud>           # Optional. Defaults to AzurePublicCloud
        useManagedIdentity: true # Optional. Must be true for managed identity. Defaults to false
```

## Helm
Coming soon

## Licenses
The external scaler is licensed under the [MIT](https://github.com/wsugarman/durabletask-azurestorage-scaler/blob/main/LICENSE) license. The storm icon was created by [Evon](https://thenounproject.com/evonmbon/) and is licensed royalty-free through [The Noun Project](https://thenounproject.com/).


[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler?ref=badge_large)