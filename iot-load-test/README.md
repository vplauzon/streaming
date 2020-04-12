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

## Load test

### Hub Feeder performance

\# Nodes|Message filler size|\# Gateways|\# Devices|Concurrency|# events / min
-|-|-|-|-|-
1|3 kb|1|1|1|5 400
1|3 kb|10|1|5|22 500

Ratio 100:1 for devices vs threads yield very unstable throughput

### Cosmos DB performance

ASA Unit|Cosmos RU|\# Gateways|\# Devices|#events / s|Latency
-|-|-|-|-|-
1|4000|1|1|90|0.2s

## Query hub-feeder in App Insights

```
//  Messages per minute, plot on a time chart
customMetrics
| where cloud_RoleName == "HUB-FEEDER"
| where name=="message-count"
| summarize throughputPerSec=sum(valueSum) by bin(timestamp, 1m)
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

SELECT TOP 1 c.recordedAt, c.IoTHub.EnqueuedTime FROM c ORDER BY c._ts DESC

SELECT TOP 1 * FROM c WHERE c.deviceId=<deviceId> ORDER BY c._ts DESC

SELECT VALUE MAX(c._ts) FROM c ORDER BY c._ts DESC

SELECT DISTINCT c.gatewayId FROM c WHERE c._ts><MAX - XYZ>

SELECT MAX(c._ts) AS timestamp, c.deviceId
FROM c
WHERE c.gatewayId=<XYZ>
GROUP BY c.deviceId
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
