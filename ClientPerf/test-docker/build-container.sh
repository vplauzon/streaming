#	Build Console App in release mode
dotnet build ../../ClientPerf/ -c Release

#	Remove temp directory
rm temp -r

#	Create temp directory
mkdir temp

#	Copy code
cp -r ../ClientConsole temp

#	Build docker container
sudo docker build -t vplauzon/client-perf-event-hub .

#	Publish image
sudo docker push vplauzon/client-perf-event-hub
