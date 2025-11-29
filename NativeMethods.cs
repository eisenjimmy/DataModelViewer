using System.Runtime.InteropServices;

namespace SchemaDiagramViewer;

// P/Invoke declarations for hiding console window
internal static class NativeMethods
{
    [DllImport("kernel32.dll")]
    internal static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}

