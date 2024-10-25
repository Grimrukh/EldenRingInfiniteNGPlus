#include "pch.h"
#include <windows.h>
#include <iostream>
#include <string>

HANDLE hThread = NULL;
HANDLE hProcess = NULL;
HANDLE hStdInWrite = NULL;

void LogMessage(const std::wstring& message)
{
	std::wcout << message << std::endl;
}

// Helper function to get the DLL's directory path without using Shlwapi
void GetDllDirectory(HMODULE hModule, WCHAR* dllPath, DWORD size)
{
    // Get the full path of the DLL
    GetModuleFileNameW(hModule, dllPath, size);

    // Find the last backslash in the path and remove the DLL file name
    for (int i = wcslen(dllPath) - 1; i >= 0; --i)
    {
        if (dllPath[i] == L'\\')
        {
            dllPath[i] = L'\0'; // Terminate the string at the last backslash
            break;
        }
    }
}

// Function to start the .NET Console App in a new process and hide the console window
DWORD WINAPI StartConsoleApp(LPVOID lpParam)
{
    WCHAR dllPath[MAX_PATH];
    GetDllDirectory((HMODULE)lpParam, dllPath, MAX_PATH);

    // Append the subfolder and executable name manually
    wcscat_s(dllPath, MAX_PATH, L"\\lib\\InfiniteNGPlusConsole.exe");

    LogMessage(L"Starting `InfiniteNGPlusConsole` at path: " + std::wstring(dllPath));

    // Path to your .NET console app is now in dllPath
    LPCWSTR exePath = dllPath;

    // Check if exePath exists.
    DWORD dwAttrib = GetFileAttributes(exePath);
    if (dwAttrib == INVALID_FILE_ATTRIBUTES || (dwAttrib & FILE_ATTRIBUTE_DIRECTORY))
    {
        MessageBoxW(NULL, L"`InfiniteNGPlusConsole` application not found!", L"Error", MB_ICONERROR | MB_OK);
        return 1;
    }

    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    SECURITY_ATTRIBUTES sa;

    // Set the security attributes struct to allow the pipe handles to be inherited
    ZeroMemory(&sa, sizeof(sa));
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);

    // We hide the console window of the child process.
    // It takes care of its own log file.
    si.dwFlags = STARTF_USESHOWWINDOW;
    si.wShowWindow = SW_HIDE;

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

        LogMessage(L"`InfiniteNGPlusConsole` started successfully. Waiting for it to finish...");

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
                    MessageBoxW(NULL, L"`InfiniteNGPlusConsole` stopped unexpectedly! See the log file.", L"Error", MB_ICONERROR | MB_OK);
                }
            }
        }
        CloseHandle(hProcess);
        hProcess = NULL;
    }
    else
    {
        LogMessage(L"Failed to start `InfiniteNGPlusConsole`.");
        // Failed to start the process
        MessageBoxW(NULL, L"Failed to start `InfiniteNGPlusConsole`.", L"Error", MB_ICONERROR | MB_OK);
    }

    return 0;
}

void TerminateConsoleApp_Hard()
{
    if (hProcess != NULL)
    {
        // Terminate process.
        TerminateProcess(hProcess, 0);

        // Close process handle
        CloseHandle(hProcess);
        hProcess = NULL;
    }
}


// Function to send the "exit" command to the console app via stdin
// Currently non-functional as no STDIN pipe is used
void TerminateConsoleApp_Soft()
{
    if (hProcess != NULL)
    {
        // Write "exit\n" to stdin to signal the console app to terminate
        const char* exitCommand = "exit\n";
        DWORD bytesWritten;
        WriteFile(hStdInWrite, exitCommand, (DWORD)strlen(exitCommand), &bytesWritten, NULL);

        // Close the write handle for stdin after sending the command
        CloseHandle(hStdInWrite);
        hStdInWrite = NULL;

        // Wait for the process to terminate after sending the command
        WaitForSingleObject(hProcess, INFINITE);
        CloseHandle(hProcess);
        hProcess = NULL;
    }
}

// Entry point for the DLL
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // Create a thread to start the console app
        LogMessage(L"Creating thread to start `InfiniteNGPlusConsole`...");
        hThread = CreateThread(NULL, 0, StartConsoleApp, hModule, 0, NULL);
        if (hThread)
        {
            CloseHandle(hThread); // We don't need the thread handle after it's started
        }
        break;

    case DLL_PROCESS_DETACH:
        // When detaching, terminate the console app if it's running
        LogMessage(L"Detaching DLL, terminating `InfiniteNGPlusConsole`...");
        TerminateConsoleApp_Hard();
        break;
    }
    return TRUE;
}
