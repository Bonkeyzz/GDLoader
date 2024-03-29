// LoaderBuilder: Internal tool that builds raw asm code for GD Patcher.

#define _CRT_SECURE_NO_WARNINGS               // Bad.

#include <iostream>
#include <Windows.h>

int getLen(void* funcaddress)
{
    int length = 0;
    for (length = 0; *((UINT32*)(&((unsigned char*)funcaddress)[length])) != 0xCCCCCCCC; ++length);
    return length;
}

std::string get_current_path(std::string append) {
    TCHAR buffer[MAX_PATH] = { 0 };
    GetModuleFileName(NULL, buffer, MAX_PATH);
    std::wstring::size_type pos = std::string(buffer).find_last_of("\\/");
    return std::string(buffer).substr(0, pos).append(append);
}

_declspec(naked) void* ldrCode()
{
	__asm
	{
		/* On_GD_Entrypoint */
        nop
        nop
        mov ebx, 0x41414141
    	call ebx
        mov ebx, 0x42424242
        mov esi, 0x43434343
    	jmp ebx 
    	nop
    	nop
    	/* ---------------- */
	}
}

int main()
{
    size_t ldrSize = getLen(ldrCode);
    void* data = malloc(ldrSize);
    memcpy(data, ldrCode, ldrSize);
	
    if (data != NULL)
    {
        FILE* out = fopen("code.bin", "wb");
        if (out != NULL)
        {
            const size_t wrote = fwrite(data, ldrSize, 1, out);
            if (wrote == ldrSize) printf("Failed to write! wrote 0x%x bytes and ldrSize is 0x%x\n", wrote, ldrSize);
            else printf("Done! Wrote 0x%x\n", wrote);
            fclose(out);
        }
        free(data);
    }
    printf("Press any key to continue...\n");
    getchar();
}
