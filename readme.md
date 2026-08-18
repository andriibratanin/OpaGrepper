# Open Data Grepper

Grep Open Data with/without caching them locally.

Both: Shell-scripted PoC (the `ImplShell` folder) and its more advanced C# successor (the `ImplCSharp` folder) are included.

Possible work flows:
- Stand-alone Shell-scripted PoC - go to the `ImplShell` folder.
- Development of the C# code:
  - go to the `ImplShell` folder, configure the data source in the `0_common.sh` script, run the `1_data_download.sh` script (to cache a data archive into the `Data` folder);
  - then go to the `ImplCSharp` folder, make sure you use the same data source in `2_total_run` scripts, make changes you need to the source `2_total.cs` file, then run `2_total_run` (PowerShell or Shell version depending on your platform) to test your changes OFFLINE;
  - don't forget to re-run the `1_data_download.sh` script in the `ImplShell` folder periodically if you are interested in data updates ;)
- Stand-alone C# code ("production", i.e. ONLINE): go to the `ImplCSharp` folder, configure the data source in `2_total_run` scripts, then run them to get the results straight from the Internet .

HINT: if you ever get confused - numbers in filenames are your guides

Tested on gov datasets:
- Debtors:
  - [Єдиний реєстр боржників](https://data.gov.ua/dataset/506734bf-2480-448c-a2b4-90b6d06df11e)
- Executive proceedings:
  - [Інформація з автоматизованої системи виконавчого провадження](https://data.gov.ua/dataset/6c0eb6c0-d19a-4bb0-869b-3280df46800a)
