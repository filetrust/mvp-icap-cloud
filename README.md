# MVP ICAP Cloud 
![CI Build](https://github.com/filetrust/mvp-icap-cloud/workflows/CI%20Build/badge.svg)
![Deploy Build](https://github.com/filetrust/mvp-icap-cloud/workflows/Deploy%20Build/badge.svg)

## Durable File Processing
Using a Blob Trigger to orchestrate a set of actions to be carried out on the uploaded file.

# High-level Description
The workflow is triggered by a Blob being added to the `original-store` container (in the `FileProcessingStorage` Azure Storage Account). When the processing of the file is complete, a message is submitted to the `TransactionOutcomeQueue`.

# Getting Started
For more information on getting started with the Durable File Processing project and an overview of the project, head over to the [Wiki](https://github.com/filetrust/mvp-icap-cloud/wiki)

T
