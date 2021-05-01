using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PEEdit
{
	class Utils
	{
		public static void WriteLog(string value, ConsoleColor front, ConsoleColor back = default)
		{
			if (back != default)
			{
				Console.BackgroundColor = back;
			}
			Console.ForegroundColor = front;
			Console.WriteLine(value.PadRight(Console.WindowWidth - 1));
			Console.ResetColor();
		}

		public static void PrintExit(int exitCode)
		{
			WriteLog("Press [ENTER] to exit.", ConsoleColor.White);
			Console.ReadLine();
			Environment.Exit(exitCode);
		}

		public static byte[] createDetour(int addr, byte[] originalInstruction, int retAddr)

		{
			List<byte> loaderData = new List<byte>();
			byte[] jmpAddr = BitConverter.GetBytes(addr);
			byte[] retAddrb = BitConverter.GetBytes(retAddr);
			loaderData.AddRange(new byte[]
			{
				0x90, 0x90, // nop, nop
				0xBB, jmpAddr[0], jmpAddr[1], jmpAddr[2], jmpAddr[3], // mov ebx, jmpAddr
				0xFF, 0xD3, // call ebx
				0xBB, retAddrb[0], retAddrb[1], retAddrb[2], retAddrb[3] // mov ebx, retAddr
			});
			loaderData.AddRange(originalInstruction);
			loaderData.AddRange(new byte[]
			{
				0xFF, 0xE3, // jmp ebx
				0x90, 0x90
			});
			return loaderData.ToArray();
		}
		public static void WriteBytesToFile(string filename, uint addr, byte[] rawData)
		{
			FileStream fstream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
			fstream.Seek(addr, SeekOrigin.Current);
			fstream.Write(rawData);
			fstream.Flush();
			fstream.Dispose();
		}
	}
}
