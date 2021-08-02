@ECHO OFF

dotnet tool restore
dotnet ef database update --project Bot600\Bot600.csproj
dotnet publish Bot600\Bot600.csproj -c Release -clp:ErrorsOnly;Summary --self-contained -r linux-arm

PAUSE
