# API Proxy Application

This application is a prototype of the functionality required within the Glasswall ICAP Server Resource. It is required to take the specified folder, write each file to the configured blob store, then monitor the specified service bus for the processing outcome message for each file.

## Pre-requisites

- The 'DurableFileProcessing' project (either running locally or deployed)
- Blob storage and Service bus connections strings consistent with those being used by 'DurableFileProcessing'

