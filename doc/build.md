# Build

## Development Build

```powershell
dotnet build oo-win-config.sln
```

Development output uses the normal .NET project output directory:

```text
src/OoWinConfig/bin/Debug/net8.0-windows/
```

## Portable Publish

```powershell
dotnet publish src/OoWinConfig/OoWinConfig.csproj -c Release -o dist/oo-win-config
```

Portable publish output:

```text
dist/oo-win-config/
  OoWinConfig.exe
  data/
    profiles/
```

## Visual Smoke Capture

Build normally, then run the app with automation capture flags:

```powershell
src/OoWinConfig/bin/Debug/net8.0-windows/OoWinConfig.exe --capture-ui --exit-after-capture
```

Capture a specific view:

```powershell
src/OoWinConfig/bin/Debug/net8.0-windows/OoWinConfig.exe --capture-ui --view snapshot-map --exit-after-capture
```

The app renders the main window, captures the WPF client area, writes a PNG under:

```text
data/debug/screenshots/
```

Then it exits. This is intended for local visual smoke checks after UI changes.
