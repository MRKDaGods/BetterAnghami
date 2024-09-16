# BetterAnghami

BetterAnghami is an enhanced version of the <a href="https://www.anghami.com/">Anghami</a> music streaming service, offering additional features and improved user experience.

## Disclaimer

BetterAnghami is an independent project developed by me. It is not affiliated with or endorsed by Anghami.

## Features

- **Fast Start-Up Times:** Quickly access your music without delays.
- **Smooth Experience:** Enjoy a seamless browsing experience powered by WebView2.
- **Custom Theming:** Personalize the app's appearance to your liking.
- **Discord Rich Presence:** Show your current listening activity on Discord.
  
## Interface

### Album View with a Custom Theme Applied
Seemless visual experience that adapt beautifully to your chosen theme <br />
<img src="https://github.com/user-attachments/assets/d7884b3c-fa4c-4c0d-a9a7-b04bdc374b70" width="900" />

### Theme Editor
Easily customize themes to match your style using the built-in theme editor <br/>
<img src="https://github.com/user-attachments/assets/e2bfe0cf-6e18-4b80-8489-379a9d410332" width="900" />


### Accessing Themes
Themes can be accessed using the dropdown menu on the top right <br/>
<img src="https://github.com/MRKDaGods/BetterAnghami/assets/25166537/ce444ab3-03fd-467f-a921-ad6503ab2106" height="350" />

### Discord Rich Presence
Showcase your music activity to friends on Discord with rich presence integration (with time left, paused, loading, etc)<br /> <br />
#### Playing
![image](https://github.com/user-attachments/assets/02732466-5f39-49bf-bc5d-b828e05574c0)
#### Paused
![image](https://github.com/user-attachments/assets/48f4a8e6-2ee0-4de6-8b8b-7b9d28bc7bda)
#### Buffering
![image](https://github.com/user-attachments/assets/2a6fe984-b1cd-49e9-bc9e-23dbe7bcc7f5)


## Building BetterAnghami

Since BetterAnghami is based on Windows Presentation Framework (WPF), it may only be compiled and ran on Windows. <br />

To build **BetterAnghami on Windows**, follow these steps:

### Prerequisites

- Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- Install [Visual Studio](https://visualstudio.microsoft.com/) with the .NET desktop development workload.

### Clone the Repository

Clone the BetterAnghami repository to your local machine:

```bash
git clone https://github.com/MRKDaGods/BetterAnghami.git
cd BetterAnghami
```

### Open in Visual Studio
Open the project in Visual Studio:
- Navigate to the cloned `BetterAnghami` directory
- Open `BetterAnghami.sln` in Visual Studio.


*Visual Studio will automatically handle restoring dependencies and compiling the project when you run it.*

### Run the Project
Once the project is opened in Visual Studio:
- Ensure `BetterAnghami` is set as the startup project.
- Press `F5` or go to `Debug > Start Debugging` to build and run the project.

## Precompiled Binaries

If you prefer not to build BetterAnghami from the source code, you can download precompiled binaries for your platform:

- **Windows:** [Download BetterAnghami for Windows](https://github.com/MRKDaGods/BetterAnghami/releases/latest)
