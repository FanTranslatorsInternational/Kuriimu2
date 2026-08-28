<div align="center">

<img src="kuriimu2icon.png" alt="Kuriimu2">

# Kuriimu2

### A toolkit for fan translation, ROM hacking, and game modding

<a href="https://github.com/FanTranslatorsInternational/Kuriimu2/releases"><img src="https://img.shields.io/github/v/release/FanTranslatorsInternational/Kuriimu2" alt="Releases"></a>
![GitHub downloads](https://img.shields.io/github/downloads/FanTranslatorsInternational/Kuriimu2/total)
<a href="https://github.com/FanTranslatorsInternational/Kuriimu2/issues"><img src="https://img.shields.io/github/issues/FanTranslatorsInternational/Kuriimu2" alt="Issues"></a>
<a href="https://github.com/FanTranslatorsInternational/Kuriimu2/blob/imgui/LICENSE.md"><img src="https://img.shields.io/github/license/FanTranslatorsInternational/Kuriimu2" alt="License"></a>
[![Discord](https://img.shields.io/discord/216681627294629888?label=Discord&logo=discord)](https://discord.gg/6NzbPkaAwf)

</div>

---

# Overview

Kuriimu2 is a general-purpose game translation project manager and toolkit designed for fan translators, ROM hackers, and game modders.

Built around a modular plugin architecture, Kuriimu2 provides a unified environment for inspecting, extracting, editing, and rebuilding game resources across multiple platforms and formats.

Rather than attempting to replace specialized tooling, Kuriimu2 aims to unify many fragmented approaches and legacy applications into a single accessible ecosystem.

Kuriimu2 is intended to serve as the first stop when exploring a game's structure, helping you to quickly open and inspect formats, experiment with resource modifications, and build workflows from there.

Depending on your project's needs, you may continue entirely within Kuriimu2, extend it through plugins, or integrate its ecosystem alongside specialized external tools better suited for game-specific requirements.

---

# Features

- Plugin-driven architecture
- Archive, image, and text extraction and injection
- Font inspection and modification
- GUI and command-line builds
- Extensible API for custom plugins
- Modern ImGui-based interface
- Open-source and community-driven

---

# Screenshots

## Archive Editor

<img width="1202" height="732" alt="image" src="https://github.com/user-attachments/assets/41467493-70e5-4640-91a9-4b0a740754a9" />

## Image Editor

<img width="1202" height="732" alt="image" src="https://github.com/user-attachments/assets/a7485c7d-136d-4bba-bbb6-1d291aaf2217" />

## Text Editor

<img width="1202" height="732" alt="image" src="https://github.com/user-attachments/assets/171d3f97-e15f-46cf-93af-31912cd4b564" />

## Font Editor

<img width="1202" height="732" alt="image" src="https://github.com/user-attachments/assets/6608e736-87a9-427d-ada3-5ea70c090855" />

---

# Requirements

Install the latest Microsoft Visual C++ Redistributable:

https://aka.ms/vc14/vc_redist.x64.exe

---

# Downloads

## Stable Releases

Download the latest stable release here:

https://github.com/FanTranslatorsInternational/Kuriimu2/releases/latest

## Beta Builds

### GUI Builds

https://github.com/FanTranslatorsInternational/Kuriimu2-ImGuiForms-Update/releases

### CommandLine Builds

https://github.com/FanTranslatorsInternational/Kuriimu2-CommandLine-Update/releases

---

# Quick Start

1. Download the latest release
2. Extract the archive
3. Launch `Kuriimu2.exe`
4. Drag & Drop supported game files
7. Edit, extract, or inject resources

---

# Supported Formats

Kuriimu2 supports a large collection of game formats. Because of the amount, a compatibility spreadsheet is maintained here:

https://docs.google.com/spreadsheets/d/1LbRqXkJUi4WD0awJMWInEfSiGtTIc2hu7ag2ngdoVC0/edit?usp=sharing

---

# Architecture

Kuriimu2 uses a modular plugin architecture that separates core functionality from game- and format-specific implementations.

This allows:

- Independent plugin development
- Better long-term scalability
- Shared tooling across projects

---

# Repository Structure

```text
Kuriimu2/
├── src/
│   ├── ui/         # User interface source code
│   └── lib/        # Core libraries and shared functionality
└── plugins/        # File format and game-specific plugins
```

---

# Plugin Development

Kuriimu2 supports plugins for archives, image formats, text formats, and fonts.

Documentation for plugin development and project architecture can be found on the wiki:

https://github.com/FanTranslatorsInternational/Kuriimu2/wiki

---

# Contributing

Contributions are always welcome!

Whether you're interested in:

- Bug fixes
- New plugins
- Documentation improvements
- UI improvements
- Platform support

---

# Building From Source

## Requirements

- Visual Studio 2022+
- .NET SDK 8.0+
- Git

## Clone Repository

```bash
git clone https://github.com/FanTranslatorsInternational/Kuriimu2.git
```

## Build

```bash
dotnet build
```

---

# Credits

Kuriimu2 is developed and maintained by dedicated contributors from various modding and programming communities.

Special thanks to:

- YW Modding Zone
- UI Translators

---

# Disclaimer

Kuriimu2 is intended for preservation, translation, research, and modding purposes.

Users are responsible for complying with the laws and regulations applicable in their country when modifying or distributing game data.
