using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Ladybug.Core;
using Ladybug.Core.Windows;
using Microsoft.Win32.SafeHandles;
using PEEdit;
using PeNet;
using PeNet.Header.Pe;

namespace GDPatcher
{
	class Program
	{
		static IntPtr _gdLoaderBaseAddr = IntPtr.Zero;
		static uint _preInitOffset = 0x1196A;
		static int _addressOfPreInit;
		public static string _gdPath { get; private set; }
		private static ImageSectionHeader _loaderHdr;
		private static bool _foundLibrary;
		public static void CopyFiles()
		{

			if (File.Exists($"{Environment.CurrentDirectory}\\GDLoader.dll"))
			{
				if(File.Exists($"{_gdPath}\\GDLoader.dll")) Utils.DeleteFile($"{_gdPath}\\GDLoader.dll");
				File.Copy($@"{Environment.CurrentDirectory}\\GDLoader.dll", $"{_gdPath}\\GDLoader.dll");
			}

			if (Directory.Exists($"{Environment.CurrentDirectory}\\Mods"))
			{
				if (!Directory.Exists($"{_gdPath}\\Mods"))
				{
					Directory.CreateDirectory($"{_gdPath}\\Mods");
					if (File.Exists($@"{Environment.CurrentDirectory}\\Mods\\ExampleAddon.dll"))
					{
						File.Copy($@"{Environment.CurrentDirectory}\\Mods\\ExampleAddon.dll", $"{_gdPath}\\Mods\\ExampleAddon.dll");
					}
				}
			}
		}
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length <= 0)
			{
				FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
				folderBrowserDialog.UseDescriptionForTitle = true;
				folderBrowserDialog.ShowNewFolderButton = false;
				folderBrowserDialog.Description = "Select the installation directory for Geometry Dash...";
				if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				{
					_gdPath = folderBrowserDialog.SelectedPath;
				}
				else
				{
					Utils.WriteLog("Invalid or null path selected.", ConsoleColor.DarkRed);
					Utils.PrintExit(-1);
				}
			}
			else
			{
				_gdPath = args[0];
			}

			if (!File.Exists($"{_gdPath}\\GeometryDash.exe"))
			{
				Utils.WriteLog("File 'GeometryDash.exe' does not exist!", ConsoleColor.DarkRed);
				Utils.PrintExit(-1);
			}
			CopyFiles();

			File.Copy($"{_gdPath}\\GeometryDash.exe", $"{_gdPath}\\GeometryDash.bak", true);
			var peFile = new PeFile($"{_gdPath}\\GeometryDash.exe");
			var GDLoaderFile = new PeFile($"{_gdPath}\\GDLoader.dll");
			_preInitOffset = GDLoaderFile.ExportedFunctions.ToList().Find(x => x.Name == "?pre_init@@YAXXZ").Address;
			Utils.WriteLog($"pre_init func Offset: 0x{_preInitOffset:x}", ConsoleColor.Yellow);
			if (peFile.ImageNtHeaders != null) 
				peFile.ImageNtHeaders.OptionalHeader.DllCharacteristics = DllCharacteristicsType.NxCompat | DllCharacteristicsType.TerminalServerAware;
			else
			{
				Utils.WriteLog("IMAGE_NT_HEADERS is null!", ConsoleColor.DarkRed);
				Utils.PrintExit(-1);
			}

			peFile.AddSection(".loader", 32, ScnCharacteristicsType.MemExecute | ScnCharacteristicsType.MemRead | ScnCharacteristicsType.MemWrite);
			_loaderHdr = peFile.ImageSectionHeaders.ToList().Find(x => x.Name == ".loader");
			peFile.AddImport("GDLoader.dll", "?pre_init@@YAXXZ");

			FileInfo stage1 = new FileInfo($"{_gdPath}\\GeometryDash_stage1patch.exe");
			//File.WriteAllBytes($"{_gdPath}\\GeometryDash_stage1patch.exe", peFile.RawFile.ToArray());

			FileStream stage1Stream = File.OpenWrite($"{_gdPath}\\GeometryDash_stage1patch.exe");
			stage1Stream.Write(peFile.RawFile.ToArray(), 0, peFile.RawFile.ToArray().Length);
			stage1Stream.Flush();
			stage1Stream.Dispose();

			DebuggerSession debugSession = new DebuggerSession();
			debugSession.ProcessStarted += DebugSessionOnProcessStarted;
			debugSession.LibraryLoaded += DebugSessionOnLibraryLoaded;
			debugSession.ExceptionOccurred += DebugSessionOnExceptionOccurred;
			debugSession.ProcessTerminated += DebugSessionOnProcessTerminated;

			Utils.WriteLog($"Starting debugger on file: '{_gdPath}\\GeometryDash_stage1patch.exe'", ConsoleColor.DarkGray);
			var dbgSession = debugSession.StartProcess(new DebuggerProcessStartInfo
			{
				CommandLine = $"{_gdPath}\\GeometryDash_stage1patch.exe"
			});

			while (debugSession.IsActive)
			{
				if (_foundLibrary)
				{
					dbgSession.Terminate();
					dbgSession.Dispose();
					break;
				}
			}
			debugSession.Dispose();
		}

		private static void DebugSessionOnProcessTerminated(object sender, DebuggeeProcessEventArgs e)
		{
			Utils.WriteLog($"[Debugger] Process: {e.Process.Id} exited.", ConsoleColor.Cyan);
		}

		private static void DebugSessionOnExceptionOccurred(object sender, DebuggeeExceptionEventArgs e)
		{
			Utils.WriteLog("[Debugger] EXCEPTION!", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Error code: {e.Exception.ErrorCode}", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Message: {e.Exception.Message}", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Is continuable: {e.Exception.Continuable}", ConsoleColor.Red);
			Utils.WriteLog($"[Debugger] Is first chance: {e.Exception.IsFirstChance}", ConsoleColor.Red);
			if (e.Exception.Continuable)
			{
				Utils.WriteLog("[Debugger] Continuing...", ConsoleColor.Yellow);
				e.Session.Continue(DebuggerAction.Continue);
			}
		}

		private static void DebugSessionOnLibraryLoaded(object sender, DebuggeeLibraryEventArgs e)
		{
			Utils.WriteLog($"[Debugger] Loaded lib: {e.Library.Name} (0x{e.Library.BaseOfLibrary:x})", ConsoleColor.DarkGray);

			if (e.Library.Name != null && e.Library.Name.Contains("GDLoader"))
			{
				Utils.WriteLog($"[Debugger] GDLoader.dll found at: 0x{e.Library.BaseOfLibrary:x}", ConsoleColor.DarkGray);
				_gdLoaderBaseAddr = e.Library.BaseOfLibrary;
				_addressOfPreInit = _gdLoaderBaseAddr.ToInt32() + (int)_preInitOffset;
				Utils.WriteLog($"[Debugger] pre_init Addr: 0x{_addressOfPreInit:x}", ConsoleColor.Yellow);
				Utils.WriteLog("[Debugger] Debugger Ladybug was made by Washi1337 (https://github.com/Washi1337).", ConsoleColor.Cyan);
				_foundLibrary = true;
				Thread.Sleep(2000);
				if (_foundLibrary) onPostDebugSuccess();
				else
				{
					Utils.WriteLog("Library not found. Exiting...", ConsoleColor.Red);
					Utils.PrintExit(-1);
				}
			}
		}

		private static void onPostDebugSuccess()
		{
			try
			{
				if (_loaderHdr != null)
				{
					// TODO: Remove this code and use a better way of detouring in the future.
					byte[] data = Utils.createDetour(_addressOfPreInit, new byte[] { 0xBE, 0x00, 0x00, 0xFF, 0xFF }, 0x00662730);

					File.Copy($"{_gdPath}\\GeometryDash_stage1patch.exe", $"{_gdPath}\\GeometryDash_stage2patch.exe");

					Utils.WriteBytesToFile($"{_gdPath}\\GeometryDash_stage2patch.exe", _loaderHdr.PointerToRawData, data);
					Utils.WriteBytesToFile($"{_gdPath}\\GeometryDash_stage2patch.exe", 0x261B2B, new byte[] { 0xE9, 0xD0, 0xB8, 0x42, 0x00 });

					Utils.WriteLog("Wrote to .loader section.", ConsoleColor.Cyan);
				}
				File.Delete($"{_gdPath}\\GeometryDash.exe");
				File.Move($"{_gdPath}\\GeometryDash_stage2patch.exe", $"{_gdPath}\\GeometryDash.exe");
				Utils.WriteLog("Patch done.", ConsoleColor.Green);
				Utils.PrintExit(0);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Utils.PrintExit(-1);
				throw;
			}
		}
		private static void DebugSessionOnProcessStarted(object sender, DebuggeeProcessEventArgs e)
		{
			Utils.WriteLog($"[Debugger] Started process: {e.Process.Id}", ConsoleColor.DarkGray);
		}
	}
}

