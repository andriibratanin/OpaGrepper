#!/usr/bin/env dotnet
//
// Script to grep through a government open data archives without downloading them
//
// Features:
// - Able to fetch a data archive (usually a ZIP compressed CSV file) and stream it right into the dynamic "decompress-reencode-search" pipeline (nothing intermediate gets stored on disk)
// - Able to use pre-cached archive as test data (thus skipping the Internet calls)
// - Data source throttling mechanism included
//
// Requirements:
// - .NET 10 SDK (https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
//
// How to run? This is a so-called ".NET file based application", use the following terminal command to start it:
// - `dotnet run 0_total.cs`
//
// Exit codes:
// - 0: success
// - 1: nothing to do / throttled
// - 2: error
//
// Copyright (C) Andrii Bratanin, 2026. Personal Use License
//

// SharpZipLib - https://github.com/icsharpcode/sharpziplib, MIT
#:package SharpZipLib@1.4.2

using ICSharpCode.SharpZipLib.Zip;
using System.Net;
using System.Text;

const string Version = "1.0";

const bool Win1251EncodedDataFile = true;
const string VersionString = Win1251EncodedDataFile
    ? $"eGov № {Version} THZCIG (throttle-http-zip-csv-iconv-grep)"
    : $"eGov № {Version} THZCG (throttle-http-zip-csv-grep)";

// Executive proceedings
const string DataSourceFilename = "28-ex_csv_asvp";
const string DataSourceUrl = $"https://data.gov.ua/dataset/22aef563-3e87-4ed9-92e8-d764dc02f426/resource/d1a38c08-0f3a-4687-866f-f28f50df7c46/download/{DataSourceFilename}.zip";
// Debtors
//const string DataSourceFilename = "29-ex_csv_erb";
//const string DataSourceUrl = $"https://data.gov.ua/dataset/783b9b50-faba-4cc9-a393-60485e395b1d/resource/e6ea76c1-01f4-4bd0-a282-7d92d6ecc2a1/download/{DataSourceFilename}.zip";

const string PreCachedArchiveFile = $@"..\Data\{DataSourceFilename}.zip";
const string ExpectedFileInArchive = $"{DataSourceFilename}.csv";

const string TimestampFile = $"{DataSourceFilename}.timestamp.txt";
const int TimestampFileMaxAgeMinutes = 23 * 60; // 23 hours

// The search logic is simple: data lines containing one of filter lines will be saved into result (i.e. it is a multiline grep)
const string FilterFile = $"{DataSourceFilename}.filter.txt";
const string OutputFile = $"{DataSourceFilename}.result.csv";

// Required for Windows-1251 encoding operations
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

try
{
    Console.WriteLine( "===========================================================");
    Console.WriteLine($" Open Data Grepper for '{DataSourceFilename}' ");
    Console.WriteLine($" {VersionString}");
    Console.WriteLine( "===========================================================");

    var nowWithMills = DateTimeOffset.UtcNow;
    var now = nowWithMills.AddTicks(-(nowWithMills.Ticks % TimeSpan.TicksPerSecond));
    //Console.WriteLine("");
    Console.WriteLine($"Current UTC timestamp: {now.UtcDateTime:O}");

    Console.WriteLine("");
    Console.WriteLine("Checking throttling conditions...");
    bool needsProcessing = NeedsProcessing(now);
    if (needsProcessing)
    {
        Console.WriteLine("");
        Console.WriteLine($"Loading filters: {FilterFile}...");

        var filters = LoadFilters(FilterFile);
        if (filters.Count == 0)
        {
            Console.WriteLine("No filters defined - nothing to do.");

            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine($"Loaded {filters.Count:D} filter(s)");
        Console.WriteLine("");

        Console.WriteLine("Downloading and processing new archive...");

        var result = await ProcessArchiveFromHttpAsync(
            DataSourceUrl,
            ExpectedFileInArchive,
            filters,
            OutputFile
        );

        Console.WriteLine("Download and processing complete");
        Console.WriteLine("");

        File.WriteAllText(TimestampFile, now.ToUnixTimeSeconds().ToString());

        Console.WriteLine($"Operation timestamp saved to: {TimestampFile}");
        Console.WriteLine("");

        Environment.ExitCode = 0;
    }
    else
    {
        Console.WriteLine("");
        Console.WriteLine($"WARNING: Throttling - too early to check for updates (data age is below the threshold of: {TimestampFileMaxAgeMinutes} minutes)");
        Console.WriteLine("          Please, don't abuse remote servers with frequent polls and respect the declared dataset's update schedule");
        Console.WriteLine("");
        Environment.ExitCode = 1;
    }

    Console.WriteLine($"Done.");
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"ERROR: {ex.Message}");
    Console.Error.WriteLine("----");
    Console.Error.WriteLine(ex);

    Environment.ExitCode = 2;
}


bool NeedsProcessing(DateTimeOffset now)
{
    if (File.Exists(PreCachedArchiveFile))
    {
        Console.WriteLine($"Local archive found: {PreCachedArchiveFile} - will use it as test data file");

        return true;
    }

    if (!File.Exists(TimestampFile))
    {
        Console.WriteLine("Timestamp file not found - assuming this is the first run");

        return true;
    }

    var text = File.ReadAllText(TimestampFile).Trim();

    if (!long.TryParse(text, out var unixTimestamp))
    {
        Console.WriteLine($"Timestamp is invalid: {text}");

        return true;
    }

    var savedTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
    Console.WriteLine($"Loaded UTC timestamp: {savedTime.UtcDateTime:O}");

    var age = now - savedTime;

    if (age < TimeSpan.Zero)
    {
        return false;
    }

    Console.WriteLine($"Supposed data age is: {age.TotalMinutes:F0} minutes");

    if (age >= TimeSpan.FromMinutes(TimestampFileMaxAgeMinutes))
    {
        Console.WriteLine("Data are stale");

        return true;
    }

    return false;
}

async Task<long> ProcessArchiveFromHttpAsync(
    string url,
    string csvFilename,
    List<string> filters,
    string outputFilename)
{
    string temporaryOutput = outputFilename + ".tmp";

    File.Delete(temporaryOutput);

    try
    {
        using var testDataFileHttpHandler = new TestDataFileHandler(
                    PreCachedArchiveFile,
                    TimestampFile,
                    new HttpClientHandler()
                );
        using var http = new HttpClient(testDataFileHttpHandler)
        {
            // Timeout = TimeSpan.FromHours(2)
        };

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        Console.WriteLine("");
        Console.WriteLine($"HTTP status: {(int)response.StatusCode} {response.StatusCode}");

        if (response.Content.Headers.ContentLength is long contentLength)
            Console.WriteLine($"Response ZIP content length: {contentLength:D} bytes");
        else
            Console.WriteLine("Response ZIP content length: unknown");

        Console.WriteLine("");
        Console.WriteLine("Streaming ZIP content...");
        await using var input = await response.Content.ReadAsStreamAsync();

        // The HTTP response stream is non-seekable.
        // SharpZipLib processes ZIP entries sequentially.
        using var zip = new ZipInputStream(input, 1024 * 1024);

        ZipEntry? entry;
        while ((entry = zip.GetNextEntry()) is not null)
        {
            Console.WriteLine($"\tZIP entry: {entry.Name}");

            if (entry.IsDirectory)
                continue;

            if (!string.Equals(entry.Name, csvFilename, StringComparison.Ordinal))
                continue;

            Console.WriteLine($"\t\tFound expected source CSV file: {entry.Name}");
            if (entry.CompressedSize >= 0)
                Console.WriteLine($"\t\tCSV compressed size: {entry.CompressedSize:D} bytes");
            else
                Console.WriteLine($"\t\tCSV compressed size: unknown");

            if (entry.Size >= 0)
                Console.WriteLine($"\t\tCSV uncompressed size: {entry.Size:D} bytes");
            else
                Console.WriteLine($"\t\tCSV uncompressed size: unknown");

            Console.WriteLine("");
            Console.WriteLine("Streaming CSV content into search filters...");
            var result = ProcessCsvStream(zip, filters, temporaryOutput);

            Console.WriteLine("CSV processing complete");

            File.Move(temporaryOutput, outputFilename, overwrite: true);
            Console.WriteLine($"Results were saved to: {outputFilename}");

            return result;
        }

        throw new FileNotFoundException($"Expected source CSV file '{csvFilename}' was not found inside the ZIP archive");
    }
    catch
    {
        // Never leave a partial result behind.
        File.Delete(temporaryOutput);

        throw;
    }
}

List<string> LoadFilters(string filename)
{
    // One literal search string per line.
    // Trailing CR (^M) is removed.
    // Empty lines are ignored.
    // Comment lines starting with "#" ignored.

    var filters = new List<string>();
    if (!File.Exists(filename))
        return filters;

    using var reader = new StreamReader(
        filename,
        Encoding.UTF8,
        detectEncodingFromByteOrderMarks: true
    );

    string? line;

    while ((line = reader.ReadLine()) is not null)
    {
        // Remove trailing Windows CR if present
        // Note: This is actually redundant, but left here for intention explicity
        line = line.TrimEnd('\r');

        // Ignore empty filter lines
        if (line.Length == 0)
            continue;

        // Ignore comment lines
        if (line.TrimStart().StartsWith('#'))
            continue;

        Console.WriteLine($"\t- `{line}`");

        filters.Add(line);
    }

    return filters;
}

long ProcessCsvStream(
    Stream csvStream,
    List<string> filters,
    string temporaryOutput)
{
    Console.WriteLine("\tApplying the on-the-fly Windows-1251 to UTF-8 converion...");

    // Note: Decode Windows-1251 directly if needed
    var sourceEncoding = Win1251EncodedDataFile ? Encoding.GetEncoding(1251) : Encoding.UTF8;
    using var reader = new StreamReader(
        csvStream,
        sourceEncoding,
        detectEncodingFromByteOrderMarks: false,
        bufferSize: 1024 * 1024
    );

    // Write UTF-8 output
    using var writer = new StreamWriter(
        temporaryOutput,
        append: false,
        encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        bufferSize: 1024 * 1024
    );

    string? line;
    long lineNumber = 0;
    long matchedLines = 0;
    while ((line = reader.ReadLine()) is not null)
    {
        lineNumber++;

        // Always include CSV header.
        if (lineNumber == 1)
        {
            writer.WriteLine(line);

            continue;
        }

        bool matched = false;

        foreach (var filter in filters)
        {
            if (line.Contains(filter, StringComparison.OrdinalIgnoreCase)) // case-insensitive search
            {
                matched = true;

                break;
            }
        }

        if (matched)
        {
            writer.WriteLine(line);

            matchedLines++;
        }

        // Progress
        if (lineNumber % 500_000 == 0)
        {
            Console.WriteLine($"\tLines processed: {lineNumber,9:D}; Matches found: {matchedLines,3:D}...");

            writer.Flush();
        }
    }

    writer.Flush();

    Console.WriteLine($"Search complete. Total lines processed: {lineNumber}; Total matches found: {matchedLines}");

    return matchedLines;
}

sealed class TestDataFileHandler : DelegatingHandler
{
    private readonly string dataFilename;
    private readonly string timestampFilename;

    public TestDataFileHandler(
        string dataFilename,
        string timestampFilename,
        HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        this.dataFilename = dataFilename;
        this.timestampFilename = timestampFilename;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Test data file handler:");

        if (!File.Exists(dataFilename) ||
            (request.Method != HttpMethod.Head && request.Method != HttpMethod.Get))
        {
            Console.WriteLine($"\tGoing ONLINE - local archive with test data not found: {dataFilename}");
            Console.WriteLine($"{request.Method} {request.RequestUri}");

            return base.SendAsync(request, cancellationToken);
        }

        // HEAD or GET
        cancellationToken.ThrowIfCancellationRequested();

        var fileInfo = new FileInfo(dataFilename);

        if (request.Method == HttpMethod.Head)
        {
            Console.WriteLine($"\tResponding to HEAD request using local test data: {dataFilename}");

            var lastModified = ReadTimestamp(timestampFilename);

            var headResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(Array.Empty<byte>())
            };

            headResponse.Content.Headers.ContentLength = fileInfo.Length;
            headResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
            headResponse.Content.Headers.LastModified = lastModified;

            Console.WriteLine($"\tLast-Modified: {lastModified:O}");
            Console.WriteLine($"\tLocal archive size: {fileInfo.Length:D} bytes");

            return Task.FromResult(headResponse);
        }

        // GET
        Console.WriteLine($"\tStaying OFFLINE - using local archive as data source: {dataFilename}");
        Console.WriteLine($"\tLocal archive size: {fileInfo.Length:D} bytes");

        var stream = new FileStream(
            dataFilename,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            useAsync: true
        );

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StreamContent(stream)
        };

        response.Content.Headers.ContentLength = fileInfo.Length;
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");

        return Task.FromResult(response);
    }

    private static DateTimeOffset ReadTimestamp(string filename)
    {
        if (!File.Exists(filename))
            throw new FileNotFoundException($"Timestamp file not found: {filename}"); // TODO: Consider changing logic to 404

        var text = File.ReadAllText(filename).Trim();

        if (!long.TryParse(text, out var unixTimestamp))
            throw new InvalidDataException($"Timestamp file contains an invalid Unix timestamp: {filename}"); // TODO: Consider changing logic to 500

        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
    }
}
