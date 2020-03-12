#!/bin/bash

##########################################################################
##  Deploys sample solution
##
##  Takes 1 parameter:
##
##  1- Name of resource group
##  2- Deployment Type:  iotOnly, cosmos, adx or all

rg=$1
type=$2

echo "Resource group:  $rg"
echo "Deployment Type:  $type"

echo
echo "Deploying ARM template"

az group deployment create -n "deploy-$(uuidgen)" -g $rg \
    --template-file deploy.json \
    --parameters deploymentType=$type

