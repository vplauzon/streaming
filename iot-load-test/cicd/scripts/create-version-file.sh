#!/bin/bash

##########################################################################
##  Create version file
##
##  Inputs:
##      version:    Full version of the container
##      outputPath: Path of the output file

version=$1
outputPath=$2

echo
echo "Version:  $version"
echo "Output Path:  $outputPath"

echo $version > $outputPath