# DNS Updater

A simple program to update local [Pihole](https://pi-hole.net/) DNS servers.

A password and host name are required as command line parameters, eg:

```bash
DnsUpdater ui-password=admin-password host=example.internal
```

## Single File Build

A single file build can be created with publish:

```bash
dotnet publish --configuration Release -r linux-x64 -p:PublishSingleFile=true --self-contained true
```
