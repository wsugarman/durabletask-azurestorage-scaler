# durabletask-azurestorage-scaler

> **Note**
>
> This helm chart has not yet been published

A KEDA external scaler for the Durable Task Azure Storage backend. It is compatible with both the [Durable Task Framework (DTFx)](https://github.com/Azure/durabletask) and [Azure Durable Functions](https://github.com/Azure/azure-functions-durable-extension).

## Installation
The below command installs a scaler called `dtfx-scaler`.
```bash
helm repo add wsugarman https://wsugarman.github.io/charts
helm repo update

helm install --namespace keda --create-namespace dtfx-scaler wsugarman/durabletask-azurestorage-scaler
```

## Deletion
The below command deletes an existing scaler release called `dtfx-scaler`.
```bash
helm uninstall --namespace keda dtfx-scaler
```

## Configuration

Values may be specified using the `--set key=value` argument as seen in the [`helm install` documentation](https://helm.sh/docs/helm/helm_install/):

```bash
helm install \
  --namespace keda \
  --create-namespace \
  --set replicaCount=2 \
  --set podIdentity.azureWorkload.enabled=true \
  dtfx-scaler \
  wsugarman/durabletask-azurestorage-scaler
```

Values may also be [overridden using a YAML file](https://helm.sh/docs/chart_template_guide/values_files/) with the `-f` argument:

```bash
helm install --namespace keda --create-namespace -f values.yaml dtfx-scaler wsugarman/durabletask-azurestorage-scaler
```

### Values

Below is a table containing the possible set of configurations and their default values.

| Path                                          | Type      | Description                                                                  | Default                                             |
| --------------------------------------------- | --------- | ---------------------------------------------------------------------------- | --------------------------------------------------- |
| `additionalAnnotations`                       | `object`  | Additional annotations to add to all of the chart's resources                | `{}`                                                |
| `additionalLabels`                            | `object`  | Additional labels to add to all of the chart's resources                     | `{}`                                                |
| `env`                                         | `array`   | Additional environment variables that will be passed into the scaler pods    | `[]`                                                |
| `fullnameOverride`                            | `string`  | Overrides the object name and the name in the `app.kubernetes.io/name` label |                                                     |
| `image.pullPolicy`                            | `string`  | Scaler gRPC service image pull policy                                        | `IfNotPresent`                                      |
| `image.pullSecrets`                           | `array`   | Scaler gRPC service image pull secrets                                       | `[]`                                                |
| `image.repository`                            | `string`  | Scaler gRPC service image repository                                         | `ghcr.io/wsugarman/durabletask-azurestorage-scaler` |
| `image.tag`                                   | `string`  | Scaler gRPC service image tag                                                | `1.0.0-alpha.1`                                     |
| `nameOverride`                                | `string`  | Overrides the chart name used in the `helm.sh/chart` label                   |                                                     |
| `nodeSelector`                                | `object`  | Node selector for scaler deployment pods                                     | `{}`                                                |
| `podAnnotations`                              | `object`  | Additional annotations for only the pods                                     | `{}`                                                |
| `podIdentity.activeDirectory.identity`        | `string`  | Identity in Azure Active Directory to use for Azure pod identity             | `''`                                                |
| `podIdentity.azureWorkload.clientId`          | `string`  | Id of Azure Active Directory Client to use for authentication with Azure Workload Identity. | `''`                                 |
| `podIdentity.azureWorkload.enabled`           | `boolean` | Specifies whether [Azure Workload Identity](https://azure.github.io/azure-workload-identity/) is to be enabled or not. | `false`   |
| `podIdentity.azureWorkload.tenantId`          | `string`  | Id Azure Active Directory Tenant to use for authentication with for Azure Workload Identity. | `''`                                |
| `podIdentity.azureWorkload.tokenExpiration`   | `integer` | Duration in seconds to automatically expire tokens for the service account.  | `3600`                                              |
| `podLabels`                                   | `object`  | Additional labels for only the pods                                          | `{}`                                                |
| `podSecurityContext`                          | `object`  | Security context for all pods                                                | [See below](#security)                              |
| `port`                                        | `integer` | Scaler gRPC service port                                                     | `4370`                                              |
| `priorityClassName`                           | `string`  | Scaler pod priority                                                          | `''`                                                |
| `replicaCount`                                | `integer` | The number of replicas for the gRPC service                                  | `1`                                                 |
| `resources.limits.cpu`                        | `string`  | Maxiumum CPU units for the scaler gRPC service                               | `100m`                                              |
| `resources.limits.memory`                     | `string`  | Maxiumum memory in bytes for the scaler service                              | `512Mi`                                             |
| `resources.requests.cpu`                      | `string`  | Requested CPU units for the scaler gRPC service                              | `10m`                                               |
| `resources.requests.memory`                   | `string`  | Requested memory in bytes for the scaler gRPC service                        | `128Mi`                                             |
| `serviceAccount.annotations`                  | `object`  | Annotations to add to the service account                                    | `{}`                                                |
| `serviceAccount.automountServiceAccountToken` | `boolean` | Specifies whether created service account should automount API-Credentials   | `true`                                              |
| `securityContext`                             | `object`  | Security context for all containers                                          | [See below](#security)                              |
| `serviceAccount.create`                       | `boolean` | Specifies whether a service account should be created                        | `true`                                              |
| `serviceAccount.name`                         | `string`  | The name of the service account to use. If not set and create is true, a name is generated based on the release name and `fullnameOverride` | |
| `topologySpreadConstraints`                   | `object`  | Scaler constraints for distributing pods across a cluster                    | `{}`                                                |
| `upgradeStrategy`                             | `object`  | The upgrade strategy                                                         | The deployment strategy for replacing existing pods |

## Security

By default, the external scaler runs as non-root in a read-only file system.

```yaml
securityContext:
  capabilities:
    drop:
    - ALL
  allowPrivilegeEscalation: false
  readOnlyRootFilesystem: true
  seccompProfile:
    type: RuntimeDefault

podSecurityContext:
  runAsNonRoot: true
  runAsUser: 200
```

