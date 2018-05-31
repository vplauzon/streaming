# Summary Streaming

Deploys the solution elaborated in [this article](https://vincentlauzon.com/2018/05/22/taming-the-fire-hose-azure-stream-analytics/).

The solution uses Azure Stream Analytics to summarize a stream of events.

Uses a Docker Container to run initial SQL Script which is defined in a [folder](sql-docker) (i.e. not a Visual Studio project).

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fvplauzon%2Fstreaming%2Fmaster%2FSummaryStreaming%2FDeployment%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>
<a href="http://armviz.io/#/?load=https%3A%2F%2Fraw.githubusercontent.com%2Fvplauzon%2Fstreaming%2Fmaster%2FSummaryStreaming%2FDeployment%2Fazuredeploy.json" target="_blank">
    <img src="http://armviz.io/visualizebutton.png"/>
</a>