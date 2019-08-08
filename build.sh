#!/usr/bin/env bash
set -e # stop on error
set +x # do not echo commands

cd PrizmDocRestClient

# Do a release build of the DLL we want to test and publish.
dotnet build --configuration Release
