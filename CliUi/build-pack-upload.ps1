remove-item -recurse bin
remove-item -recurse obj
dotnet build -c Release
dotnet nuget push  .\bin\Release\CliUi.*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate
