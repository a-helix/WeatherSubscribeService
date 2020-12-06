#!/bin/bash
sudo rm -r src
dotnet build WeatherSubscribeService.sln -c Release -o "src/"
docker build Dockerfile -t subscribe
