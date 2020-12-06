#!/bin/bash
sudo rm -r src
dotnet build WeatherSubscribeService.sln -c Release -o "src/"
sudo rm -r /src
sudo mv src /src
