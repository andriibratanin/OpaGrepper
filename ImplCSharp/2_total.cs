#!/usr/bin/env dotnet
//
// Script to grep through government open data archives without downloading them
//
// Features:
// - Able to fetch a data archive (usually a ZIP compressed CSV file) and stream it right into a dynamic "decompress-reencode-search" pipeline (where nothing intermediate gets stored on disk)
// - Able to use pre-cached archive as test data (thus skipping the Internet calls)
// - Data source throttling mechanism included
//
// Requirements:
// - .NET 10 SDK (https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
//
// How to run? This is a so-called ".NET file based application", use the following terminal command to start it:
// - `dotnet run 2_total.cs`
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

const string Version = "2.0";

const string IconvEnvVar = "ICONV";
const string SourceUrlEnvVar = "SOURCE_URL";
const string ResultDirEnvVar = "RESULT_DIR";

const int TimestampFileMaxAgeMinutes = 23 * 60; // 23 hours

try
{
    var win1251EncodedDataFile = !string.Equals(
            Environment.GetEnvironmentVariable(IconvEnvVar),
            "false",
            StringComparison.OrdinalIgnoreCase
    );

    var versionString = win1251EncodedDataFile
        ? $"eGov № {Version} THZCIG (throttle-http-zip-csv-iconv-grep)"
        : $"eGov № {Version} THZCG (throttle-http-zip-csv-grep)";

    Console.WriteLine("===========================================================");
    Console.WriteLine($" Open Data Grepper");
    Console.WriteLine($" {versionString}");
    Console.WriteLine("===========================================================");

    var dataSourceUrl = Environment.GetEnvironmentVariable(SourceUrlEnvVar);
    if (string.IsNullOrWhiteSpace(dataSourceUrl))
    {
        Console.WriteLine($"ERROR: Environment variable {SourceUrlEnvVar} is required!");
        Environment.ExitCode = 2;
        return;
    }

    string dataSourceFilename = Path.GetFileNameWithoutExtension(new Uri(dataSourceUrl).LocalPath);
    Console.WriteLine($"Running for '{dataSourceFilename}' data source");

    // Required for Windows-1251 encoding operations
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    var dataDir = Path.Combine("..", "Data"); // "..\Data"

    var preCachedArchiveFile = Path.Combine(dataDir, $"{dataSourceFilename}.zip");
    var expectedFileInArchive = $"{dataSourceFilename}.csv";

    var resultDirEnv = Environment.GetEnvironmentVariable(ResultDirEnvVar);
    var resultDir = string.IsNullOrWhiteSpace(resultDirEnv)
        //? AppContext.BaseDirectory
        ? Directory.GetCurrentDirectory()
        : resultDirEnv;

    var timestampFile = Path.Combine(resultDir, $"{dataSourceFilename}.timestamp.txt");

    // The search logic is simple: data lines containing one of filter lines will be saved into result (i.e. it is a multiline grep)
    var filterFile = Path.Combine(resultDir, $"{dataSourceFilename}.filter.txt");
    var outputFile = Path.Combine(resultDir, $"{dataSourceFilename}.result.csv");

    var nowWithMills = DateTimeOffset.UtcNow;
    var now = nowWithMills.AddTicks(-(nowWithMills.Ticks % TimeSpan.TicksPerSecond));
    Console.WriteLine("");
    Console.WriteLine($"Current UTC timestamp: {now.UtcDateTime:O}");

    Console.WriteLine("");
    Console.WriteLine("Checking throttling conditions...");
    bool needsProcessing = NeedsProcessing(preCachedArchiveFile, timestampFile, now);
    if (needsProcessing)
    {
        Console.WriteLine("");
        Console.WriteLine($"Loading filters: {filterFile}...");

        var filters = LoadFilters(filterFile);
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
            preCachedArchiveFile,
            timestampFile,
            dataSourceUrl,
            expectedFileInArchive,
            win1251EncodedDataFile,
            filters,
            outputFile
        );

        Console.WriteLine("Download and processing complete");
        Console.WriteLine("");

        File.WriteAllText(timestampFile, now.ToUnixTimeSeconds().ToString());

        Console.WriteLine($"Operation timestamp saved to: {timestampFile}");
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


bool NeedsProcessing(string preCachedArchiveFile, string timestampFile, DateTimeOffset now)
{
    if (File.Exists(preCachedArchiveFile))
    {
        Console.WriteLine($"Local archive found: {preCachedArchiveFile} - will use it as test data file");

        return true;
    }

    if (!File.Exists(timestampFile))
    {
        Console.WriteLine("Timestamp file not found - assuming this is the first run");

        return true;
    }

    var text = File.ReadAllText(timestampFile).Trim();

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
    string preCachedArchiveFile,
    string timestampFile,
    string url,
    string csvFilename,
    bool win1251EncodedDataFile,
    List<string> filters,
    string outputFilename)
{
    string temporaryOutput = outputFilename + ".tmp";

    File.Delete(temporaryOutput);

    try
    {
        using var testDataFileHttpHandler = new TestDataFileHandler(
                    preCachedArchiveFile,
                    timestampFile,
                    new HttpClientHandler()
                );
        using var http = new HttpClient(testDataFileHttpHandler)
        {
            // Timeout = TimeSpan.FromHours(2)
        };

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        Console.WriteLine("");
        Console.WriteLine($"HTTP status  : {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"Last-Modified: {response.Content.Headers.LastModified?.ToString("O") ?? "unknown"}");

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
            var result = ProcessCsvStream(win1251EncodedDataFile, zip, filters, temporaryOutput);

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
    // Comment lines starting with "#" are ignored.

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

        // Note: search lines never get space-trimmed!
        Console.WriteLine($"\t- `{line}`");

        filters.Add(line);
    }

    return filters;
}

long ProcessCsvStream(
    bool win1251EncodedDataFile,
    Stream csvStream,
    List<string> filters,
    string temporaryOutput)
{
    Console.WriteLine("\tApplying the on-the-fly Windows-1251 to UTF-8 converion...");

    // Note: Decode Windows-1251 directly if needed
    var sourceEncoding = win1251EncodedDataFile ? Encoding.GetEncoding(1251) : Encoding.UTF8;
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
            if (line.Contains(filter, StringComparison.OrdinalIgnoreCase)) // Case-insensitive search
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

    // Totals
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
        Console.WriteLine($"\t{request.Method} {request.RequestUri}");

        if (!File.Exists(dataFilename) ||
            (request.Method != HttpMethod.Head && request.Method != HttpMethod.Get))
        {
            // Online
            Console.WriteLine($"\tGoing ONLINE - local archive with test data not found: {dataFilename}");

            return base.SendAsync(request, cancellationToken);
        }

        // Offline
        Console.WriteLine($"\tStaying OFFLINE - local archive with test data found: {dataFilename}");

        // HEAD or GET
        cancellationToken.ThrowIfCancellationRequested();

        var fileInfo = new FileInfo(dataFilename);

        var lastModified = ReadTimestamp(timestampFilename);
        if (lastModified is null)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                RequestMessage = request,
                Content = new StringContent("Failed to read timestamp.")
            });
        }
        //Console.WriteLine($"\tLast-Modified: {lastModified:O}");
        //Console.WriteLine($"\tLocal archive size: {fileInfo.Length:D} bytes");

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request
        };

        HttpContent content;
        if (request.Method == HttpMethod.Head)
        {
            content = new ByteArrayContent(Array.Empty<byte>());
        }
        else
        {
            // GET
            var stream = new FileStream(
                dataFilename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                useAsync: true
            );

            content = new StreamContent(stream);
        }

        content.Headers.ContentLength = fileInfo.Length;
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Headers.LastModified = lastModified;

        response.Content = content;

        return Task.FromResult(response);
    }

    private static DateTimeOffset? ReadTimestamp(string filename)
    {
        if (!File.Exists(filename))
        {
            Console.WriteLine($"ERROR: Timestamp file not found: {filename}");
            return null;
        }

        var text = File.ReadAllText(filename).Trim();

        if (!long.TryParse(text, out var unixTimestamp))
        {
            Console.WriteLine($"ERROR: Timestamp file contains an invalid Unix timestamp: {filename}");
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
    }
}
