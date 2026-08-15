# Open Data Grepper

Grep Open Data with/without caching them locally.

Both: Shell-scripted PoC (`ImplShell` folder) and its more advanced C# successor (`ImplCSharp` folder) are included.

Possible work flows:
- Stand-alone Shell-scripted PoC - go to `ImplShell` folder.
- Development of C# code:
  - go to `ImplShell` folder, configure the data source in `0_common.sh`, run `1_data_download.sh` (to cache data archive into `Data` folder);
  - then go to `ImplCSharp` folder, make sure you use the same data source in `2_total.cs`, make changes you need, run `2_total_run.ps1` to test them OFFLINE;
  - don't forget to re-run the `1_data_download.sh` script periodically in case if you are interested in data updates ;)
- Stand-alone C# code ("production"): go to `ImplCSharp` folder, configure the data source in `2_total.cs`, run `2_total_run.ps1` to get results from the Internet (ONLINE).

HINT: if you ever get confused - numbers in filenames are your guides

Tested on gov datasets:
- Debtors:
  - [Єдиний реєстр боржників](https://data.gov.ua/dataset/506734bf-2480-448c-a2b4-90b6d06df11e)
- Executive proceedings:
  - [Інформація з автоматизованої системи виконавчого провадження](https://data.gov.ua/dataset/6c0eb6c0-d19a-4bb0-869b-3280df46800a)
