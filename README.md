# Kuriimu2
Kuriimu is a general purpose game translation project manager and toolkit for authors of fan translations and game mods.

## Requirements
You need to have Net Core Desktop Runtime 3.1.6 installed on your system.<br>
You can get it here: https://dotnet.microsoft.com/download/dotnet-core/3.1

## Download
You can download the newest build of Kuriimu2 in the "Actions" tab. Just select the newest successful build and download its artifact.<br>
You need to be logged in at github to download artifacts from a successful build.
Those builds are considered beta and can contain bugs and unfinished features.

## Usage
Kuriimu2 consists of many libraries and user interfaces. A build from "Actions" contains a ready-to-run GUI that you can run.

### Architecture
1. Kontract - The main API host for all interfaces and base classes. Defines interfaces like ITextAdapter, IFontAdapter, ILoadFiles, etc...
1. Komponent - A series of tools used by plugins, Kore and sometimes the UI, contains BinaryReader/WriterX and a bunch of other utility classes and helpers.
1. Kanvas - The image library. Handles all things images, ETC1/A4, DXT, PVRTC, ATC, IndexedColor, etc...
1. Kryptography - Contains all compression, encryption, and hashing classes.
1. Kore - The main API that the UI and eventual CLI uses to load plugins and do all Plugin-bound functions. Batch import/export and other functions.
1. Kuriimu2 - The WPF UI that is the main user-side program.

### Plugins
* Plugins currently make use of a dev-side nuget package that contains all five of the main libraries.
  * The libraries will be separated later on down the road.
