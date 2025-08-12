# OtaPackager

Small helper to prepare OTA artifacts:

- copies .pe files from an input folder to an output folder
- computes SHA256 for each .pe
- generates manifest.json (main .pe is last)

Usage

```pwsh
# Build
dotnet build ESP32-NF-MQTT-DHT/Tools/OtaPackager/OtaPackager.csproj -c Release

# Run
# OtaPackager <inputDir> <version> <outputDir> [--main=App.pe] [--base-url=https://host/path/] [--include=App.pe;Lib.pe]

# Examples
# 1) Package all non-framework PEs from input dir, main is App.pe
dotnet ESP32-NF-MQTT-DHT/Tools/OtaPackager/bin/Release/net8.0/OtaPackager.dll `
	e:/path/to/ota_payload 1.0.3 e:/path/to/dist

# 2) Force base URL and explicit include list
dotnet ESP32-NF-MQTT-DHT/Tools/OtaPackager/bin/Release/net8.0/OtaPackager.dll `
	e:/path/to/ota_payload 1.0.3 e:/path/to/dist --base-url=https://cdn.example.com/env/ --include=App.pe;Iot.Device.Something.pe
```

Notes

- By default, framework PEs are skipped: mscorlib.pe, System._.pe, nanoFramework._.pe.
- Use `--include=` to specify exact PEs if needed.
- `--base-url` prefixes URLs in manifest.json.
- Ensure `--main` matches device `Config.MainAppName` (default: `App.pe`).
