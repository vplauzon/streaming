#!/bin/bash

#	This file needs to have execution permission obviously:  chmod u+x build-container.sh

#	Build Console App in release mode
dotnet build ../../ClientPerf/ -c Release

#	Copy dll
cp ../ClientConsole/bin/Release/netcoreapp2.0/ClientConsole.dll .

#	Build docker container
sudo docker build -t client-perf-event-hub .