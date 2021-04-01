#include <cstdio>
#include <string>
#include <iostream>

#include "PEFile.h"

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
int main()
{
	const std::string opened_file = create_open_file_dialog();
	if(opened_file.empty())
	{
        printf("No file selected, aborting...\n");
        printf("Press [ENTER] to exit.\n");
        std::cin.get();
        return 0;
	}
	
    printf("Opening file %s...\n", opened_file.c_str());
    PEFile pe(opened_file.c_str());

	// Create PE Section for the loader (with the size of 0x32 and marked as executable)
    int testsecLoc = pe.addSection(".loader", 0x32, true);

	// Add imports with the 3 base init functions
    const char* functions[] = { "?pre_init@@YAXXZ", "?post_init@@YAXXZ", "?on_title_screen@@YAXXZ" };
    pe.addImport("GDLoader.dll", functions, 3);

	// Disable DLL repositioning
	// I have encountered an error with the code cave. DLL functions do not get resolved cause DLL is being relocated.
	// TODO: Re-enable DLL repositioning for patched executable and handle dynamic library loading
    pe.peHeaders.OptionalHeader.DllCharacteristics = IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE | IMAGE_DLLCHARACTERISTICS_NX_COMPAT;

	// Add code into the section
	// TODO: Add shellcode for executing the 3 DLL functions above
    const char* data = "\x90\x90\x90\x90";
    ZeroMemory(pe.sections[testsecLoc].RawData, 0x32); // Making sure section does not have any data
    memcpy(pe.sections[testsecLoc].RawData, data, sizeof(data));

	// Save the modified executable
	pe.saveToFile("GDPatched.exe");
    printf("File is patched!\n");
    printf("Press [ENTER] to exit.\n");
    std::cin.get();
}