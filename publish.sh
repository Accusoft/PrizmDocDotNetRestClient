#!/usr/bin/env bash
set -e # stop on error
set +x # do not echo commands

cd PrizmDocRestClient

# Perform an explicit restore.
dotnet restore

# Create the package file.
dotnet pack --no-build --no-restore --configuration Release

# Push the package to nuget.org.
dotnet nuget push bin/Release/PrizmDocRestClient*.nupkg --api-key ${NUGET_API_KEY} -s https://api.nuget.org/v3/index.json
