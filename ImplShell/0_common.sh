#!/usr/bin/env bash
# Script with common variables used in several places

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    echo "Error: this script must be sourced, not executed." >&2
    exit 1
fi

# Data source configuration
#> Executive proceedings data dump
FILENAME="28-ex_csv_asvp"
URL="https://data.gov.ua/dataset/22aef563-3e87-4ed9-92e8-d764dc02f426/resource/d1a38c08-0f3a-4687-866f-f28f50df7c46/download/$FILENAME.zip"
#> Debtors data dump
#FILENAME="29-ex_csv_erb"
#URL="https://data.gov.ua/dataset/783b9b50-faba-4cc9-a393-60485e395b1d/resource/e6ea76c1-01f4-4bd0-a282-7d92d6ecc2a1/download/$FILENAME.zip"

# Dynamic configuration
DATA_DIR="../Data"
ARCHIVE_FILE="$DATA_DIR/$FILENAME.zip"
CSV_FILE="$DATA_DIR/$FILENAME.csv"
CSV_CONVERTED_FILE="$DATA_DIR/${FILENAME}_converted.csv"
