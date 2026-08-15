# Shell-scripted PoC

A straightforward implementation of an idea to parse/grep an open data archive.

Pros:
- Simple
- No additional dependencies/requirements

Cons:
- Stores downloaded and intermediate processed data on disk (takes lots of space)
- Slow
- Linux/WSL-only

How to use:
- configure source url and filename in `0_common.sh`
- run scripts one-by-one in order (staring from 1)
- before running `4_data_filter.sh` put lines of interest (you are looking for in the source) into `4_data_filter.txt` file
- data source lines (containing your lines of interest from `4_data_filter.txt`) will be dumped into `5_data_result.csv` file

**Troubleshooting**

~~Search may not work as expected in case if the filter file contains Windows-style line endings.~~ (fixed)

To check if filter file contains redundant symbols (like `^M`) run:
```bash
cat -A 4_data_filter.txt
```
If you see `^M` at the end of each line, run:
```bash
sed -i 's/\r$//' 4_data_filter.txt
```
___
