FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-amd64 AS builder

COPY "2_total.cs" .

RUN dotnet publish "2_total.cs" \
    -r linux-musl-x64 \
    -c Release \
    --self-contained true \
    -p:DebugSymbols=false \
    -p:DebugType=None \
    -p:PublishAot=false \
    -p:PublishSingleFile=true \
    -o publish

FROM alpine:3.24 AS final

LABEL \
    org.opencontainers.image.title="Open Data Grepper" \
    org.opencontainers.image.description="Grep Open Data without caching them locally" \
    org.opencontainers.image.version="v2.0" \
    org.opencontainers.image.source="https://github.com/andriibratanin/OpaGrepper" \
    org.opencontainers.image.authors="Andrii Bratanin"
#    org.opencontainers.image.revision="${COMMIT}" \

#RUN apk add --no-cache libgcc libstdc++ icu-libs
# Note: the app will successfully run even without the "icu-libs" if the "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT" environment variable set
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
RUN apk add --no-cache libgcc libstdc++

WORKDIR "/App"

COPY --link --from=builder publish/ .

ENV SOURCE_URL=""
ENV ICONV="true"
ENV RESULT_DIR="/Data"

VOLUME "/Data"

ENTRYPOINT ["./2_total"]
