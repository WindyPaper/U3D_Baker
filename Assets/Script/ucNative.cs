/* 
 * Native dll invocation helper by Francis R. Griffiths-Keam
 * www.runningdimensions.com/blog
 */

using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ucNative
{
    public static T Invoke<T, T2>(IntPtr library, params object[] pars)
    {
        IntPtr funcPtr = GetProcAddress(library, typeof(T2).Name);
        if (funcPtr == IntPtr.Zero)
        {
            Debug.LogWarning("Could not gain reference to method address. Function name = " + typeof(T2).Name);
            //return default(T);
        }

        var func = Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, typeof(T2).Name), typeof(T2));
        return (T)func.DynamicInvoke(pars);
    }

    public static void Invoke_Void<T2>(IntPtr library, params object[] pars)
    {
        IntPtr funcPtr = GetProcAddress(library, typeof(T2).Name);
        if (funcPtr == IntPtr.Zero)
        {
            Debug.LogWarning("Could not gain reference to method address. Function name = " + typeof(T2).Name);
            //return default(T);
        }

        var func = Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, typeof(T2).Name), typeof(T2));
        func.DynamicInvoke(pars);
    }

    public static void Invoke<T>(IntPtr library, params object[] pars)
    {
        IntPtr funcPtr = GetProcAddress(library, typeof(T).Name);
        if (funcPtr == IntPtr.Zero)
        {
            Debug.LogWarning("Could not gain reference to method address.");
            return;
        }

        var func = Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(T));
        func.DynamicInvoke(pars);
    }

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandleA(string lpFileName);


    [DllImport("kernel32", SetLastError = true)]
    public static extern void FreeLibraryAndExitThread(IntPtr hModule, int code);

    [System.Flags]
    public enum LoadLibraryFlags : uint
    {
        DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
        LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
        LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
        LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
        LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
        LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008,
        LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
        LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
        LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000
    }

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int AddDllDirectory(string NewDirectory);

    [DllImport("kernel32")]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32.dll")]
    private static extern int FormatMessage(int dwFlags,
    IntPtr lpSource, int dwMessageId, int dwLanguageId,
    out string lpBuffer, int nSize, IntPtr pArguments);

    public static string GetErrorMessage(int errorCode)
    {
        const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        string lpMsgBuf;
        int dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER
            | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;

        int retVal = FormatMessage(dwFlags, IntPtr.Zero, errorCode, 0,
                                    out lpMsgBuf, 0, IntPtr.Zero);
        if (0 == retVal)
        {
            return null;
        }
        return lpMsgBuf;
    }
   
}