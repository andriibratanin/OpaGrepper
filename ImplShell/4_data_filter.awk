# read all filter strings into the "filters" array
NR == FNR {
    # Remove trailing ^M (carriage return) if the filter file uses
    # Windows CRLF line endings
    sub(/\r$/, "", $0)

    # Remove leading whitespace
    sub(/^[[:space:]]+/, "", $0)

    # Ignore empty lines and comments
    if ($0 == "" || $0 ~ /^#/) {
        next
    }

    # Store filters in lowercase for case-insensitive matching
    filters[++n] = tolower($0)
    next
}

# always include header from source file
FNR == 1 {
    print
    next
}

# process every remaining line of the source CSV
{
    # Convert the current line to lowercase once
    line = tolower($0)

    # check the current line against every filter string
    for (i = 1; i <= n; i++) {
        # index() returns:
        #   > 0  if the filter string is contained somewhere in the line
        #   1    if the line starts with the filter string
        #   0    if the filter string is not found
        #
        # index() performs literal string matching, so characters such as
        # ., +, (, [ etc. in the filter file don't get interpreted as
        # regular-expression syntax.
        if (index(line, filters[i]) > 0) { # contains
        #if (index(line, filters[i]) == 1) { # starts with
            print
            next
        }
    }
}
