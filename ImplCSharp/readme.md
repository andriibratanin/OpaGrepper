# CSharp rewrite of the PoC

More advanced rewrite of the PoC.

Pros:
- Aggressive in-memory streaming, nothing intermediate gets stored on disk
- Much faster than the Shell analog
- May use an archive (pre-downloaded from the source into `../Data` folder by the `../ImplShell/1_data_download.sh` script) as test/cache data (i.e. "offline" mode)
- Runs on both: Windows/Linux/WSL

See the `2_total.cs` file's header for more details.
