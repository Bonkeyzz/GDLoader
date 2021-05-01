using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using Ladybug.Core;
using Ladybug.Core.Windows;
using PeNet;
using PeNet.Header.Net.MetaDataTables;
using PeNet.Header.Pe;
using Microsoft.Win32;


namespace PEEdit
{
	class Program
	{
		static IntPtr gdLoaderBaseAddr = IntPtr.Zero;
		static uint preInitOffset = 0x1196A;
		static int addressOfPreInit = 0;
		private static string gdPath;
		private static ImageSectionHeader loaderHdr = null;
		private static ImageSectionHeader textHdr = null;
		private static bool foundLibrary = false;
		static void Main(string[] args)
		{
			Console.Write("Enter full game path of game 'Geometry Dash': ");
			gdPath = Console.ReadLine();
			if (!System.IO.File.Exists($"{gdPath}\\GeometryDash.exe"))
			{
				Utils.WriteLog("File 'GeometryDash.exe' does not exist!", ConsoleColor.DarkRed);
				Utils.PrintExit(-1);
			}

			if (System.IO.File.Exists($"{Environment.CurrentDirectory}\\GDLoader.dll"))
			{
				if (System.IO.File.Exists($"{gdPath}\\GDLoader.dll"))
					System.IO.File.Delete($"{gdPath}\\GDLoader.dll");
				System.IO.File.Copy($"{Environment.CurrentDirectory}\\GDLoader.dll", $"{gdPath}\\GDLoader.dll");
			}

			if (System.IO.Directory.Exists($"{Environment.CurrentDirectory}\\Mods"))
			{
				if (!System.IO.Directory.Exists($"{gdPath}\\Mods"))
				{
					System.IO.Directory.CreateDirectory($"{gdPath}\\Mods");
					if (System.IO.File.Exists($"{Environment.CurrentDirectory}\\Mods\\ExampleAddon.dll"))
					{
						System.IO.File.Copy($"{Environment.CurrentDirectory}\\Mods\\ExampleAddon.dll", $"{gdPath}\\Mods\\ExampleAddon.dll");
					}
				}
			}

			System.IO.File.Copy($"{gdPath}\\GeometryDash.exe", $"{gdPath}\\GeometryDash.bak", true);
			var peFile = new PeFile($"{gdPath}\\GeometryDash.exe");
			var GDLoaderFile = new PeFile($"{gdPath}\\GDLoader.dll");
			preInitOffset = GDLoaderFile.ExportedFunctions.ToList().Find(x => x.Name == "?pre_init@@YAXXZ").Address;
			Utils.WriteLog($"pre_init func Offset: 0x{preInitOffset:x}", ConsoleColor.Yellow);
			if (peFile.ImageNtHeaders != null) 
				peFile.ImageNtHeaders.OptionalHeader.DllCharacteristics = DllCharacteristicsType.NxCompat | DllCharacteristicsType.TerminalServerAware;
			else
			{
				Utils.WriteLog("IMAGE_NT_HEADERS is null!", ConsoleColor.DarkRed);
				Utils.PrintExit(-1);
			}

			peFile.AddSection(".loader", 32, ScnCharacteristicsType.MemExecute | ScnCharacteristicsType.MemRead | ScnCharacteristicsType.MemWrite);
			loaderHdr = peFile.ImageSectionHeaders.ToList().Find(x => x.Name == ".loader");
			textHdr = peFile.ImageSectionHeaders.ToList().Find(x => x.Name == ".text");
			peFile.AddImport("GDLoader.dll", "?pre_init@@YAXXZ");

			System.IO.File.WriteAllBytes($"{gdPath}\\GeometryDash_stage1patch.exe", peFile.RawFile.ToArray());

			DebuggerSession debugSession = new DebuggerSession();
			debugSession.ProcessStarted += DebugSessionOnProcessStarted;
			debugSession.LibraryLoaded += DebugSessionOnLibraryLoaded;
			debugSession.ExceptionOccurred += DebugSessionOnExceptionOccurred;
			debugSession.ProcessTerminated += DebugSessionOnProcessTerminated;

			Utils.WriteLog($"Starting debugger on file: '{gdPath}\\GeometryDash_stage1patch.exe'", ConsoleColor.DarkGray);
			var dbgSession = debugSession.StartProcess(new DebuggerProcessStartInfo
			{
				CommandLine = $"{gdPath}\\GeometryDash_stage1patch.exe"
			});

			while (debugSession.IsActive)
			{
				if (foundLibrary)
				{
					dbgSession.Terminate();
					dbgSession.Dispose();
					break;
				}
			}
			debugSession.Dispose();
			if (foundLibrary) onPostDebugSuccess();
			else
			{
				Utils.WriteLog("Library not found. Exiting...", ConsoleColor.Red);
				Utils.PrintExit(-1);
			}
		}

		private static void DebugSessionOnProcessTerminated(object? sender, DebuggeeProcessEventArgs e)
		{
			Utils.WriteLog($"[Debugger] Process: {e.Process.Id} exited.", ConsoleColor.DarkGray);
		}

		private static void DebugSessionOnExceptionOccurred(object? sender, DebuggeeExceptionEventArgs e)
		{
			Utils.WriteLog("[Debugger] EXCEPTION!", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Error code: {e.Exception.ErrorCode}", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Message: {e.Exception.Message}", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Is continuable: {e.Exception.Continuable}", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Is first chance: {e.Exception.IsFirstChance}", ConsoleColor.Red);
			if (e.Exception.Continuable)
			{
				e.Session.Continue(DebuggerAction.Continue);
			}
		}

		private static void DebugSessionOnLibraryLoaded(object? sender, DebuggeeLibraryEventArgs e)
		{
			// Utils.WriteLog($"[Debugger] Loaded lib: {e.Library.Name} (0x{e.Library.BaseOfLibrary:x})", ConsoleColor.DarkGray);

			if (e.Library.Name != null && e.Library.Name.Contains("GDLoader"))
			{
				Utils.WriteLog($"[Debugger] GDLoader.dll found at: 0x{e.Library.BaseOfLibrary:x}", ConsoleColor.DarkGray);
				gdLoaderBaseAddr = e.Library.BaseOfLibrary;
				addressOfPreInit = gdLoaderBaseAddr.ToInt32() + (int)preInitOffset;
				Utils.WriteLog($"[Debugger] pre_init addr: 0x{addressOfPreInit:x}", ConsoleColor.Yellow);
				Utils.WriteLog("[Debugger] Debugger Ladybug was made by Washi1337 (https://github.com/Washi1337).", ConsoleColor.Cyan);
				foundLibrary = true;
			}
		}

		private static void onPostDebugSuccess()
		{
			if (loaderHdr != null)
			{
				// TODO: Remove this code and use a better way of detouring in the future.
				byte[] data = Utils.createDetour(addressOfPreInit, new byte[] {0xBE, 0x00, 0x00, 0xFF, 0xFF}, 0x00662730);

				System.IO.File.Copy($"{gdPath}\\GeometryDash_stage1patch.exe", $"{gdPath}\\GeometryDash_stage2patch.exe");
				System.IO.File.Delete($"{gdPath}\\GeometryDash_stage1patch.exe");

				Utils.WriteBytesToFile($"{gdPath}\\GeometryDash_stage2patch.exe", loaderHdr.PointerToRawData, data);
				Utils.WriteBytesToFile($"{gdPath}\\GeometryDash_stage2patch.exe", 0x261B2B, new byte[]{ 0xE9, 0xD0, 0xB8, 0x42, 0x00 });

				Utils.WriteLog("Wrote to .loader section.", ConsoleColor.Cyan);
			}
			System.IO.File.Delete($"{gdPath}\\GeometryDash.exe");
			System.IO.File.Move($"{gdPath}\\GeometryDash_stage2patch.exe", $"{gdPath}\\GeometryDash.exe");
			Utils.WriteLog("Patch done.", ConsoleColor.Green);
			Utils.PrintExit(0);
		}
		private static void DebugSessionOnProcessStarted(object? sender, DebuggeeProcessEventArgs e)
		{
			Utils.WriteLog($"[Debugger] Started process: {e.Process.Id}", ConsoleColor.DarkGray);
		}
	}
}

