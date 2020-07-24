# API Proxy Application

This application is a prototype of the functionality required within the Glasswall ICAP Server Resource. It is required to take the specified folder, write each file to the configured blob store, then monitor the specified service bus for the processing outcome message for each file.

## Pre-requisites

- The 'DurableFileProcessing' project (either running locally or deployed)
- Blob storage and Service bus connections strings consistent with those being used by 'DurableFileProcessing'

# Configuration

The application requires that a foldername is provided that contains the files to be uploaded to the Blob store for processing. This can be done on the command line
```
./apiproxyapp.exe -f "C:\filefolder"
```
or by adding an entry to the `appsettings.json` file
```
{
	...
	"folder-path" :  "C:\\filefolder",
	...
}
```
The remaining configuration items are required to match the configuration used in the `DurableFileProcessing` project `local.settings.json` file.
- `input-container-name`: this specifies the Blob storage contain name into which the files are uploaded. This is hard-coded in the `DurableFileProcessing` project.
- `outcome-queue-name`: this specifies the Service Bus name to be monitored for processing outcome messages. This is the `TransactionOutcomeQueueName` configuration item in the `DurableFileProcessing` project.

There are two `secrets` that part of the configuration. These are to be specified in the `secret.appsettings.json` file. The example below can be used as a template, with configuration taken from the `DurableFileProcessing` project.
```
{
  "blob-container-connection-string": "<must match the "FileProcessingStorage" configuration item in the `DurableFileProcessing` project>",
  "service-bus-connection-string": "<must match the "ServiceBusConnectionString" configuration item in the `DurableFileProcessing` project>"
}
```

The "Copy to Output Folder" property of the `secret.appsettings.json` file must be set to  "Copy if newer" in Visual Studio.




