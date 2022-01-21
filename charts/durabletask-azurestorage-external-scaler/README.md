# durabletask-azurestorage-external-scaler
**Notice**: Helm chart is coming soon

A KEDA external scaler for the Durable Task Azure Storage backend

## Requirements
- [KEDA](https://github.com/kedacore/charts/tree/main/keda) chart
- Kubernetes API server compatible with the 1.23 SDK

## Installation
The below command installs a scaler called `durabletask-scaler`.
```bash
$ helm repo add wsugarman https://wsugarman.github.io/charts
$ helm repo update

$ helm install durabletask-scaler wsugarman/durabletask-azurestorage-external-scaler --namespace keda --create-namespace
```

## Deletion
The below command deletes an existing scaler release called `durabletask-scaler`.
```bash
$ helm delete durabletask-scaler
```

## Configuration
| Path                        | Type       | Description                                                | Default                                                      |
| ----                        | ----       | -----------                                                | -------                                                      |
| `nameOverride`              | `string`   | Overrides the chart name used in the `helm.sh/chart` label |                                                              |
| `fullnameOverride`          | `string`   | Overrides the name in the `app.kubernetes.io/name` label   |                                                              |
| `additionalLabels           | `object`   | Additional labels to add to all of the chart's resources   | `{}`                                                         |
| `image.repository`          | `string`   | Scaler gRPC service image repository                       | `ghcr.io/wsugarman/durabletask-azurestorage-external-scaler` |
| `image.tag`                 | `string`   | Scaler gRPC service image tag                              | `1.0.0-alpha.1`                                              |
| `image.pullPolicy`          | `string`   | Scaler gRPC service image pull policy                      | `Always`                                                     |
| `image.pullSecrets`         | `array`    | Scaler gRPC service image pull secrets                     | `[]`                                                         |
| `port`                      | `integer`  | Scaler gRPC service port                                   | `4370`                                                       |
| `resources.requests.cpu`    | `string`   | Requested CPU units for the scaler gRPC service            | `10m`                                                        |
| `resources.requests.memory` | `string`   | Requested memory in bytes for the scaler gRPC service      | `128Mi`                                                      |
| `resources.limits.cpu`      | `string`   | Maxiumum CPU units for the scaler gRPC service             | `100m`                                                       |
| `resources.limits.memory`   | `string`   | Maxiumum memory in bytes for the scaler gRPC service       | `512Mi`                                                      |
| `rbac.create`               | `boolean`  | Indicates whether RBAC roles should be created             | `true`                                                       |
| `serviceAccount.create`     | `boolean`  | Indicates whether a service account should be created      | `true`                                                       |
| `serviceAccount.name`       | `string`   | Overrides the name of the service account                  | `""`                                                         |
