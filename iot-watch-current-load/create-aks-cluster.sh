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

#   Fetch unique ID used in the resource group
#   We've explicitely attached it as a tag in the ARM template
uniqueId=$(az resource list \
    --resource-type Microsoft.Devices/IotHubs \
    -g $rg \
    --query "[0].tags.uniqueId" \
    -o tsv)
#   With it, we can determined the AKS cluster name
clusterName="aks-$uniqueId"

echo
echo "Unique ID:  $uniqueId"
echo "Cluster Name:  $clusterName"

echo
echo "Deploy AKS Cluster"

az aks create \
    -g $rg \
    -n $clusterName \
    --node-count 2 \
    --min-count 1 \
    --max-count 100 \
    --generate-ssh-keys \
    --nodepool-name default \
    --enable-cluster-autoscaler \
    --kubernetes-version $kv

echo
echo "Login into AKS Cluster"

az aks get-credentials -g $rg -n $clusterName

echo
echo "Instantiate templates"

./instantiate-template.sh $rg