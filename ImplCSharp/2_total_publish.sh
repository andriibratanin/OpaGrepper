#!/usr/bin/env bash
# Script to publish the C# file based application with source in `2_total.cs` file into a stand-alone executable
# Note: result will be in the "publish" subdirectory

dotnet publish 2_total.cs \
    -r linux-x64 \
    -c Release \
    --self-contained true \
    -p:DebugSymbols=false \
    -p:DebugType=None \
    -p:PublishAot=false \
    -p:PublishSingleFile=true \
    -o "publish"
