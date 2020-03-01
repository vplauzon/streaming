#!/bin/bash

##########################################################################
##  Deploys aks cluster
##
##  Takes 1 parameter:
##
##  1- Name of resource group

rg=$1

kv="1.15.7"

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
    --min-count 1 \
    --max-count 25 \
    --generate-ssh-keys \
    --nodepool-name feeder \
    --enable-cluster-autoscaler \
    --enable-vmss \
    --kubernetes-version $kv

echo
echo "Add node pool"

az aks nodepool add \
    -g $rg \
    --cluster-name $clusterName \
    --name peeker \
    --node-count 1 \
    --kubernetes-version $kv