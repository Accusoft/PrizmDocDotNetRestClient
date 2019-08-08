#!/usr/bin/env bash
set -e # stop on error
set +x # do not echo commands

cd PrizmDocRestClient.Tests

TARGET_FRAMEWORK=$1

# Modify the test project file to only target the specified .NET framework
sed -i "s/<TargetFrameworks>.*<\/TargetFrameworks>/<TargetFramework>${TARGET_FRAMEWORK}<\/TargetFramework>/" PrizmDocRestClient.Tests.csproj

# dotnet restore is only needed on .NET Core 1.x SDK.
# See https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-restore?tabs=netcore2x
if [[ ${TARGET_FRAMEWORK} == "netcoreapp1."* ]]; then
  dotnet restore --no-dependencies
fi

# Build only the tests. Do not rebuild the DLL we are testing!
dotnet build --no-dependencies --configuration Release

# Run the tests. Since we just built the tests, use --no-build to ensure we don't build the tests (or the DLL we are testing) again.
dotnet test --no-build --configuration Release
