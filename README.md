# Durable Task KEDA External Scaler for Azure Storage
[![Artifact Hub](https://img.shields.io/endpoint?url=https://artifacthub.io/badge/repository/wsugarman)](https://artifacthub.io/packages/keda-scaler/wsugarman-keda-scalers/durabletask-azurestorage-scaler)
[![Scaler Build](https://github.com/wsugarman/durabletask-azurestorage-scaler/actions/workflows/scaler-ci.yml/badge.svg)](https://github.com/wsugarman/durabletask-azurestorage-scaler/actions/workflows/scaler-ci.yml)
[![Scaler Code Coverage](https://codecov.io/gh/wsugarman/durabletask-azurestorage-scaler/branch/main/graph/badge.svg)](https://codecov.io/gh/wsugarman/durabletask-azurestorage-scaler)
[![GitHub License](https://img.shields.io/github/license/wsugarman/durabletask-azurestorage-scaler?label=License)](https://github.com/wsugarman/durabletask-azurestorage-scaler/blob/main/LICENSE)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler?ref=badge_shield)

A KEDA external scaler for [Durable Task Framework (DTFx)](https://github.com/Azure/durabletask) and [Azure Durable Function](https://github.com/Azure/azure-functions-durable-extension) applications in Kubernetes that rely on the [Azure Storage backend](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-azure-storage-provider).

## Trigger Specification
This specification describes the `external` trigger for applications that use the Durable Task Azure Storage provider.

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: dtfx-scaler.keda:4370
        connectionFromEnv: STORAGE_CONNECTIONSTRING_ENV_NAME
        maxActivitiesPerWorker: 5
        maxOrchestrationsPerWorker: 2
        taskHubName: mytaskhub
```

### Parameter List
- **`accountName`** - Optional name of the Azure Storage account used by the Durable Task Framework (DTFx). This value is only required when `useManagedIdentity` is `true`
- **`clientId`** - Optional identity used when authenticating via managed identity. This value can only be specified when `useManagedIdentity` is `true`
- **`cloud`** - Optional name of the cloud environment that contains the Azure Storage account. Must be a known Azure cloud environment, or `Private` for Azure Stack Hub or air-gapped clouds. If `'Private'` is specified, both `endpointSuffix` and `entraEndpoint` must be specified. Defaults to the `'AzurePublicCloud'`. Possible values include:
  - `AzurePublicCloud`
  - `AzureUSGovernmentCloud`
  - `AzureChinaCloud`
  - `Private`
- **`connection`** - Optional connection string for the Azure Storage account that may be used as an alternative to `connectionFromEnv`
- **`connectionFromEnv`** - Optional name of the environment variable your deployment uses to get the connection string. Defaults to `'AzureWebJobsStorage'`
- **`endpointSuffix`** - Optional suffix for the Azure Storage service URLs. This value is only required when `cloud` is `'Private'`. Otherwise, the value is automatically derived for well-known cloud environments
- **`entraEndpoint`** - Optional host authority for Microsoft Entra. This value is only required when `cloud` is `'Private'`. Otherwise, the value is automatically derived for well-known cloud environments
- **`maxActivitiesPerWorker`** - Optional maximum number of activity work items that a single worker may process at any time. This is equivalent to `MaxConcurrentActivityFunctions`in Azure Durable Functions and `MaxConcurrentTaskActivityWorkItems` in the Durable Task Framework (DTFx). Must be greater than 0. Defaults to `10`
- **`maxOrchestrationsPerWorker`** - Optional maximum number of orchestration work items that a single worker may process at any time. This is equivalent to `MaxConcurrentOrchestratorFunctions` in Azure Durable Functions and `MaxConcurrentTaskOrchestrationWorkItems` in the Durable Task Framework (DTFx). Must be greater than 0. Defaults to `5`
- **`scalerAddress`** - Required address for the scaler service within the Kubernetes cluster. The format of the address is `'<scaler-service-name>.<scaler-kubernetes-namespace>:<port>'`. By default, the chart uses port `4370` while the service name and namespace are dependent on the Helm installation command. For example, an installation like `helm install -n keda dtfx-scaler wsugarman/durabletask-azurestorage-scaler` would use the address `dtfx-scaler.keda:4370`. For more details, please see the [service template](/charts/durabletask-azurestorage-scaler/templates/03-service.yaml) in the Helm chart
- **`taskHubName`** - Optional name of the Durable Task Framework (DTFx) task hub. This name is used when determining the name of blob containers, tables, and queues related to the application. Defaults to `'TestHubName'`
- **`useManagedIdentity`** - Optionally indicates that Microsoft Entra Workload Identity should be used to authenticate between the scaler and the Azure Storage account. If `true`, `Account` must be specified, and the scaler deployment must also include a workload identity. Defaults to `false`
- **`useTablePartitionManagement`** - Optionally indicates that the task hub uses the newer [Partition Manager V3](https://techcommunity.microsoft.com/blog/appsonazureblog/preview-of-durable-functions-extension-v3-0-0/4000452) that relies on Azure Table Storage instead of the older Blob-based Partition Manager. Defaults to `true`

## Authentication
The scaler supports authentication using either an [Azure Storage connection string](https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string) or [Microsoft Entra Workload Identity](https://azure.github.io/azure-workload-identity/docs/).

### Connection Strings
Connection strings may be specified using an environment variable exposed to the deployment using the parameter `connectionFromEnv`. By default, the scaler will look for an environment variable called `AzureWebJobsStorage`. For example:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: dtfx-scaler.keda:4370 # Required. Address of the external scaler service
        connectionFromEnv: <variable> # Optional. By default 'AzureWebJobsStorage'
```

Connection strings may also be specified directly via the `connection` parameter:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: dtfx-scaler.keda:4370 # Required. Address of the external scaler service
        connection: DefaultEndpointsProtocol=https;AccountName=<account-name>;AccountKey=<account-key> # Optional. Defaults to connectionFromEnv
```

### Identity-Based Connection
To use an identity, the scaler deployment must be configured to use Azure Workload Identity. If there are multiple identities, be sure to specify the `clientId` parameter if it is not the default used by the deployment.

An example specification that uses an identity-based connection can be seen below:

```yml
  triggers:
    - type: external
      metadata:
        scalerAddress: dtfx-scaler.keda:4370 # Required. Address of the external scaler service
        accountName: <account-name> # Optional. Required for workload identity
        clientId: <client-id>       # Optional. Recommended if there are multiple identities
        cloud: <cloud>              # Optional. Defaults to AzurePublicCloud
        useManagedIdentity: true    # Optional. Must be true for workload identity. Defaults to false
```

### Transport Layer Security (TLS) Protocol
The scaler optionally supports TLS. Because the KEDA and external scaler pods are seperate, both parties must be configured for mutual TLS. To configure connections from the KEDA pod to use TLS, the corresponding `ScaledObject` must include information about the client certificates using the field `authenticationRef` and a matching `TriggerAuthentication` object containing the certificate. The scaler pods on the other hand must be configured via the Helm chart using the `tls*` values to provide a certificate (and optionally verify the client's certificate). See the chart [README](./charts/durabletask-azurestorage-scaler/README.md) for more details.

```yml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: dtfx-scaler-auth
  namespace: <namespace>
spec:
  secretTargetRef:
  - parameter: caCert
    name: <secret>
    key: tls.crt
  - parameter: tlsClientCert
    name: <name>
    key: tls.crt
  - parameter: tlsClientKey
    name: <name>
    key: tls.key
---
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: <name>
  namespace: <namespace>
spec:
  scaleTargetRef:
    name: <function app>
    kind: Deployment
  triggers:
    - type: external
      metadata:
        scalerAddress: dtfx-scaler.keda:4370 # Required. Address of the external scaler service
        accountName: <account-name> # Optional. Required for workload identity
      authenticationRef:
        name: dtfx-scaler-auth
```

## Helm
The scaler is available as a Helm chart in the repository https://wsugarman.github.io/charts:

```bash
helm repo add wsugarman https://wsugarman.github.io/charts
helm repo update

helm install --namespace keda --create-namespace dtfx-scaler wsugarman/durabletask-azurestorage-scaler
```

For more information, see the chart [README](./charts/durabletask-azurestorage-scaler/README.md) or visit [Artifact Hub](https://artifacthub.io/packages/keda-scaler/wsugarman-keda-scalers/durabletask-azurestorage-scaler).

## Licenses
The external scaler is licensed under the [MIT](https://github.com/wsugarman/durabletask-azurestorage-scaler/blob/main/LICENSE) license. The storm icon was created by [Evon](https://thenounproject.com/evonmbon/) and is licensed royalty-free through [The Noun Project](https://thenounproject.com/).

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fwsugarman%2Fdurabletask-azurestorage-scaler?ref=badge_large)
