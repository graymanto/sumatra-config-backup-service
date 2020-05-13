# Sumatra PDF Configuration Backup Service

This is a straightforward dotnet core windows service that works around a bug (questionable design?) in Sumatra pdf. If Sumatra crashes or is shut down unexpectedly, by a windows update for example, information about the previous session is not written to the configuration file. Therefore the state of the previous session is lost. This service attempts to at least partially work around this by making a copy of the configuration file whenever it has a non empty session state. It will restore this configuration when the service starts (if Sumatra is not running).

To use the service, it should be possible to do something like the following

```
# Build and publish app to required location

dotnet publish -r win-x64 -c Release -o "%MYLOCATION%"

sc create SumatraConfigService BinPath=%MYLOCATION%\SumatraBackupService.exe
sc start SumatraConfigService
```

Note that the service needs to run as the logged in user in order to have access to the users %LOCALAPPDATA% directory.
