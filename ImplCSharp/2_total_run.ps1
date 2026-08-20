# Script to run the C# file based application with source in `2_total.cs` file

# Data source URL
# Executive proceedings
$env:SOURCE_URL = "https://data.gov.ua/dataset/22aef563-3e87-4ed9-92e8-d764dc02f426/resource/d1a38c08-0f3a-4687-866f-f28f50df7c46/download/28-ex_csv_asvp.zip"
# Debtors
#$env:SOURCE_URL = "https://data.gov.ua/dataset/783b9b50-faba-4cc9-a393-60485e395b1d/resource/e6ea76c1-01f4-4bd0-a282-7d92d6ecc2a1/download/29-ex_csv_erb.zip"

# Flag signaling that Wind1251 -> UTF-8 re-encoding is needed
# Note: must be exactly "true" or "false" string (case insensitive, default is "true")
#$env:ICONV = "true"
#$env:ICONV = "false"

# Where to place results
# Note: this is not a "Data" (possibly pre-cached data source) directory location
$env:RESULT_DIR = (Resolve-Path (Join-Path $PSScriptRoot ".")).Path

dotnet run 2_total.cs

#$env:SOURCE_URL = $null
#$env:ICONV = $null
#$env:RESULT_DIR = $null
