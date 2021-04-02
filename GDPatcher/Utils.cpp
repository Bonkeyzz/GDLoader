#include "Utils.h"
#include <Windows.h>

#define CURRENTFILE         "Utils.cpp"
#define DebugMsg(f_, ...)   { printf("[DEBUG::%s] ", CURRENTFILE); printf((f_), ##__VA_ARGS__); printf("\n"); }

size_t Utils::replaceBytes(FILE* fi, FILE* fo, uint8_t* what, uint8_t* repl, size_t size)
{
    size_t i, index = 0, count = 0;
    int ch;
    while (EOF != (ch = fgetc(fi))) {
        if (ch == what[index]) {
            if (++index == size) {
                for (i = 0; i < size; ++i) {
                    fputc(repl[i], fo);
                }
                index = 0;
                ++count;
            }
        }
        else {
            for (i = 0; i < index; ++i) {
                fputc(what[i], fo);
            }
            index = 0;
            fputc(ch, fo);
        }
    }
    for (i = 0; i < index; ++i) {
        fputc(what[i], fo);
    }

    return count;
}

int Utils::getLoaderLen(void* funcaddress)
{
	int length = 0;
	for (length = 0; *((UINT32*)(&((unsigned char*)funcaddress)[length])) != 0xCCCCCCCC; ++length);
	return length;
}

int Utils::patchCodeSection(const char* inFile, const char* outFile)
{
    uint8_t toRepl[] = { 0xBF, 0x4E, 0xE6, 0x40, 0xBB, 0xBE, 0x00, 0x00, 0xFF, 0xFF };
    uint8_t with[] = { 0xBF, 0x4E, 0xE6, 0x40, 0xBB, 0xE9, 0xD0, 0xB8, 0x42, 0x00 };
	DebugMsg("Created New file: %s", inFile);
	DebugMsg("New final file: %s", outFile);

	FILE* fin, * fout;
	
	fin = fopen(inFile, "rb");
	fout = fopen(outFile, "wb");

	if (fin && fout) {
		int count = replaceBytes(fin, fout, toRepl, with, sizeof(toRepl));
		DebugMsg("number of replaceBytes count is %zu\n", count);
		fclose(fin);
		fclose(fout);
        DeleteFile(inFile);
		return 0;
	}
	return 1;
}