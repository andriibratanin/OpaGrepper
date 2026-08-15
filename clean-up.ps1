# Script to clean up the workspace
$ErrorActionPreference = 'SilentlyContinue'

# Cached data
Remove-Item "Data\28-ex_csv_asvp*.*"
Remove-Item "Data\29-ex_csv_erb*.*"

# Timestamps
Remove-Item "ImplCSharp\*.timestamp.txt"
Remove-Item "ImplShell\*.timestamp.txt"

# Temporary uncomplete data
Remove-Item "ImplCSharp\*.tmp"
Remove-Item "ImplShell\*.tmp"

# Results
Remove-Item "ImplCSharp\*.result.csv"
Remove-Item "ImplShell\5_result.csv"

# Filters (warning: make sure you know what are you doing)
#Remove-Item "ImplCSharp\*.filter.csv"
#Remove-Item "ImplShell\*.filter.csv"
