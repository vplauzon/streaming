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

### Hub Feeder

\# Feeder Nodes|Message filler size|\# Gateways|\# Devices|Concurrency|# events / min|IoT Scale
-|-|-|-|-|-|-
1|3 kb|1|1|1|5 400|1 x B3
1|3 kb|1 000|1|1|4 500|1 x B3
1|3 kb|200|15|1|4 500|1 x B3
1|3 kb|1 000|15|2|9 000|1 x B3
1|3 kb|1 000|15|4|15 500|1 x B3
1|3 kb|30 000|15|?|~450 000|2 x B3

Ratio 100:1 for devices vs threads yield very unstable throughput

### Cosmos DB

ASA Unit|Cosmos RU|\# Gateways|\# Devices|#events / min|Latency
-|-|-|-|-|-
1|4 000|1 000|1|4 500|<1s
3|4 000|200|15|4 500|<1s
6|5 000|1 000|15|15 500|<1s

### Kusto

Cluster|SKUs|\# Gateways|\# Devices|#events / min|Latency
-|-|-|-|-|-
Standard|2xD14v2|1 000|15|15 500|(0s, 40s)
Streaming|2xD14v2|1 000|15|15 500|(0s, 40s)

## Query hub-feeder in App Insights

```
//  Messages per minute, plot on a time chart
customMetrics
| where cloud_RoleName == "HUB-FEEDER"
| where name=="message-count"
| summarize throughputPerSec=sum(valueSum) by bin(timestamp, 1m)
| render timechart

//  Exception happening in code
exceptions
| where cloud_RoleName == "HUB-FEEDER"
| sort by timestamp desc
| limit 10
```

## Query Stream Analytics on Log Analytics

https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-monitoring#metrics-available-for-stream-analytics

//  ASA Resource utilisation (%)
AzureMetrics
| where ResourceProvider == "MICROSOFT.STREAMANALYTICS"
| where MetricName == "ResourceUtilization"
| summarize sum(Count*Average) by bin(TimeGenerated, 1m)
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

SELECT TOP 1 c.id, c.gatewayId, c.deviceId, c.recordedAt, c.EventProcessedUtcTime FROM c ORDER BY c.recordedAtTs DESC

SELECT TOP 1 * FROM c WHERE c.deviceId=<deviceId> ORDER BY c._ts DESC

SELECT VALUE MAX(c._ts) FROM c ORDER BY c._ts DESC

SELECT DISTINCT c.gatewayId FROM c WHERE c._ts><MAX - XYZ>

SELECT MAX(c._ts) AS timestamp, c.deviceId
FROM c
WHERE c.gatewayId=<XYZ>
GROUP BY c.deviceId
```

//  See https://docs.microsoft.com/en-us/azure/cosmos-db/cosmosdb-monitor-resource-logs#diagnostic-queries

//  RUs/s
AzureDiagnostics 
| where ResourceProvider=="MICROSOFT.DOCUMENTDB" 
| where Category=="DataPlaneRequests" 
| summarize ru=sum(toreal(requestCharge_s))/60 by bin(TimeGenerated, 1m)
| render timechart 
