#!/usr/bin/env bash

set -euo pipefail

source ./0_common.sh

TIMESTAMP_FILE="$FILENAME.timestamp.txt"

# Resource is considered stale after NN seconds.
MAX_AGE=$((23 * 60 * 60)) # 23 * 60 * 60 seconds = 23 hours

now=$(date +%s)

download() {
    echo "Downloading $URL..."

    # Download to a temporary file first so a failed/incomplete download does not replace the existing resource.
    tmp="${ARCHIVE_FILE}.tmp"

    if curl -fL --retry 3 -o "$tmp" "$URL"; then
        mv "$tmp" "$ARCHIVE_FILE"
        printf '%s\n' "$now" > "$TIMESTAMP_FILE"
        echo "Download successful."
    else
        rm -f "$tmp"
        echo "Download failed." >&2
        exit 2
    fi
}

# No timestamp file -> download.
if [[ ! -f "$TIMESTAMP_FILE" ]]; then
    echo "Timestamp file not found."
    download
    exit 0
fi

saved_timestamp=$(cat "$TIMESTAMP_FILE")

# Make sure the timestamp is a valid integer.
if ! [[ "$saved_timestamp" =~ ^[0-9]+$ ]]; then
    echo "Invalid timestamp file."
    download
    exit 0
fi

age=$((now - saved_timestamp))

# Timestamp is in the future or is younger than 23 hours.
if (( age < MAX_AGE )); then
    echo "Timestamp is fresh ($((age / 3600)) hours old). No download needed."
    exit 1
fi

echo "Timestamp is stale ($((age / 3600)) hours old). Checking remote resource..."

# HEAD request and extract Last-Modified.
last_modified=$(
    curl -fsIL "$URL" |
    awk 'BEGIN { IGNORECASE=1 }
         /^Last-Modified:/ {
             sub(/^[^:]*:[[:space:]]*/, "")
             print
             exit
         }'
)

# Some servers don't provide Last-Modified.
if [[ -z "$last_modified" ]]; then
    echo "Server did not provide Last-Modified; downloading."
    download
    exit 0
fi

remote_timestamp=$(date -d "$last_modified" +%s 2>/dev/null || true)

if [[ -z "$remote_timestamp" ]]; then
    echo "Could not parse Last-Modified: $last_modified"
    echo "Downloading anyway."
    download
    exit 0
fi

if (( remote_timestamp > saved_timestamp )); then
    echo "Remote resource is newer."
    download
else
    echo "Remote resource has not changed."
    printf '%s\n' "$now" > "$TIMESTAMP_FILE"
    exit 1
fi
