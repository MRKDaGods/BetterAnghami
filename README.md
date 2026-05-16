# BetterAnghami

BetterAnghami is an enhanced version of the [Anghami](https://www.anghami.com/) music streaming service, offering additional features and an improved user experience.

## Disclaimer

BetterAnghami is an independent project and is not affiliated with or endorsed by Anghami.

## Features

- **Fast Start-Up Times:** Quickly access your music without delays.
- **Smooth Experience:** Seamless browsing experience powered by WebView2.
- **Custom Theming:** Personalize the app's appearance with a built-in theme editor.
- **Discord Rich Presence:** Show your current listening activity on Discord.

## Interface

### Album View with a Custom Theme Applied

Seamless visual experience that adapts beautifully to your chosen theme.

<img src="https://github.com/user-attachments/assets/bdc942fe-33a1-4654-962e-175fdff6d659" width="900" />

### Theme Editor

Easily customize themes to match your style using the built-in theme editor.

<img src="https://github.com/user-attachments/assets/ea09fb12-e609-472f-8247-3a3abb4a86d7" width="900" />

### Accessing Themes

Themes can be accessed using the dropdown menu on the top right.

<img src="https://github.com/user-attachments/assets/1fcd3466-9f45-4094-8788-fb84eac0b6c1" height="350" />

### Discord Rich Presence

Showcase your music activity to friends on Discord with rich presence integration (time remaining, paused state, loading, etc).

<img src="https://github.com/user-attachments/assets/a6299596-140f-4648-8d50-1a60952091e4" width="500" />

## Building BetterAnghami

BetterAnghami is a WPF application and can only be built and run on Windows.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### Clone the Repository

```bash
git clone https://github.com/MRKDaGods/BetterAnghami.git
cd BetterAnghami
```

### Build and Run

**Using the .NET CLI:**

```bash
dotnet build BetterAnghami/BetterAnghami.csproj -c Release
dotnet run --project BetterAnghami/BetterAnghami.csproj
```

**Using Visual Studio:**

1. Install [Visual Studio](https://visualstudio.microsoft.com/) with the **.NET desktop development** workload.
2. Open `BetterAnghami.sln`.
3. Set `BetterAnghami` as the startup project.
4. Press `F5` to build and run.

## Precompiled Binaries

Download the latest precompiled release for Windows:

- [Download BetterAnghami for Windows](https://github.com/MRKDaGods/BetterAnghami/releases/latest)
