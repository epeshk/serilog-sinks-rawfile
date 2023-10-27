# Serilog.Sinks.RawFile

Writes [Serilog](https://serilog.net) events to one or more text or binary files.

Writes directly in file, by default in UTF-8 encoding, bypassing conversion from UTF-16 and avoiding heap allocations where possible.

This library is a community fork of `Serilog.Sinks.File`

### Differences to Serilog.Sinks.File

- Uses `Serilog.Formatting.IBinaryWriterFormatter` from `Serilog.Formatting.BinaryWriter` instead of `Serilog.Formatting.ITextFormatter`
- Encoding is dependent on `IBinaryWriterFormatter` implementation (UTF-8 by default). `encoding` configuration parameter is not supported
- Writing to disk are `buffered` by default
- Shared file access is not supported
- `keepFileOpen` option added (`true` by default, matches with Serilog.Sinks.File behavior). If `false`, file handle will be opened and closed for each write to disk.
- Logging is suspended on IO errors, such as insufficient disk space. Some amount of logs stored in memory, in hope of being written to disk later
- In case of contention, log events are prerendered before acquiring a file write lock

### Getting started

Install the [Serilog.Sinks.RawFile](https://www.nuget.org/packages/Serilog.Sinks.RawFile/) package from NuGet:

```powershell
Install-Package Serilog.Sinks.RawFile
```

To configure the sink in C# code, call `WriteTo.RawFile()` during logger configuration:

```csharp
var log = new LoggerConfiguration()
    .WriteTo.RawFile("log.txt", rollingInterval: RawFileRollingInterval.Day)
    .CreateLogger();
```

This will append the time period to the filename, creating a file set like:

```
log20180631.txt
log20180701.txt
log20180702.txt
```

### Limits

To avoid bringing down apps with runaway disk usage the file sink **limits file size to 1GB by default**. Once the limit is reached, no further events will be written until the next roll point (see also: [Rolling policies](#rolling-policies) below).

The limit can be changed or removed using the `fileSizeLimitBytes` parameter.

```csharp
    .WriteTo.RawFile("log.txt", fileSizeLimitBytes: null)
```

For the same reason, only **the most recent 31 files** are retained by default (i.e. one long month). To change or remove this limit, pass the `retainedFileCountLimit` parameter.

```csharp
    .WriteTo.RawFile("log.txt", rollingInterval: RawFileRollingInterval.Day, retainedFileCountLimit: null)
```

### Rolling policies

To create a log file per day or other time period, specify a `rollingInterval` as shown in the examples above.

To roll when the file reaches `fileSizeLimitBytes`, specify `rollOnFileSizeLimit`:

```csharp
    .WriteTo.RawFile("log.txt", rollOnFileSizeLimit: true)
```

This will create a file set like:

```
log.txt
log_001.txt
log_002.txt
```

Specifying both `rollingInterval` and `rollOnFileSizeLimit` will cause both policies to be applied, while specifying neither will result in all events being written to a single file.

Old files will be cleaned up as per `retainedFileCountLimit` - the default is 31.

### JSON `appsettings.json` configuration

To use the file sink with _Microsoft.Extensions.Configuration_, for example with ASP.NET Core or .NET Core, use the [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) package. First install that package if you have not already done so:

```powershell
Install-Package Serilog.Settings.Configuration
```

Instead of configuring the file directly in code, call `ReadFrom.Configuration()`:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
```

In your `appsettings.json` file, under the `Serilog` node, :

```json
{
  "Serilog": {
    "WriteTo": [
      { "Name": "RawFile", "Args": { "path": "log.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

See the XML `<appSettings>` example above for a discussion of available `Args` options.

### Controlling event formatting

The file sink creates events in a fixed text format by default:

```
2018-07-06 09:02:17.148 +10:00 [INF] HTTP GET / responded 200 in 1994 ms
```

The format is controlled using an _output template_, which the file configuration method accepts as an `outputTemplate` parameter.

The default format above corresponds to an output template like:

```csharp
  .WriteTo.RawFile("log.txt",
    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
```

### Auditing

The file sink can operate as an audit file through `AuditTo`:

```csharp
    .AuditTo.RawFile("audit.txt")
```

Only a limited subset of configuration options are currently available in this mode.

### Performance

It is recommended to use [Serilog.Sinks.Background](https://github.com/epeshk/serilog-sinks-background) package to wrap the file sink and perform all disk access on a background worker thread.

### Extensibility

`Serilog.Sinks.RawFile` supports hooks for original `Serilog.Sinks.File` package via `Serilog.Sinks.RawFile.Hooks` adapter.

### Building from sources

`Serilog.Sinks.RawFile` uses [source dependency](https://github.com/epeshk/serilog-utf8-commons) for format strings support without providing an external IBufferWriterFormatter implementation. To build this library either disable `UTF8_FORMATTER` constant, or place [this](https://github.com/epeshk/serilog-utf8-commons) repository near.

_Copyright &copy; 2023 Serilog Contributors - Provided under the [Apache License, Version 2.0](http://apache.org/licenses/LICENSE-2.0.html)._
