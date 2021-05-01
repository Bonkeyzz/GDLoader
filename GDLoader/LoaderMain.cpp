#include <Windows.h>
#include <string>
#include <filesystem>
#include <iostream>
#include <tchar.h>

namespace fs = std::filesystem;

typedef void(__cdecl* MOD_INIT)();

// From stackoverflow, not me...
void CreateConsole()
{
    if (!AllocConsole()) {
        return;
    }
    FILE* fDummy;
    freopen_s(&fDummy, "CONOUT$", "w", stdout);
    freopen_s(&fDummy, "CONOUT$", "w", stderr);
    freopen_s(&fDummy, "CONIN$", "r", stdin);
    std::cout.clear();
    std::clog.clear();
    std::cerr.clear();
    std::cin.clear();

    HANDLE hConOut = CreateFile(_T("CONOUT$"), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    HANDLE hConIn = CreateFile(_T("CONIN$"), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    SetStdHandle(STD_OUTPUT_HANDLE, hConOut);
    SetStdHandle(STD_ERROR_HANDLE, hConOut);
    SetStdHandle(STD_INPUT_HANDLE, hConIn);
    std::wcout.clear();
    std::wclog.clear();
    std::wcerr.clear();
    std::wcin.clear();
}

/// <summary>
/// Checks if a directory exists or not.
/// </summary>
/// <param name="dirPath">Absolute directory path</param>
/// <returns>true if directory is present, false if its not</returns>
BOOL dirExists(const std::string& dirPath)
{
	DWORD FilleAttr = GetFileAttributesA(dirPath.c_str());
	
	return (FilleAttr != INVALID_FILE_ATTRIBUTES  && FilleAttr & FILE_ATTRIBUTE_DIRECTORY) ? true : false;
}

/// <summary>
/// Get current executing path
/// </summary>
/// <returns>(wstring) Absolute current directory</returns>
std::wstring GetCurrentPath() {
	TCHAR buffer[MAX_PATH] = { 0 };
	GetModuleFileName(NULL, buffer, MAX_PATH);
	std::wstring::size_type pos = std::wstring(buffer).find_last_of(L"\\/");
	return std::wstring(buffer).substr(0, pos);
}

void __declspec(dllexport) __cdecl pre_init() {
	CreateConsole();
	std::wstring wcurrentDir = GetCurrentPath();
	wcurrentDir = wcurrentDir.append(L"\\Mods");
	std::string currentDir{ wcurrentDir.begin(), wcurrentDir.end() };
	if (dirExists(currentDir))
	{
		printf("[LoaderMain.cpp] hi.");
		printf("[LoaderMain.cpp] hi there.");
		for (const auto& entry : fs::directory_iterator(currentDir))
		{
			MOD_INIT preInitFunc;

			std::string mod_full_path = entry.path().string();
			std::string mod_filename = entry.path().filename().string();

			printf("[LoaderMain.cpp] Loading: %s\n", mod_filename.c_str());
			HINSTANCE lib = LoadLibraryA(mod_full_path.c_str());
			if (lib)
			{
				printf("[LoaderMain.cpp] Loaded: %s\n", mod_filename.c_str());
				preInitFunc = (MOD_INIT)GetProcAddress(lib, "on_gd_entrypoint");
				if (NULL != preInitFunc)
				{
					preInitFunc();
				}
				else
				{
					printf("[LoaderMain.cpp] Error initializing: %s\n", mod_filename.c_str());
				}
			}
			else
			{
				printf("[LoaderMain.cpp] Failed to load: %s\n", mod_filename.c_str());
			}

		}
	}
	else
	{
		printf("[LoaderMain.cpp] Directory 'Mods' does not exist, no mods are loaded.\n");
	}
}

void __declspec(dllexport) __cdecl post_init() {

}

void __declspec(dllexport) __cdecl on_title_screen() {

}