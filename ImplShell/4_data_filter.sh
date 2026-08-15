#!/usr/bin/env bash

set -euo pipefail

source ./0_common.sh

FILTER="4_data_filter.txt"
OUTPUT="5_data_result.csv"

if [[ ! -f "$CSV_CONVERTED_FILE" ]]; then
    echo "Input file not found: $CSV_CONVERTED_FILE" >&2
    exit 1
fi

if [[ ! -f "$FILTER" ]]; then
    echo "Filter file not found: $FILTER" >&2
    exit 1
fi

echo "Searching $CSV_CONVERTED_FILE using patterns from $FILTER..."

# First implementation
# -F = fixed/literal strings, not regular expressions
# -f = read search strings from filter.txt
#grep -F -f "$FILTER" "$CSV_CONVERTED_FILE" > "$OUTPUT" || {
#    status=$?
#    # grep returns 1 when there are simply no matches.
#    # Treat that as a successful search.
#    if [[ $status -ne 1 ]]; then
#        echo "grep failed." >&2
#        exit "$status"
#    fi
#}

# Second implementation
# Add ^ to the beginning of every filter string.
# -F keeps the filter strings literal.
#sed 's/^/^/' "$FILTER" > "${FILTER}.tmp"
#grep -F -f "$FILTER.tmp" "$CSV_CONVERTED_FILE" > "$OUTPUT" || {
#    status=$?
#    # grep returns 1 when there are simply no matches.
#    # Treat that as a successful search.
#    if [[ $status -ne 1 ]]; then
#        rm -f "${FILTER}.tmp"
#        echo "grep failed." >&2
#        exit "$status"
#    fi
#}
#rm -f "${FILTER}.tmp"

# Third implemenation
#awk -f - "$FILTER" "$CSV_CONVERTED_FILE" > "$OUTPUT" <<'AWK'
## read all filter strings into the "filters" array
#NR == FNR {
#    # Remove trailing ^M (carriage return) if the filter file uses Windows CRLF line endings
#    sub(/\r$/, "", $0)
#
#    filters[++n] = $0
#    next
#}
#
## always include header from source file
#FNR == 1 {
#    print
#    next
#}
#
## process every remaining line of the source CSV
#{
#    # check the current line against every filter string
#    for (i = 1; i <= n; i++) {
#        # index() returns:
#        #   > 0  if the filter string is contained somewhere in the line
#        #   1    if the line starts with the filter string
#        #   0    if the filter string is not found
#        # index() performs literal string matching, so characters such as ., +, (, [ etc. in the filter file don't get interpreted as regular-expression syntax
#        if (index($0, filters[i]) > 0) { # contains
#        #if (index($0, filters[i]) == 1) { # starts with
#            print
#            next
#        }
#    }
#}
#AWK

# Fourth implmentation
awk -f 4_data_filter.awk "$FILTER" "$CSV_CONVERTED_FILE" > "$OUTPUT"

echo "Done: $OUTPUT"
