#include <cstdio>
#include <Windows.h>

extern "C" void __declspec(dllexport) __cdecl on_gd_entrypoint(void) {
	MessageBoxA(0, "Another example Loaded!", "", MB_ICONINFORMATION | MB_OK);
	printf("Woah.\n");
}