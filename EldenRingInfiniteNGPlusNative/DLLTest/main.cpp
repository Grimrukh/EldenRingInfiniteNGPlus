#include <windows.h>
#include <iostream>
#include <string>

void LogMessage(const std::wstring& message)
{
    std::wcout << message << std::endl;
}

void ReportError(const std::string& function) {
    // Get the last error and format it
    DWORD errorCode = GetLastError();
    LPVOID errorMessage;

    FormatMessage(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,
        errorCode,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPWSTR)&errorMessage,
        0, NULL);

    std::wcout << L"Error in " << function.c_str() << L": " << (LPWSTR)errorMessage << L" (Code " << errorCode << L")" << std::endl;

    // Free the buffer allocated by FormatMessage
    LocalFree(errorMessage);
}

int _main()
{
    // First, kill any running processes called `InfinteNGPlusConsole.exe`
    system("taskkill /f /im InfiniteNGPlusConsole.exe");

    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    SECURITY_ATTRIBUTES sa;

    // Print working directory.
    WCHAR buffer[MAX_PATH];
    GetCurrentDirectory(MAX_PATH, buffer);
    std::wcout << L"Current directory: " << buffer << std::endl;

    // Print directory of executable.
    GetModuleFileNameW(NULL, buffer, MAX_PATH);
    std::wcout << L"Executable directory: " << buffer << std::endl;

    // Find the last backslash in the path and remove the DLL file name
    for (int i = wcslen(buffer) - 1; i >= 0; --i)
    {
        if (buffer[i] == L'\\')
        {
            buffer[i] = L'\0'; // Terminate the string at the last backslash
            break;
        }
    }

    // Append the subfolder and executable name manually
    wcscat_s(buffer, MAX_PATH, L"\\EldenRingInfiniteNGPlus\\InfiniteNGPlusConsole.exe");
    LPCWSTR exePath = buffer;
    // LPCWSTR exePath = L"C:\\Windows\\System32\\Notepad.exe";

    // Check if exePath exists.
    DWORD dwAttrib = GetFileAttributes(exePath);
    if (dwAttrib == INVALID_FILE_ATTRIBUTES || (dwAttrib & FILE_ATTRIBUTE_DIRECTORY))
    {
		MessageBoxW(NULL, L"Console application not found!", L"Error", MB_ICONERROR | MB_OK);
		return 1;
	}

    HANDLE hThread = NULL;
    HANDLE hProcess = NULL;
    HANDLE hStdInWrite = NULL;

    // Set the security attributes struct to allow the pipe handles to be inherited
    ZeroMemory(&sa, sizeof(sa));
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;

    // Create a pipe for the stdin of the child process
    HANDLE hStdInRead = NULL;
    if (!CreatePipe(&hStdInRead, &hStdInWrite, &sa, 0))
    {
        MessageBoxW(NULL, L"Failed to create stdin pipe.", L"Error", MB_ICONERROR | MB_OK);
        return 1;
    }

    // Ensure the write handle to stdin is not inherited
    SetHandleInformation(hStdInWrite, HANDLE_FLAG_INHERIT, 0);

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);

    si.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW; // Redirect stdin and hide window
    si.hStdInput = hStdInRead; // Redirect stdin to the pipe
    si.wShowWindow = SW_SHOW;  // Hide the window

    ZeroMemory(&pi, sizeof(pi));

    // Start the console application
    if (CreateProcessW(
        exePath,             // Path to .NET 8.0 executable
        NULL,                // Command line arguments (NULL if none)
        NULL,                // Process handle not inheritable
        NULL,                // Thread handle not inheritable
        TRUE,                // Inherit handles so that child process inherits the pipe handles
        CREATE_NEW_CONSOLE,  // Creation flags (0 means no special flags)
        NULL,                // Use parent's environment block
        NULL,                // Use parent's starting directory
        &si,                 // Pointer to STARTUPINFO structure
        &pi))                // Pointer to PROCESS_INFORMATION structure
    {
        // Successfully started the process, store the process handle
        hProcess = pi.hProcess;
        
        CloseHandle(pi.hThread); // Close the thread handle, we only need the process handle

        // Close the read handle to stdin, as we only need the write handle
        CloseHandle(hStdInRead);

        LogMessage(L"Console app started successfully. Process handle: " + std::to_wstring((DWORD)hProcess));

        LogMessage(L"Waiting for console app to exit...");
        // Wait for the process to exit
        DWORD result = WaitForSingleObject(hProcess, INFINITE);

        if (result == WAIT_OBJECT_0)
        {
            // Process has exited, check the exit code
            DWORD exitCode;
            if (GetExitCodeProcess(hProcess, &exitCode))
            {
                if (exitCode != 0)
                {
                    // If the exit code is non-zero, show an error message
                    MessageBoxW(NULL, L"Console application stopped unexpectedly!", L"Error", MB_ICONERROR | MB_OK);
                }
            }
        }
        CloseHandle(hProcess);
        hProcess = NULL;
    }
    else
    {
        LogMessage(L"Failed to start the .NET console app.");
        // Failed to start the process
        MessageBoxW(NULL, L"Failed to start the .NET console app.", L"Error", MB_ICONERROR | MB_OK);
    }

    return 0;
}

int main() {
    
    // Specify the name or path of the DLL you want to load: "EldenRingInfiniteNGPlusLauncher.dll"
    LPCWSTR dllName = L"EldenRingInfiniteNGPlusLauncher.dll";

    // Attempt to load the DLL
    HMODULE hModule = LoadLibrary(dllName);

    if (hModule == NULL)
    {
        std::cerr << "Failed to load DLL: " << dllName << std::endl;
        ReportError("LoadLibrary");
    }
    else
    {
        std::cout << "Successfully loaded DLL: " << dllName << std::endl;

        std::cout << "Main thread sleeping for 25 seconds..." << std::endl;
        Sleep(25000); // Sleep for 25,000 milliseconds (25 seconds)

        // If you want to unload the DLL after successful load
        if (!FreeLibrary(hModule)) {
            std::cerr << "Failed to unload the DLL" << std::endl;
            ReportError("FreeLibrary");
        }
        else {
            std::cout << "Successfully unloaded the DLL" << std::endl;
        }
    }
    
    std::cout << "Program terminating." << std::endl;
    return 0;
}