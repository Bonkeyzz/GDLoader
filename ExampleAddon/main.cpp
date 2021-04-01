#include <Windows.h>

extern "C" void __declspec(dllexport) __cdecl init(void) {
	MessageBoxA(0, "Example Loaded!", "", MB_ICONINFORMATION | MB_OK);
}