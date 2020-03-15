# IoT Watch Current Load test

This is a load test to simulate a bunch of devices pushing telemetry to Azure IoT hub.  The telemetry is then ingested in some data service and random poke are done at the "current" (i.e. most up-to-date) telemetry.

We are comparing two data services:  Azure Cosmos DB & Azure Data Explorer.

Likely need to install both IoT Hub + App Insights extension on Azure CLI (cf https://docs.microsoft.com/en-us/cli/azure/azure-cli-extensions-overview?view=azure-cli-latest):

*   az extension add --name application-insights
*   az extension add --name azure-iot

Register devices:  https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#c-support
Sending telemetry:  https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet#send-simulated-telemetry

Container:  https://hub.docker.com/repository/docker/vplauzon/perf-streaming

kubectl apply -f hub-feeder.yaml

```sql
//  Metrics
customMetrics
| where timestamp > ago(10m)
| where cloud_RoleName == "HUB-FEEDER"
| project-reorder cloud_RoleInstance
| sort by timestamp desc, cloud_RoleInstance asc

//  Message-count by bin and instances
customMetrics
| where timestamp > ago(10m)
| where cloud_RoleName == "HUB-FEEDER"
| where name=="message-count"
| summarize messages=sum(valueSum) by bin(timestamp, 1m), cloud_RoleInstance
| sort by timestamp desc, cloud_RoleInstance asc

//  Chart, per second
customMetrics
| where timestamp > ago(30m)
| where cloud_RoleName == "HUB-FEEDER"
| where name=="message-count"
| summarize throughputPerSec=sum(valueSum)/60.0 by bin(timestamp, 1m)
| render columnchart 

//  Chart, per minute
customMetrics
| where timestamp > ago(30m)
| where cloud_RoleName == "HUB-FEEDER"
| where name=="message-count"
| summarize throughputPerMin=sum(valueSum) by bin(timestamp, 1m)
| render columnchart 

exceptions 
| where timestamp > ago(10m)
| where cloud_RoleName == "HUB-FEEDER"
| sort by timestamp desc
| limit 10
```

Limits:

4Kb:  https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling#quotas-and-throttling
https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling
https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-scaling