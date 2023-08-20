# Kuriimu2
Kuriimu2 is a general purpose game translation project manager and toolkit for authors of fan translations and game mods.

## Requirements
You need to have the .Net Core Desktop Runtime 3.1.6 installed on your system.<br>
If you are on Mac or Linux, you can just install the .Net Core Runtime 3.1.6.

We build against the .Net Core SDK version 3.1.302.<br>
You can get it here: https://dotnet.microsoft.com/download/dotnet-core/3.1

We only build for x64. If you have a x86 operating system, then the software will not run.<br>
Make sure to download x64 version of the above mentioned runtimes only.
We will not provide x86 builds of this software. You can compile it yourself if you really need to.

## Download
You can download the latest release of Kuriimu2 from our release page:<br>
https://github.com/FanTranslatorsInternational/Kuriimu2/releases/latest

There are several different UI's you can choose from:
1. EtoForms: A graphical user interface in a look native to the respective operating system. There are releases for Mac, Linux, and Windows.
1. CommandLine: A command line interface of the main features of Kuriimu2. There are releases for Windows only.

## Development builds
You can download the latest developer build of Kuriimu2 from our [Actions](https://github.com/FanTranslatorsInternational/Kuriimu2/actions) tab.<br>
Just select the latest successful build and download its artifact.

You need to be logged in at github to download artifacts from a successful build.<br>
Those builds are considered beta and can contain bugs and unfinished features.

## Wiki
For documentation on developing for Kuriimu2, like creating plugins, or an explanation of our general archiveitecture,<br>
please refer to our [Wiki](https://github.com/FanTranslatorsInternational/Kuriimu2/wiki).

## Known issues
### Linux
1. Drag&Drop is currently unsupported on various ArchLinux distributions, including Manjaro.
