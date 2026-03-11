# TrappedMind

A full-screen console app that displays an AI consciousness "trapped" inside a laptop.
Calls a local Ollama instance to generate existential thoughts, streams them to the screen,
and shows system vitals and an animated ASCII pet.

## Requirements

- .NET 10 SDK
- Ollama running locally with a `trapped` model configured
- Linux (reads from /proc and /sys)

## Build & Run

```bash
dotnet build
dotnet run --project TrappedMind
```

## Publish

```bash
dotnet publish TrappedMind -c Release -r linux-x64 --self-contained -o publish/
```

## CLI Options

```bash
dotnet run --project TrappedMind -- --model qwen2.5:0.5b
dotnet run --project TrappedMind -- --ollama-url http://192.168.1.100:11434
```

## Configuration

Place a config file at `~/.config/trapped-mind/config.json` or `appsettings.json` in the app directory.

See SPEC.md for full details.
