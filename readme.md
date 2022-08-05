# DNS Updater

A simple program to update local [Pihole](https://pi-hole.net/) DNS servers.

## Single File Build

A single file build can be created with publish:

```bash
dotnet publish --configuration Release -r linux-x64 -p:PublishSingleFile=true --self-contained true
```
