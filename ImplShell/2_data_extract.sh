#!/usr/bin/env bash

set -euo pipefail

source ./0_common.sh

if [[ ! -f "$ARCHIVE_FILE" ]]; then
    echo "Archive not found: $ARCHIVE_FILE" >&2
    exit 1
fi

echo "Extracting $ARCHIVE_FILE..."

if unzip -o "$ARCHIVE_FILE" -d "$DATA_DIR/"; then
    echo "Extraction successful."
    #rm -f "$ARCHIVE_FILE"
    #echo "Deleted $ARCHIVE_FILE."
else
    echo "Extraction failed. Keeping $ARCHIVE_FILE." >&2
    exit 1
fi
