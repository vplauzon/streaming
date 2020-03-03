# IoT Watch Current Load test

This is a load test to simulate a bunch of devices pushing telemetry to Azure IoT hub.  The telemetry is then ingested in some data service and random poke are done at the "current" (i.e. most up-to-date) telemetry.

We are comparing two data services:  Azure Cosmos DB & Azure Data Explorer.

Likely need to install both IoT Hub + App Insights extension on Azure CLI (cf https://docs.microsoft.com/en-us/cli/azure/azure-cli-extensions-overview?view=azure-cli-latest):

*   az extension add --name application-insights
*   az extension add --name azure-iot

Register devices:  https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#c-support
Sending telemetry:  https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet#send-simulated-telemetry

Container:  https://hub.docker.com/repository/docker/vplauzon/perf-streaming

kubectl apply -f service.yaml

```sql
customMetrics
| where timestamp > ago(10m)
| sort by timestamp desc, cloud_RoleName asc
```

Limits:

https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling
