#!/bin/bash
if [[ -d src ]]
	then
		echo "Deleting old src..."
		sudo rm -r src
		echo "Done."
fi
echo "Building WeatherSubscribeService..."
cd ..
dotnet build WeatherSubscribeService.sln -c Release -o "src/"
echo "WeatherSubscribeService has been built."
echo "Building container..."
cd Deploy
docker build Dockerfile -t subscribe
echo "Container has been built."