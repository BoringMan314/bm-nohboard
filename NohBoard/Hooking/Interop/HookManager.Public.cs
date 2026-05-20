/*
Copyright (C) 2016 by Eric Bataille <e.c.p.bataille@gmail.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace ThoNohT.NohBoard.Hooking.Interop
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using static Defines;
    using static FunctionImports;

    public static partial class HookManager
    {
        #region Properties

        public static bool TrapMouse { get; set; }

        public static bool TrapKeyboard { get; set; }

        public static Func<int, bool> KeyboardInsert = null;

	    public static int TrapToggleKeyCode { get; set; } = VK_SCROLL;

        public static int ScrollHold { get; set; } = 50;

        public static int PressHold { get; set; } = 0;

        private static int modalUiNestCount;

        #endregion Properties

        #region Methods

        public static void EnterModalUiScope() => modalUiNestCount++;

        public static void ExitModalUiScope()
        {
            if (modalUiNestCount > 0)
                modalUiNestCount--;
        }

        public static bool IsModalUiActive => modalUiNestCount > 0;

        public static void RunModalUi(Action action) => RunModalUi(() => { action(); return 0; });

        public static T RunModalUi<T>(Func<T> func)
        {
            EnterModalUiScope();
            try
            {
                return func();
            }
            finally
            {
                ExitModalUiScope();
            }
        }

        public static void EnableMouseHook()
        {
            if (mouseHookHandle != 0) return;

            mouseDelegate = MouseHookProc;
            mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);

            if (mouseHookHandle != 0) return;

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public static void DisableMouseHook()
        {
            if (mouseHookHandle == 0) return;

            var result = UnhookWindowsHookEx(mouseHookHandle);
            mouseHookHandle = 0;
            mouseDelegate = null;

            if (result != 0) return;

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public static void EnableKeyboardHook()
        {
            if (keyboardHookHandle != 0) return;

            keyboardDelegate = KeyboardHookProc;
            keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardDelegate, IntPtr.Zero, 0);

            if (keyboardHookHandle != 0) return;

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public static void DisableKeyboardHook()
        {
            if (keyboardHookHandle == 0) return;

            var result = UnhookWindowsHookEx(keyboardHookHandle);
            keyboardHookHandle = 0;
            keyboardDelegate = null;

            if (result != 0) return;

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        #endregion Methods
    }
}
