# Durable Task KEDA External Scaler for Azure Storage
A KEDA external scaler for the Durable Task Azure Storage backend. It is compatible with both the [Durable Task Framework (DTFx)](https://github.com/Azure/durabletask) and [Azure Durable Functions](https://github.com/Azure/azure-functions-durable-extension).

## Installation
The below command installs a scaler called `dtfx-scaler`:

```bash
helm repo add wsugarman https://wsugarman.github.io/charts
helm repo update

helm install --namespace keda --create-namespace dtfx-scaler wsugarman/durabletask-azurestorage-scaler
```

## Deletion
The below command deletes an existing scaler release called `dtfx-scaler`:

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
  --set tls.serverCert.secret=server-tls \
  dtfx-scaler \
  wsugarman/durabletask-azurestorage-scaler
```

Values may also be [overridden using a YAML file](https://helm.sh/docs/chart_template_guide/values_files/) with the `-f` argument:

```bash
helm install --namespace keda --create-namespace -f values.yaml dtfx-scaler wsugarman/durabletask-azurestorage-scaler
```

### Values

The following table contains the possible set of configurations and their default values.

| Path                                        | Type      | Description                                                                                                                                 | Default                                             |
| ------------------------------------------- | --------- | ------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------- |
| `additionalAnnotations`                     | `object`  | Additional annotations to add to all of the chart's resources                                                                               | `{}`                                                |
| `additionalLabels`                          | `object`  | Additional labels to add to all of the chart's resources                                                                                    | `{}`                                                |
| `env`                                       | `array`   | Additional environment variables that will be passed into the scaler pods                                                                   | `null`                                              |
| `envFrom`                                   | `array`   | Additional sources of environment variables available in the scaler pods                                                                    | `null`                                              |
| `fullnameOverride`                          | `string`  | Overrides the object name and the name in the `app.kubernetes.io/name` label                                                                |                                                     |
| `image.pullPolicy`                          | `string`  | Scaler gRPC service image pull policy                                                                                                       | `IfNotPresent`                                      |
| `image.pullSecrets`                         | `array`   | Scaler gRPC service image pull secrets                                                                                                      | `[]`                                                |
| `image.repository`                          | `string`  | Scaler gRPC service image repository                                                                                                        | `ghcr.io/wsugarman/durabletask-azurestorage-scaler` |
| `image.tag`                                 | `string`  | Scaler gRPC service image tag                                                                                                               | `3.0.0`                                             |
| `logging.format`                            | `string`  | The logging message format                                                                                                                  | `systemd`                                           |
| `logging.level`                             | `string`  | The minimum log level to be written to the console                                                                                          | `information`                                       |
| `logging.timestampFormat`                   | `string`  | The timestamp format in the log messages                                                                                                    | `O`                                                 |
| `nameOverride`                              | `string`  | Overrides the chart name used in the `helm.sh/chart` label                                                                                  |                                                     |
| `nodeSelector`                              | `object`  | Node selector for scaler deployment pods                                                                                                    | `{}`                                                |
| `podAnnotations`                            | `object`  | Additional annotations for only the pods                                                                                                    | `{}`                                                |
| `podIdentity.clientId`                      | `string`  | Microsoft Entra Client ID to use for authentication with Microsoft Entra Workload Identity.                                                 | `''`                                                |
| `podIdentity.enabled`                       | `boolean` | Specifies whether [Microsoft Entra Workload Identity](https://azure.github.io/azure-workload-identity/) is to be enabled or not.            | `false`                                             |
| `podIdentity.tenantId`                      | `string`  | Microsoft Entra Tenant ID associated with the `clientId`                                                                                    | `''`                                                |
| `podIdentity.tokenExpiration`               | `integer` | Duration in seconds to automatically expire tokens for the service account.                                                                 | `3600`                                              |
| `podLabels`                                 | `object`  | Additional labels for only the pods                                                                                                         | `{}`                                                |
| `podSecurityContext`                        | `object`  | Security context for all containers in the pod                                                                                              | [See below](#security)                              |
| `port`                                      | `integer` | Scaler gRPC service port                                                                                                                    | `4370`                                              |
| `priorityClassName`                         | `string`  | Scaler pod priority                                                                                                                         | `''`                                                |
| `replicaCount`                              | `integer` | The number of replicas for the gRPC service                                                                                                 | `1`                                                 |
| `resources.limits.cpu`                      | `string`  | Maxiumum CPU units for the scaler gRPC service                                                                                              | `512M`                                              |
| `resources.limits.memory`                   | `string`  | Maxiumum memory in bytes for the scaler service                                                                                             | `1G`                                                |
| `resources.requests.cpu`                    | `string`  | Requested CPU units for the scaler gRPC service                                                                                             | `50m`                                               |
| `resources.requests.memory`                 | `string`  | Requested memory in bytes for the scaler gRPC service                                                                                       | `128M`                                              |
| `serviceAccount.annotations`                | `object`  | Annotations to add to the service account                                                                                                   | `{}`                                                |
| `serviceAccount.automount`                  | `boolean` | Specifies whether created service account should automount API-Credentials                                                                  | `true`                                              |
| `securityContext`                           | `object`  | Security context for the container                                                                                                          | [See below](#security)                              |
| `serviceAccount.create`                     | `boolean` | Specifies whether a service account should be created                                                                                       | `true`                                              |
| `serviceAccount.name`                       | `string`  | The name of the service account to use. If not set and create is true, a name is generated based on the release name and `fullnameOverride` |                                                     |
| `tls.caCert.key`                            | `string`  | The secret key that contains the custom Certificate Authority (CA) certificate                                                              | `tls.crt`                                           |
| `tls.caCert.secret`                         | `string`  | The name of the Kubernetes secret that contains the expected custom Certificate Authority (CA) certificate for client TLS certificates      |                                                     |
| `tls.serverCert.keys.cert`                  | `string`  | The secret key that contains the server TLS certificate                                                                                     | `tls.crt`                                           |
| `tls.serverCert.keys.key`                   | `string`  | The secret key that contains the server TLS certificate key                                                                                 | `tls.key`                                           |
| `tls.serverCert.secret`                     | `string`  | The name of the Kubernetes secret that contains the server TLS certificate                                                                  |                                                     |
| `tls.unsafe`                                | `boolean` | Specifies whether the client TLS certificate must be validated                                                                              | `false`                                             |
| `topologySpreadConstraints`                 | `object`  | Scaler constraints for distributing pods across a cluster                                                                                   | `{}`                                                |
| `upgradeStrategy`                           | `object`  | The upgrade strategy                                                                                                                        | `{}`                                                |

## Security

By default, the external scaler runs as non-root in a read-only file system:

```yaml
securityContext:
  allowPrivilegeEscalation: false
  capabilities:
    drop:
    - ALL
  readOnlyRootFilesystem: true

podSecurityContext:
  runAsNonRoot: true
  seccompProfile:
    type: RuntimeDefault
```
