# EasyIni

A lightweight INI file parser for .NET, targeting **.NET Framework 4.0+** and **.NET Standard 2.0+**, with full cross-platform support including Windows XP.

[![NuGet](https://img.shields.io/nuget/v/EasyIni.svg)](https://www.nuget.org/packages/EasyIni)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Features

- **Zero dependencies** — pure C#, no external packages required
- **Multi-target** — `net40` (XP support) and `netstandard2.0` (cross-platform)
- **Comment preservation** — leading comments, inline comments, and blank lines are preserved through parse → modify → save round-trips
- **Quoted values** — values containing special characters (`;`, `#`, `=`) are automatically quoted on save
- **Generic deserialization** — map INI sections to POCO classes with `[IniProperty]` attributes
- **Flexible API** — both dictionary-style indexer access and strongly-typed mapping
- **Configurable behavior** — case-sensitivity, duplicate key handling, whitespace trimming

## Installation

```bash
dotnet add package EasyIni
```

Or via NuGet Package Manager:

```
Install-Package EasyIni
```

## Quick Start

### Basic Parsing

```csharp
using EasyIni;

string iniText = @"
; Database configuration
[server]
host=localhost
port=3306 ; default port
";

var ini = IniParser.Parse(iniText);

// Indexer access
string host = ini["server"]["host"];           // "localhost"
int port = ini["server"]["port"].Get<int>();   // 3306

// Generic access with defaults
int timeout = ini.Get<int>("server", "timeout", 30);  // 30 (not found)
```

### Modifying and Saving

```csharp
// Partial modification — comments are preserved
ini["server"]["port"] = new IniValue("5432");
ini.Set("server", "host", "prod-server");

// Full serialization
string output = ini.ToString();
ini.Save("config.ini");
```

### Strongly-Typed Mapping

```csharp
[IniSection("db")]
public class DatabaseConfig
{
    [IniProperty("host")]
    public string Host { get; set; }

    [IniProperty("port")]
    public int Port { get; set; }

    [IniProperty("pooling")]
    public bool Pooling { get; set; }
}

// Deserialize
var config = IniSerializer.Deserialize<DatabaseConfig>(iniText);
Console.WriteLine(config.Host);  // "localhost"

// Serialize back
var ini = IniSerializer.Serialize(config);
string text = IniSerializer.SerializeToString(config);
```

### Multi-Section Mapping

```csharp
[IniSection("app")]
public class AppConfig
{
    [IniProperty("name", Section = "app")]
    public string AppName { get; set; }

    [IniProperty("host", Section = "server")]
    public string ServerHost { get; set; }
}
```

### Options

```csharp
var options = new IniParseOptions
{
    CaseSensitive = true,
    DuplicateKeyBehavior = DuplicateKeyBehavior.Throw,
    AllowQuotedValues = true,
    TrimWhitespace = true
};

var ini = IniParser.Parse(iniText, options);
```

### Safe Parsing

```csharp
IniData result;
string error;
if (IniParser.TryParse(iniText, out result, out error))
{
    // success
}
else
{
    Console.WriteLine("Parse error: " + error);
}
```

## API Overview

| Class | Description |
|---|---|
| `IniParser` | Static entry point: `Parse()`, `ParseFile()`, `TryParse()` |
| `IniData` | Top-level container with indexer `["section"]` |
| `IniSection` | Ordered collection of `IniEntry` items |
| `IniEntry` | A single `key=value` pair with comments |
| `IniValue` | Value wrapper with type conversion via `Get<T>()` |
| `IniSerializer` | Generic `Deserialize<T>()` / `Serialize<T>()` |
| `IniParseOptions` | Case sensitivity, duplicate handling, quoting |
| `IniParseException` | Exception thrown on parse errors |

## Compatibility

| Target | .NET Version | Platforms |
|---|---|---|
| `net40` | .NET Framework 4.0+ | Windows XP SP3+ |
| `netstandard2.0` | .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5–9 | Windows, Linux, macOS |

## License

MIT — see [LICENSE](LICENSE) for details.
