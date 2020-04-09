# IoT Load test

This is a load test to simulate a bunch of devices pushing telemetry to Azure IoT hub.  The telemetry is then ingested in some data service and random poke are done at the "current" (i.e. most up-to-date) telemetry.

We are comparing two data services:  Azure Cosmos DB & Azure Data Explorer.

Likely need to install both IoT Hub + App Insights extension on Azure CLI (cf https://docs.microsoft.com/en-us/cli/azure/azure-cli-extensions-overview?view=azure-cli-latest):

*   az extension add --name application-insights
*   az extension add --name azure-iot

Register devices:  https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#c-support
Sending telemetry:  https://docs.microsoft.com/en-us/azure/iot-hub/quickstart-send-telemetry-dotnet#send-simulated-telemetry

Container:  https://hub.docker.com/repository/docker/vplauzon/perf-streaming

kubectl apply -f hub-feeder.yaml

Limits:

4Kb:  https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling#quotas-and-throttling
https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling
https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-scaling

## Query hub-feeder in App Insights

```
//  Messages per second, plot on a time chart
customMetrics
| where cloud_RoleName == "HUB-FEEDER"
| where name=="message-count"
| summarize throughputPerSec=sum(valueSum)/60.0 by bin(timestamp, 1m)
| render timechart

exceptions
| where cloud_RoleName == "HUB-FEEDER"
| sort by timestamp desc
| limit 10
```

## Query Stream Analytics on Log Analytics

https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-monitoring#metrics-available-for-stream-analytics

//  All metrics
AzureMetrics
| where ResourceProvider == "MICROSOFT.STREAMANALYTICS"
| distinct MetricName
| order by MetricName asc

//  Both input and output events:  count per second
AzureMetrics
| where ResourceProvider == "MICROSOFT.STREAMANALYTICS"
| where MetricName == "InputEvents" or MetricName == "OutputEvents" 
| summarize Count=sum(Total)/60 by MetricName, bin(TimeGenerated, 1m)
| render timechart 

//  Both input and output events:  bytes per second
AzureMetrics
| where ResourceProvider == "MICROSOFT.STREAMANALYTICS"
| where MetricName == "InputEventBytes" or MetricName == "OutputEventBytes" 
| summarize Count=sum(Total)/60 by MetricName, bin(TimeGenerated, 1m)
| render timechart 

//  Backlog
AzureMetrics
| where ResourceProvider == "MICROSOFT.STREAMANALYTICS"
| where MetricName == "InputEventsSourcesBacklogged"
| summarize Count=max(Total) by MetricName, bin(TimeGenerated, 1m)
| render timechart 

##  Query Cosmos DB on Log Analytics

```
SELECT COUNT(1) FROM c

SELECT TOP 1 * FROM c ORDER BY c._ts DESC

SELECT TOP 1 * FROM c WHERE c.deviceId=<deviceId> ORDER BY c._ts DESC

SELECT VALUE MAX(c._ts) FROM c ORDER BY c._ts DESC

SELECT DISTINCT c.gatewayId, c.deviceId FROM c WHERE c._ts><MAX - XYZ>
```

//  See https://docs.microsoft.com/en-us/azure/cosmos-db/cosmosdb-monitor-resource-logs#diagnostic-queries

//  All categories
AzureDiagnostics 
| where ResourceProvider=="MICROSOFT.DOCUMENTDB" 
| distinct Category

//  Cost of writes, total
AzureDiagnostics 
| where ResourceProvider=="MICROSOFT.DOCUMENTDB" 
| where Category=="DataPlaneRequests" 
| summarize ru=sum(toreal(requestCharge_s))/60 by bin(TimeGenerated, 1m)
| render timechart 