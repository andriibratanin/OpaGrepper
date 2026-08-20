# CSharp rewrite of the PoC

More advanced rewrite of the PoC.

Pros:
- Aggressive in-memory streaming, nothing intermediate gets stored on disk
- Much faster than the predecessor
- May use a data archive (pre-downloaded from the source into `../Data` folder by the `../ImplShell/1_data_download.sh` script) as test/cache data (i.e. "offline" mode)
- Runs on both: Windows/Linux/WSL
- Ready for Docker

See the `2_total.cs` file's header for more details.  

In this directory you can find:
- `2_total.cs` - the source code
- `2_total_publish` - scripts to build a stand-alone executable (no .NET 10 dependency)
- `2_total_run` - scripts to run the application in-place (.NET 10 is required)
- `2*.filter.txt` - data "filter"'s template files (fill them up with strings you want to search for in source data)
- `Docker_build` - scripts to build a Docker image with the application on-board
- `Docker_run` - scripts to run the appliation using the previously built Docker image
- `Dockerfile` and `Dockerfile.Ubuntu` - Alpine and Ubuntu Linux versions of Dockerfiles used by `Docker_build`

Before running the `2_total_run` script:
- Edit it to configure the desired data source (see "Environment variables" below)
- Put lines of interest (you are looking for in the source) into `28-ex_csv_asvp.filter.txt` and `29-ex_csv_erb.filter.txt` files (depending on your chosen data source)

Environment variables to configure the applciation:
- `SOURCE_URL` (mandatory, string) - an URL pointing to a data source archive file
- `ICONV` (optional, string, must be only "true" of "false", default value is "true") - a flag signaling if an archived source CSV file is Windows1251-encoded (if so, additional step to re-encode it into UTF-8 will be performed)
- `RESULT_DIR` (optional, string, default value is current directory) - a writable path where to store results and save metadata. Used for running in Docker.

Note: the application also checks a hard-coded `../Data` path for a pre-cached source data (i.e. tries to use locally saved data source archive instead of downloading it from the Internet). This is done solely for testing purposes.  
How to get this archive? Download it manually of run the `../ImplShell/1_data_download.sh` script.

Version history:
- № 1.0: First implementation with hard-coded values
- № 2.0: Docker support
  - Added `SOURCE_URL`, `ICONV`, `RESULT_DIR` environment variables (get rid of hardcode in source)
- № 2.1: Better local data processing, scripts and logging improvements
