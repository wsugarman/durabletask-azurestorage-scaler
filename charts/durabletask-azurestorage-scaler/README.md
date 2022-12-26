# durabletask-azurestorage-scaler
**Notice**: Helm chart is coming soon

A KEDA external scaler for the Durable Task Azure Storage backend

## Installation
The below command installs a scaler called `durabletask-scaler`.
```bash
$ helm repo add wsugarman https://wsugarman.github.io/charts
$ helm repo update

$ helm install dtfx-scaler wsugarman/durabletask-azurestorage-scaler --namespace keda --create-namespace
```

## Deletion
The below command deletes an existing scaler release called `dtfx-scaler`.
```bash
$ helm delete dtfx-scaler
```

## Configuration
| Path                        | Type       | Description                                                | Default                                                      |
| ----                        | ----       | -----------                                                | -------                                                      |
| `nameOverride`              | `string`   | Overrides the chart name used in the `helm.sh/chart` label |                                                              |
| `fullnameOverride`          | `string`   | Overrides the name in the `app.kubernetes.io/name` label   |                                                              |
| `additionalLabels`          | `object`   | Additional labels to add to all of the chart's resources   | `{}`                                                         |
| `image.repository`          | `string`   | Scaler gRPC service image repository                       | `ghcr.io/wsugarman/durabletask-azurestorage-scaler` |
| `image.tag`                 | `string`   | Scaler gRPC service image tag                              | `1.0.0-alpha.1`                                              |
| `image.pullPolicy`          | `string`   | Scaler gRPC service image pull policy                      | `Always`                                                     |
| `image.pullSecrets`         | `array`    | Scaler gRPC service image pull secrets                     | `[]`                                                         |
| `port`                      | `integer`  | Scaler gRPC service port                                   | `4370`                                                       |
| `resources.requests.cpu`    | `string`   | Requested CPU units for the scaler gRPC service            | `10m`                                                        |
| `resources.requests.memory` | `string`   | Requested memory in bytes for the scaler gRPC service      | `128Mi`                                                      |
| `resources.limits.cpu`      | `string`   | Maxiumum CPU units for the scaler gRPC service             | `100m`                                                       |
| `resources.limits.memory`   | `string`   | Maxiumum memory in bytes for the scaler gRPC service       | `512Mi`                                                      |
