# azurite
This helm chart is only used for integration testing, and therefore is not published. Its design is based on https://github.com/Azure/Azurite/issues/968#issuecomment-887974975.

If you wish to connect to the emulator (e.g. via the [Microsoft Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)), run the following command in a separate window:
```bash
kubectl port-forward service/azurite -n azure 10000:10000 10001:10001 10002:10002
```

To learn more about the Azurite emulator, see https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite.
