@ECHO OFF

dotnet tool restore
dotnet ef database update --project WatcherBot\WatcherBot.csproj
dotnet publish WatcherBot\WatcherBot.csproj -c Release -clp:ErrorsOnly;Summary --self-contained -r win-x64

PAUSE
