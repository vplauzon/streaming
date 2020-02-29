# IoT Watch Current Load test

This is a load test to simulate a bunch of devices pushing telemetry to Azure IoT hub.  The telemetry is then ingested in some data service and random poke are done at the "current" (i.e. most up-to-date) telemetry.

We are comparing two data services:  Azure Cosmos DB & Azure Data Explorer.