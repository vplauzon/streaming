# IoT Watch Current Load test

This is a load test to simulate a bunch of devices pushing telemetry to Azure IoT hub.  The telemetry is then ingested in some data service and random poke are done at the "current" (i.e. most up-to-date) telemetry.

We are comparing two data services:  Azure Cosmos DB & Azure Data Explorer.

Register devices:  https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#c-support
Sending telemetry:  https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet#send-simulated-telemetry