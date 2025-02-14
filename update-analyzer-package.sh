#!/bin/bash

# Clean analyzer project
cd DemoAnalyzers
rm -rf bin obj

# Build with explicit output
dotnet build -c Release -p:OutputPath=./bin/Release/netstandard2.0/

# Generate unique version
VERSION=1.0.$(date +%s)

# Pack with explicit DLL reference
dotnet pack -c Release \
    -p:Version=$VERSION \
    -p:PackageOutputPath=../packages \
    -p:IncludeContentInPack=true \
    -p:ContentTargetFolders=analyzers/dotnet/cs \
    -p:NoBuild=true

# Clean and update demo project
cd ../DemoProject
rm -rf bin obj .vs .vscode
dotnet remove package DemoAnalyzers
dotnet add package DemoAnalyzers --version $VERSION --source ../packages