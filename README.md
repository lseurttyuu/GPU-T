<h1 align="center">
  <img src="docs/readme/title_gpu_t_black.png#gh-light-mode-only" alt="GPU-T">
  <img src="docs/readme/title_gpu_t_white.png#gh-dark-mode-only" alt="GPU-T">
</h1>

<p align="center">
  <strong>A comprehensive graphics card information utility for Linux.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Linux-blue?logo=linux" alt="Platform Linux">
  <img src="https://img.shields.io/badge/Framework-Avalonia%20UI-purple" alt="Avalonia">
  <img src="https://img.shields.io/badge/.NET-%209.0-512bd4" alt=".NET">
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License">
</p>
<p align="center">
  <a href="https://ko-fi.com/P5P71Y95K8"><img src="https://ko-fi.com/img/githubbutton_sm.svg" alt="ko-fi"></a>
</p>

GPU-T is a modern desktop utility built with **.NET** and **Avalonia UI** designed to provide detailed information about your video card and GPU. It reads directly from the Linux kernel (`sysfs`), graphics APIs and the custom hardware databases to display low-level hardware specifications, real-time sensors, and advanced feature support.

<p align="center">
  <img src="docs/readme/gpu_t_main_screenshot_light.png#gh-light-mode-only" alt="Screenshots of GPU-T application (light mode)">
  <img src="docs/readme/gpu_t_main_screenshot_dark.png#gh-dark-mode-only" alt="Screenshots of GPU-T application (dark mode)">
</p>

## Why create this?

For years, users on Windows have relied on *GPU-Z* as the gold standard for verifying graphics hardware. It is clean, precise, and tells you exactly what is under the hood.

After switching to **Linux** in 2025, I couldn't find a direct alternative that offered the same specific "density" of information in a clean, native GUI. While other great tools exist, I wanted a dedicated application that mimics the familiarity and utility of GPU-Z: a simple interface, instant hardware lookup, and real-time monitoring. 

GPU-T is my attempt (assisted by AI) to fill that void — providing a diagnostic tool for the Linux open-source ecosystem.

## Features

GPU-T is designed to be a "Single Source of Truth" for your GPU on Linux:

* **Hardware Reconnaissance:** Identifies GPU make, model, revision, die size, transistor count, and release date using custom, updateable JSON databases.
* **Smart Detection (Experimental):** Implements a "Best Match" algorithm that detects specific silicon revisions (e.g., distinguishing between variants of the same chip ID) and warns if an exact match isn't found.
* **Real-time Sensors:** Monitors clock speeds (GPU/VRAM), temperatures (Hotspot/Edge), fan speeds, board power draw (PPT), and other vital metrics in real-time. Includes support for logging sensor data to a file.
* **Advanced Capabilities:** Checks for support of Vulkan, OpenCL, CUDA, Ray Tracing, etc.
* **Deep Dive:** 
    * **PCIe Resizable BAR** status detection (via direct PCI resource analysis).
    * **BIOS** and Driver version readout.
    * **Memory** type, vendor, and bus width verification.
    * **Vulkan** version, extensions, and features lookup.
    * **OpenCL** version, vendor, and other capabilities lookup.
    * **VA-API** status, including encode & decode capabilities lookup (AMD GPUs).
    * **CUDA** tech information - metrics and capabilities (NVIDIA GPUs).
    * **NVENC/NVDEC** encode & decode capabilities lookup (NVIDIA GPUs).
* **Vendor-Agnostic Architecture:** Built with a modular architecture. Currently supports **AMD Radeon** and **NVIDIA GeForce** GPUs (using `amdgpu` and/or proprietary `nvidia` drivers), but is designed to support Intel in the future.
* **TechPowerUp Lookup:** Directly open the TechPowerUp website to verify data about your specific GPU model.

## Supported Hardware

Currently, the application is fully implemented for **AMD Radeon** and **NVIDIA GeForce** GPUs on Linux.

- [x] **AMD Radeon:** GPUs released in 2014 or later, compatible with the open-source `amdgpu` driver. This includes, but is not limited to: RDNA architectures (RX 6000, RX 7000, RX 9000), Vega, Polaris.
- [x] **NVIDIA GeForce:** GPUs released in 2010 or later, running on proprietary `nvidia` drivers (R535 and later are preferred). Support includes, but is not limited to: GTX 9xx, GTX 10-series, and modern RTX GPUs.
- [ ] **Intel Arc:** Architecture ready, implementation planned.

## Roadmap

Please check out the Discussions tab.

## Installation

### 1. Arch Linux (AUR)
Thanks to community maintainers, Arch users can install via the AUR:
```bash
yay -S gpu-t
```
Alternatively, for the unstable development version, use:
```bash
yay -S gpu-t-git
```

### 2. AppMan (AM)
You can install via the `am` package manager:
```bash
am -i gpu-t
```

### 3. Universal AppImage

1.  Download the latest AppImage from the **Releases** tab.
2.  Mark the file as executable: `chmod +x GPU-T.AppImage`.
3.  Run the application.

*Note: No root privileges are required, as the app reads user-accessible paths in `/sys/class/drm`.*

*Note 2: The application has been verified on Debian 13, Mint 22.3, Ubuntu 22.04, Ubuntu 24.04 and Alpine Linux v3.23. It is expected to work on most modern Linux distributions.*

## Prerequisites
GPU-T relies on standard Linux utilities to fetch API-specific information. Ensure the following are installed on your system (the app works without these, but will show more info / more accurate info with these):

* `vulkan-tools` (provides `vulkaninfo`)
* `clinfo` (for OpenCL detection)
* `mesa-utils` (provides `glxinfo` for OpenGL)
* `pciutils` (provides `lspci` for ReBAR detection)
* `vainfo` (for Multimedia capabilities readout - AMD GPUs)
* `nvidia-smi` (it is included in proprietary NVIDIA drivers; necessary for proper Sensors readout - NVIDIA GPUs)


## Building from Source

Requirements: **.NET SDK 9.0 or newer**.

1.  Clone the repository:
    ```bash
    git clone https://github.com/lseurttyuu/GPU-T
    cd GPU-T
    ```
2.  Restore dependencies and build:
    ```bash
    dotnet build
    ```
3.  Run the application:
    ```bash
    dotnet run
    ```

If you wish to build the app using the Release configuration, keep in mind that the automatic NVAPI/NVML sidecar compilation (`GPU-T.Nvapi.csproj`) is excluded from the standard Release step by default. If you want to learn more about how the custom Release compilation process works for packaging, please head over to [Issue #74](https://github.com/lseurttyuu/GPU-T/issues/74).

## Architecture

For developers interested in the code, GPU-T uses a clean **MVVM** architecture with a focus on modularity:

* **Services Layer:** Separated into `Probes` (hardware polling), `Advanced` (API providers), and `Utilities`.
* **Factory Pattern:** A `GpuProbeFactory` determines the GPU vendor at runtime and injects the correct logic (e.g., `LinuxAmdGpuProbe`, `LinuxNvidiaGpuProbe`), making it easy to add Intel support practically without touching the UI code.
* **Database:** Local JSON databases handle the static hardware specs. They support user overrides and easy updates for new hardware definitions. Want to modify the database GPU-T uses? Just head over to `~/.local/share/GPU-T/` and edit the appropriate JSON file.
* **Settings:** The app stores various user preferences (like theme selection, window height, etc.) locally. The JSON configuration file is stored safely in `~/.config/GPU-T/`.

Additionally, the project contains a dedicated NVAPI/NVML sidecar app (`GPU-T.Nvapi.csproj` located in the `Nvapi` directory). This sidecar is responsible for advanced sensor readouts and populating the deep-dive information in the Advanced tab for NVIDIA GPUs. It has been completely decoupled from the main application to ensure absolute stability and memory safety.

## Built With

* **[.NET](https://dotnet.microsoft.com/)** - The underlying cross-platform framework.
* **[Avalonia UI](https://avaloniaui.net/)** - For the pixel-perfect, cross-platform user interface.
* **[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)** - Provides the MVVM architecture helpers (Observables, Commands).

## Contributing

Contributions are welcome! If you have an Intel GPU and want to help implement Intel GPU support, feel free to open a Pull Request. Want to discuss new features? Please head over to the Discussions tab.

## Support the Project

GPU-T is developed entirely in my free time. If this tool has saved you time, helped you diagnose a hardware issue, or you just want to support continued development, please consider leaving a tip. 

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/P5P71Y95K8)

Your support is incredibly appreciated and helps keep the project going!

## Acknowledgements, Credits & Disclaimer

This project is heavily inspired by **GPU-Z** by **TechPowerUp**. It is not an official port, nor is it affiliated with TechPowerUp in any way. GPU-T is a tool designed for Linux users who miss the clarity and utility of the original Windows tool.

Special thanks to the TechPowerUp team for setting the standard in GPU diagnostics and maintaining the extensive [GPU Database](https://www.techpowerup.com/gpu-specs/), which this application links to via the "Lookup" button.

### Community Credits:
* **Samueru-sama** - for helping implement the universal AppImage (sharun) packaging architecture.
* **xndbogdan** - for creating the framework to support Nvidia and Intel graphics cards (preliminary support).
* **yobson** - for submitting and maintaining the AUR package.
* **yochananmarqos** - for submitting and maintaining the AUR package (stable).
* **dCo3lh0** - for adding GPU-T to the AppMan (AM) database.
* The **r/linux_gaming** community for the incredible feedback and support!

---
<p align="center">
  Created with ❤️ for the Linux Community.
</p>