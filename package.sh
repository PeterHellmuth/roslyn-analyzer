#!/bin/bash

# Clean analyzer project
cd DemoAnalyzers
rm -rf bin obj

# Set a fixed version number
VERSION=1.0.0
echo "Building version: $VERSION"

# Delete the existing package if it exists
rm -f ../packages/DemoAnalyzers.$VERSION.nupkg

# Build & pack analyzers
dotnet build -c Release
dotnet pack -c Release -p:Version=$VERSION -o ../packages

# Clean up old versions of the package
find ../packages -name "DemoAnalyzers.*.nupkg" -not -name "DemoAnalyzers.$VERSION.nupkg" -delete

# Clear the NuGet cache
dotnet nuget locals all --clear

# Update demo project
cd ../DemoProject
rm -rf bin obj .vs .vscode

# Force restore with latest version
dotnet restore --source ../packages --force-evaluate

echo "Updated to analyzer version $VERSION"