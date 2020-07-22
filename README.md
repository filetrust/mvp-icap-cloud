# MVP ICAP Cloud 

## Durable File Processing
Using a Blob Trigger to orchestrate a set of actions to be carried out on the uploaded file.

# High-level Description
The workflow is triggered by a Blob being added to the `original-store` container (in the `FileProcessingStorage` Azure Storage Account). When the processing of the file is complete, a message is submitted to the `TransactionOutcomeQueue`.

# Storage
All workflow storage is persisted in `FileProcessingStorage`
`original-store` : A Blob container into which any file to be process is written. The addition of blobs to this store triggers the workflow.
`rebuild-store` : A blob container into which the rebuild version of the file is written. The original file's hash value is used to name the file written to this store.

# Configuration
The following configuration is required in `local.settings.json` for the 'DurableFileProcessing' project folder.
- `AzureWebJobsStorage` : The connection string of the Azure Storage Account being used by the framework. For local development use "UseDevelopmentStorage=true". When deployed to Azure, this may use the same storage account as 'FileProcessingStorage'.
- `FileProcessingStorage` : The connection string of the Azure Storage Account being used by the workflow logic. In order that the Filetype Detection and Rebuild APIs can access the necessary stores, this Storage Account must be provided within Microsoft Azure and not the Azure Storage Emulator.
- `ServiceBusConnectionString` : The connection string of the Service Bus Namespace in which the `TransactionOutcomeQueueName` exists.
- `TransactionOutcomeQueueName`  : The name of the Service Bus Queue within specified Service Bus Namespace used to return the processing results.
- `FiletypeDetectionUrl` & `FiletypeDetectionKey` : The URL used to access the Filetype Detection API, and its associated key.
- `RebuildUrl` & `RebuildKey` : The URL used to access the Rebuild API, and its associated key.

*Sample local.settings.json file*
```
{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FileProcessingStorage": "DefaultEndpointsProtocol=https;AccountName=durablefilestorage;AccountKey=STORAGE_ACCOUNT_CONNECTION_STRING",
    "ServiceBusConnectionString": "Endpoint=sb://SERVICE_BUS_NAMESPACE_CONNECTION_STRING",
    "TransactionOutcomeQueueName": "transaction-outcome",
    "FiletypeDetectionUrl": "https://FILETYPE_DETECTION_API_URL",
    "FiletypeDetectionKey": "FILETYPE_DETECTION_KEY",
    "RebuildUrl": "https://REBUILD_API_URL",
    "RebuildKey": "REBUILD_KEY",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  }
}
```

## Setup
To setup a development environment the following Azure CLI commands can be used to create the necessary resources, and provide the configuration items required in the `local.settings.json` file.

Create a resource group to contain the development resources
```
az group create --location uksouth --name <Resource Group Name>
```

### Setup Blob Storage Containers

Create the Storage Account
```
az storage account create --name <Storage Account Name> --resource-group <Resource Group Name> --location uksouth
```

Get the Storage Account Connection String and store it in a variable
```
$connectionstring=$(az storage account show-connection-string --name <Storage Account Name> --resource-group <Resource Group Name>)
$env:AZURE_STORAGE_CONNECTION_STRING = $connectionString
```
The connection string can then be used to set the `FileProcessingStorage` configuration item. Just type `$connectionString` to access it.

Create the necessary containers
```
az storage container create --name "original-store" --connection-string $connectionString
az storage container create --name "rebuild-store" --connection-string $connectionString
```

### Setup Service Bus Queue

Create the Service Bus Namespace
```
az servicebus namespace create --name <SB Namespace Name>  --resource-group <Resource Group Name>  --location uksouth
```
Get the connection string for the namespace
```
az servicebus namespace authorization-rule keys list --resource-group <Resource Group Name> --namespace-name <SB Namespace Name> --name RootManageSharedAccessKey --query primaryConnectionString
```
This connection string can then be used to set the `ServiceBusConnectionString` configuration item.

Create the service bus queue
```
az servicebus queue create --resource-group <Resource Group Name>  --namespace-name <SB Namespace Name> --name "transaction-outcome"
```
The value entered as the `--name` can then be used to set the `TransactionOutcomeQueueName` configuration item.

# Storage Emulator
The [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) can be used to support development by hosting the durable function framework files. Since the file processing APIs need access to the `original` and `rebuilt` stores, these cannot be emulated locally.