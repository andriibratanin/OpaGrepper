#!/usr/bin/env bash
# Script to clean up the workspace

# Cached data
rm -f Data/28-ex_csv_asvp*.*
rm -f Data/29-ex_csv_erb*.*

# Timestamps
rm -f ImplCSharp/*.timestamp.txt
rm -f ImplShell/*.timestamp.txt

# Temporary incomplete data
rm -f ImplCSharp/*.tmp
rm -f ImplShell/*.tmp

# Results
rm -f ImplCSharp/*.result.csv
rm -f ImplShell/5_result.csv

# Filters (warning: make sure you know what you are doing)
# rm -f ImplCSharp/*.filter.csv
# rm -f ImplShell/*.filter.csv
