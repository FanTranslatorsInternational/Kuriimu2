# Kuriimu2
Kuriimu is a general purpose game translation project manager and toolkit for authors of fan translations and game mods.

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

#### Building the Nuget Packge for Plugin Developement
* TBD
