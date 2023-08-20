remove-item -recurse bin
remove-item -recurse obj
dotnet build -c Release
dotnet nuget push  .\bin\Release\CliUi.*.nupkg -k oy2hiais73dbgrlqz4pqiszepc4ip5ouhaqd3mpd5h4uca --source https://api.nuget.org/v3/index.json --skip-duplicate
