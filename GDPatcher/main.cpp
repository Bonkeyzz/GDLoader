

#include <cstdio>
#include <string>
#include <iostream>
#include <filesystem>

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>

#include "PEFile.h"
#include "__global_macro.h"

#include "Utils.h"

namespace fs = std::filesystem;


#ifdef DEBUG_ENABLED
#define CURRENTFILE         "main.cpp"
#define DebugMsg(f_, ...)   { printf("[DEBUG::%s] ", CURRENTFILE); printf((f_), ##__VA_ARGS__); printf("\n"); }
#else
#define DebugMsg(f_, ...) ;
#endif

std::string get_current_path() {
    TCHAR buffer[MAX_PATH] = { 0 };
    GetModuleFileName(NULL, buffer, MAX_PATH);
    std::wstring::size_type pos = std::string(buffer).find_last_of("\\/");
    return std::string(buffer).substr(0, pos);
}
std::string create_open_file_dialog() {
    OPENFILENAME ofn;
    char fileName[MAX_PATH] = "";
    ZeroMemory(&ofn, sizeof(ofn));
    std::string title = "Select Geometry Dash executable...";
	
    ofn.lStructSize = sizeof(OPENFILENAME);
    ofn.hwndOwner = 0;
    ofn.lpstrFilter = "Executable File (*.exe)\0*.exe\0";
    ofn.lpstrFile = fileName;
    ofn.nMaxFile = MAX_PATH;
    ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;
    ofn.lpstrDefExt = "";
    ofn.lpstrTitle = const_cast<LPSTR>(title.c_str());
    std::string absoluteFilePath;

    if (GetOpenFileName(&ofn))
        absoluteFilePath = fileName;

    return absoluteFilePath;
}

__declspec(naked) void* ldrCode()
{
    __asm
    {
    	/* On_GD_Entrypoint */
        pushad
        pushfd
        mov ebx, 0x7C122CE0
        call ebx
        mov ebx, 0x00662730
        mov esi, 0xFFFF0000
    	jmp ebx 
    	nop
    	nop
    	/* ---------------- */
    }
}

int main()
{
    Utils utils;
	const std::string opened_file = create_open_file_dialog();
	if(opened_file.empty())
	{
        printf("[*] No file selected, aborting...\n");
        printf("Press [ENTER] to exit.\n");
        std::cin.get();
        return 0;
	}

    fs::path currentDir = opened_file;
    DebugMsg("Exe File: %s", opened_file.c_str());
    currentDir = currentDir.remove_filename();

	printf("[!] Opening file %s...\n", opened_file.c_str());
    PEFile pe(opened_file.c_str());

	// Create PE Section for the loader (with the size of 0x32 and marked as executable)
    int testsecLoc = pe.addSection(".loader", 0x32, true);

	// Add imports with the 3 base on_gd_entrypoint functions
    const char* functions[] = { "?pre_init@@YAXXZ", "?post_init@@YAXXZ", "?on_title_screen@@YAXXZ" };
    pe.addImport("GDLoader.dll", functions, 3);

	// Disable DLL repositioning
	// I have encountered an error without this. DLL functions do not get resolved cause DLL is being relocated in memory.
    pe.peHeaders.OptionalHeader.DllCharacteristics = IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE | IMAGE_DLLCHARACTERISTICS_NX_COMPAT;

    DebugMsg("ldrCode size: 0x%x", utils.getLoaderLen(ldrCode));
	
	// Add code into the section
	ZeroMemory(pe.sections[testsecLoc].RawData, 0x32); // Making sure section does not have any data
	memcpy(pe.sections[testsecLoc].RawData, ldrCode, utils.getLoaderLen(ldrCode));


	// Save the modified executable
	if(pe.saveToFile("GeometryDash_1.exe"))
	{
        const char* currentPatchedfile = currentDir.append("GeometryDash_1.exe").string().data();
        currentDir.remove_filename();
        const char* newPatchedFile = currentDir.append("GeometryDash_patched.exe").string().data();


        int status = utils.patchCodeSection(currentPatchedfile, newPatchedFile);

        if (status == 0)
        {
            printf("[!] File is patched successfully!\n");
            rename(opened_file.c_str(), "GeometryDash_original.bak");
            rename(newPatchedFile, "GeometryDash.exe");
        }
        else
        {
            printf("[*] Failed to patch file!\n");
        }
	}
    else
    {
        printf("[*] Failed to patch file!\n");
    }
    printf("Press [ENTER] to exit.\n");
    std::cin.get();
}