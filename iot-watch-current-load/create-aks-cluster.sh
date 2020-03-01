#!/bin/bash

##########################################################################
##  Deploys aks cluster
##
##  Takes 1 parameter:
##
##  1- Name of resource group

rg=$1

echo "Resource group:  $rg"

echo
echo "Retrieving unique-id"

uniqueId=$(az resource list \
    --resource-type Microsoft.Devices/IotHubs \
    -g $rg \
    --query "[0].tags.uniqueId" \
    -o tsv)
clusterName="aks-$uniqueId"

echo
echo "Unique ID:  $uniqueId"
echo "Cluster Name:  $clusterName"

echo
echo "Deploy AKS Cluster"

az aks create \
    -g $rg \
    -n $clusterName \
    --node-count 1 \
    --generate-ssh-keys \
    --nodepool-name hub-feeder \
    --enable-cluster-autoscaler true \
    --enable-vmss \
    