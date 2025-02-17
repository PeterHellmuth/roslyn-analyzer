#!/bin/bash

# Clean analyzer project
cd DemoAnalyzers
rm -rf bin obj

# Generate semantic version (wildcard-friendly)
VERSION=1.0.$(date +%s)
echo "Building version: $VERSION"

# Build & pack analyzers
dotnet build -c Release
dotnet pack -c Release -p:Version=$VERSION -o ../packages

# Update demo project (wildcard remains in .csproj)
cd ../DemoProject
rm -rf bin obj .vs .vscode

# Force restore with latest version
dotnet restore --source ../packages --force-evaluate

echo "Updated to analyzer version $VERSION"