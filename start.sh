#!/bin/bash
rm -r src
dotnet build WeatherSubscribeService.sln -c Release -o "src/"
sudo cp -avr src /src/
