using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using WinRT;

namespace UntamedMusicPlayer.Helpers;

public static partial class CursorHelper
{
    extension(UIElement element)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ProtectedCursor")]
        public extern void SetProtectedCursor(InputCursor? value);
    }

    public static InputCursor? LoadCursor(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var hcursor = LoadCursorFromFileW(filePath);
        if (hcursor == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        return CreateCursorFromHCURSOR(hcursor);
    }

    private static InputCursor? CreateCursorFromHCURSOR(nint hcursor)
    {
        if (hcursor == 0)
        {
            return null;
        }

        const string classId = "Microsoft.UI.Input.InputCursor";
        _ = WindowsCreateString(classId, (uint)classId.Length, out var hs);
        _ = RoGetActivationFactory(hs, typeof(IActivationFactory).GUID, out var fac);
        _ = WindowsDeleteString(hs);
        if (fac is not IInputCursorStaticsInterop interop)
        {
            return null;
        }

        interop.CreateFromHCursor(hcursor, out var cursorAbi);
        if (cursorAbi == 0)
        {
            return null;
        }

        return MarshalInspectable<InputCursor>.FromAbi(cursorAbi);
    }

    [
        GeneratedComInterface,
        Guid("ac6f5065-90c4-46ce-beb7-05e138e54117"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    internal partial interface IInputCursorStaticsInterop
    {
        // IInspectable unused methods
        void GetIids();
        void GetRuntimeClassName();
        void GetTrustLevel();

        [PreserveSig]
        int CreateFromHCursor(nint hcursor, out nint inputCursor);
    }

    [
        GeneratedComInterface,
        Guid("00000035-0000-0000-c000-000000000046"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    internal partial interface IActivationFactory
    {
        // IInspectable unused methods
        void GetIids();
        void GetRuntimeClassName();
        void GetTrustLevel();

        [PreserveSig]
        int ActivateInstance(out nint instance);
    }

    [LibraryImport("api-ms-win-core-winrt-l1-1-0.dll")]
    private static partial int RoGetActivationFactory(
        nint runtimeClassId,
        in Guid iid,
        out IActivationFactory factory
    );

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint LoadCursorFromFileW(string name);

    [LibraryImport(
        "api-ms-win-core-winrt-string-l1-1-0.dll",
        StringMarshalling = StringMarshalling.Utf16
    )]
    private static partial int WindowsCreateString(
        string? sourceString,
        uint length,
        out nint @string
    );

    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    private static partial int WindowsDeleteString(nint @string);
}
