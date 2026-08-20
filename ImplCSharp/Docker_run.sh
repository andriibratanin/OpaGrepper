#!/usr/bin/env bash
# Script to run a built Docker image

# Note: to debug container internals run:
# docker run --rm -it -v ../Data:/Data --entrypoint /bin/sh opa-grepper

# Executive proceedings
docker run --rm \
    -e SOURCE_URL="https://data.gov.ua/dataset/22aef563-3e87-4ed9-92e8-d764dc02f426/resource/d1a38c08-0f3a-4687-866f-f28f50df7c46/download/28-ex_csv_asvp.zip" \
    -e ICONV="true" \
    -e RESULT_DIR="/Result" \
    -v ../Data:/Data \
    -v .:/Result \
    opa-grepper

# Debtors
docker run --rm \
    -e SOURCE_URL="https://data.gov.ua/dataset/783b9b50-faba-4cc9-a393-60485e395b1d/resource/e6ea76c1-01f4-4bd0-a282-7d92d6ecc2a1/download/29-ex_csv_erb.zip" \
    -e ICONV="true" \
    -e RESULT_DIR="/Result" \
    -v ../Data:/Data \
    -v .:/Result \
    opa-grepper
