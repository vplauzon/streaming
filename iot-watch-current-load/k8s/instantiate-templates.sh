#!/bin/bash

##########################################################################
##  Instantiate templates
##
##  Takes 1 parameter:
##
##  1- Name of resource group

rg=$1

echo "Resource group:  $rg"

echo
echo "Retrieving IoT Hub connection string"

iotConnectionString=$(az iot hub show-connection-string \
    -g $rg \
    --query "[0].connectionString[0]" \
    -o tsv)

echo
echo "Retrieving App Insights instrumentation key"

appInsightsKey=$(az monitor app-insights component show \
    -g $rg \
    --query "[0].instrumentationKey" \
    -o tsv)

echo
echo "Instantiating hub-feeder.yaml"

#   Escape connection string that might contain '/' in it:
escapedIotConnectionString=$(echo $iotConnectionString|sed -e 's/[\/&]/\\&/g')

#   Find and replace tokens
sed "s/{app-insights-key}/$appInsightsKey/g" hub-feeder-template.yaml \
    | sed "s/{iot-connection-string}/$escapedIotConnectionString/g" \
    > hub-feeder.yaml

