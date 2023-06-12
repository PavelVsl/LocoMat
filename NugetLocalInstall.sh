#!/bin/bash

# build
dotnet build -c Release
# package file to publish
dotnet pack -c Release
# get the latest nupkg file in the .nupkg directory
latest_nupkg=$(ls -t .nupkg/*.nupkg 2>/dev/null | head -1)

if [ -z "$latest_nupkg" ]; then
  echo "No .nupkg files found"
  exit 1
fi

# install package as global tool
dotnet tool update --global --prerelease --add-source .nupkg/ locomat

echo "Done"
