#pragma once
#define _CRT_SECURE_NO_WARNINGS

#include <cstdint>
#include <cstdio>
#include <string>

class Utils
{
public:
	int getLoaderLen(void* funcaddress);
	int patchCodeSection(const char* inFile, const char* outFile);
private:
	size_t replaceBytes(FILE* fi, FILE* fo, uint8_t* what, uint8_t* repl, size_t size);
};
