#!/usr/bin/env bash

set -euo pipefail

source ./0_common.sh

if [[ ! -f "$CSV_FILE" ]]; then
    echo "Input file not found: $CSV_FILE" >&2
    exit 1
fi

echo "Converting $CSV_FILE from Windows-1251 to UTF-8..."

tmp="${CSV_CONVERTED_FILE}.tmp"

if iconv -f WINDOWS-1251 -t UTF-8 "$CSV_FILE" > "$tmp"; then
    mv "$tmp" "$CSV_CONVERTED_FILE"
    rm -f "$CSV_FILE"
    echo "Conversion successful: $CSV_CONVERTED_FILE"
else
    rm -f "$tmp"
    echo "Conversion failed. Original file was preserved." >&2
    exit 1
fi
