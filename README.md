# GDLoader: Mod Loader for Geometry Dash!

**This project is currently W.I.P and is not ready for the end-user.**

## Purpose
This project aims to allow people to load their own mods in Geometry Dash without using DLL injection or external programs to write to the game's memory.

## Installing
0. If you don't have .NET Core 6 installed please install it. [Link](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.16-windows-x86-installer?cid=getdotnetcore). **Application may open and close without any output if you don't have it.**
1. Download the [latest release](https://github.com/Bonkeyzz/GDLoader/releases) of the mod loader.
2. Extract it.
3. Open `GDPatcher.exe`, A folder open dialog will open and you will have to point it to your GD install directory (Usually: `C:\Program Files (x86)\Steam\steamapps\common\Geometry Dash`, but if you have installed it in a different directory then make sure to use that).
4. If it says `Patch Done` then everything should be okay. You can now use Mods in the game!

## Issues
1. ~~`GDLoader.dll` Base address is being relocated so the program crashes when the injected code tries to call the `pre_init` function.~~ (**Fixed**)

## Creating Mods
An example addon is in this repo, currently only one function is implemented but more are coming in the future.

## Donations
If you like this project and want to support it, you can donate me on this [Paypal.Me link.](https://www.paypal.com/paypalme/bonkeyzz) Thank you for any support. :)
