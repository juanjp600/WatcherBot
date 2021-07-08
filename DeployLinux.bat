@ECHO OFF

dotnet publish Bot600\Bot600.csproj -c Release -clp:ErrorsOnly;Summary --self-contained -r linux-arm

PAUSE
