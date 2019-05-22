'ref
'<http://www.vbforums.com/showthread.php?322791-flash-icon-in-taskbar-resolved>
'<https://social.msdn.microsoft.com/Forums/vstudio/en-US/689b2422-bf55-48ce-b1af-acf48217153f/flash-window-in-wpf?forum=wpf>
'<https://stackoverflow.com/questions/5118226/how-to-make-a-wpf-window-to-blink-on-the-taskbar>


Public Class FlashWindow
    '<DllImport("user32.dll")>
    '<MarshalAs(UnmanagedType.Bool)>
    'Private Function FlashWindowEx(ByRef pwfi As FLASHWINFO) As Boolean

    Private Declare Function FlashWindowEx Lib "user32" (ByRef pfwi As FLASHWINFO) As Boolean

    '<StructLayout(LayoutKind.Sequential)>
    Private Structure FLASHWINFO
        Public cbSize As UInteger
        Public hwnd As IntPtr
        Public dwFlags As UInteger
        Public uCount As UInteger
        Public dwTimeout As UInteger
    End Structure

    Public Const FLASHW_STOP As UInteger = 0
    Public Const FLASHW_CAPTION As UInteger = 1
    Public Const FLASHW_TRAY As UInteger = 2
    Public Const FLASHW_ALL As UInteger = 3
    Public Const FLASHW_TIMER As UInteger = 4
    Public Const FLASHW_TIMERNOFG As UInteger = 12

    Public Shared Function Flash(ByVal hwnd As IntPtr) As Boolean
        If Win2000OrLater Then
            Dim fi As FLASHWINFO = Create_FLASHWINFO(hwnd, FLASHW_ALL Or FLASHW_TIMERNOFG, UInteger.MaxValue, 0)
            Return FlashWindowEx(fi)
        End If

        Return False
    End Function

    Private Shared Function Create_FLASHWINFO(ByVal handle As IntPtr, ByVal flags As UInteger, ByVal count As UInteger, ByVal timeout As UInteger) As FLASHWINFO
        Dim fi As FLASHWINFO = New FLASHWINFO()
        'fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi))
        fi.cbSize = Convert.ToUInt32(System.Runtime.InteropServices.Marshal.SizeOf(fi))
        fi.hwnd = handle
        fi.dwFlags = flags
        fi.uCount = count
        fi.dwTimeout = timeout
        Return fi
    End Function

    Public Shared Function Flash(ByVal hwnd As IntPtr, ByVal count As UInteger) As Boolean
        If Win2000OrLater Then
            Dim fi As FLASHWINFO = Create_FLASHWINFO(hwnd, FLASHW_ALL, count, 0)
            Return FlashWindowEx(fi)
        End If

        Return False
    End Function

    Public Shared Function Start(ByVal win As System.Windows.Window) As Boolean
        If Win2000OrLater Then
            '<https://social.msdn.microsoft.com/Forums/en-US/5f89ac58-d2ef-4ac0-aefb-b2826dbef48a/when-the-window-handle-is-available-in-wpf?forum=wpf>
            Dim handle = New System.Windows.Interop.WindowInteropHelper(win).EnsureHandle()
            Dim fi As FLASHWINFO = Create_FLASHWINFO(handle, FLASHW_ALL, UInteger.MaxValue, 0)
            Return FlashWindowEx(fi)
        End If

        Return False
    End Function

    Public Shared Function [Stop](ByVal win As System.Windows.Window) As Boolean
        If Win2000OrLater Then
            Dim handle = New System.Windows.Interop.WindowInteropHelper(win).EnsureHandle()
            Dim fi As FLASHWINFO = Create_FLASHWINFO(handle, FLASHW_STOP, UInteger.MaxValue, 0)
            Return FlashWindowEx(fi)
        End If

        Return False
    End Function

    Private Shared ReadOnly Property Win2000OrLater As Boolean
        Get
            Return System.Environment.OSVersion.Version.Major >= 5
        End Get
    End Property

End Class
