#!/bin/bash
sudo rm -r src
echo "Building WeatherSubscribeService..."
dotnet build WeatherSubscribeService.sln -c Release -o "src/"
echo "WeatherSubscribeService has been built."
echo "Building container..."
docker build Dockerfile -t subscribe
echo "Container has been built."