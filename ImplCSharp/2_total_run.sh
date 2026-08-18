#!/usr/bin/env bash
#
# Script to run the C# file based application with source in `2_total.cs` file
#
# Make sure .NET 10 SDK is installed:
# - sudo add-apt-repository ppa:dotnet/backports -y
# - sudo apt install dotnet-sdk-10.0
#

dotnet run 2_total.cs
