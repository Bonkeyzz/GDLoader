# GDLoader: Mod Loader for Geometry Dash!

**This project is currently W.I.P and is not ready for the end-user.**

## Purpose
This project aims to allow people to load their own mods in Geometry Dash without using DLL injection or external programs to write to the game's memory.

## Installing
1. Download the [latest release](https://github.com/Bonkeyzz/GDLoader/releases) of the mod loader.
2. Extract it.
3. Open `GDPatcher.exe`, it will ask for a the game directory of the game. It usually is `C:\Program Files (x86)\Steam\steamapps\common\Geometry Dash` but if you have installed it in a different directory then make sure to use that.

## Issues
1. ~~`GDLoader.dll` Base address is being relocated so the program crashes when the injected code tries to call the `pre_init` function.~~ (**Fixed**)

## Creating Mods
An example addon is in this repo, currently only one function is implemented but more are coming in the future.

## Donations
If you like this project and want to support it, you can donate me on this [Paypal.Me link.](https://www.paypal.com/paypalme/bonkeyzz) Thank you for any support. :)
