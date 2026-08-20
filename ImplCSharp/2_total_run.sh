#!/usr/bin/env bash
#
# Script to run the C# file based application with source in `2_total.cs` file
#
# Make sure .NET 10 SDK is installed:
# - sudo add-apt-repository ppa:dotnet/backports -y
# - sudo apt install dotnet-sdk-10.0
#

# Data source URL
# Executive proceedings
export SOURCE_URL="https://data.gov.ua/dataset/22aef563-3e87-4ed9-92e8-d764dc02f426/resource/d1a38c08-0f3a-4687-866f-f28f50df7c46/download/28-ex_csv_asvp.zip"
# Debtors
#export SOURCE_URL="https://data.gov.ua/dataset/783b9b50-faba-4cc9-a393-60485e395b1d/resource/e6ea76c1-01f4-4bd0-a282-7d92d6ecc2a1/download/29-ex_csv_erb.zip"

# Flag signaling that Win1251 -> UTF-8 re-encoding is needed
# Note: must be exactly "true" or "false" string (case insensitive, default is "true")
#export ICONV="true"
#export ICONV="false"

# Where to place results
# Note: this is not a "Data" (possibly pre-cached data source) directory location
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
export RESULT_DIR="$(realpath "$SCRIPT_DIR")"

dotnet run 2_total.cs

#unset SOURCE_URL
#unset ICONV
#unset RESULT_DIR
