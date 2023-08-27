using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace SoulsFormats;

public static class Kernel32 {
    [DllImport("kernel32", SetLastError = true)]
    static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
        
    [DllImport("kernel32.dll")]
    public static extern int GetLastError();
    public static IntPtr LoadLibrary(string path) {
        return LoadLibraryW(path);
    }
}