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
    using System.Runtime.InteropServices;
    using static Defines;
    using static FunctionImports;
    using static Structs;

    public static partial class HookManager
    {
        internal delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        private static bool trapEnabled;

        #region Mouse Hook

        private static HookProc mouseDelegate;

        private static int mouseHookHandle;

        private static int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode < 0) return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);

            var info = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

            ushort subCode;
            switch (wParam)
            {
                case WM_LBUTTONDOWN:
                    MouseState.AddPressedElement(MouseKeyCode.LeftButton);

                    break;
                case WM_LBUTTONUP:
                    MouseState.RemovePressedElement(MouseKeyCode.LeftButton, PressHold);

                    break;
                case WM_RBUTTONDOWN:
                    MouseState.AddPressedElement(MouseKeyCode.RightButton);

                    break;
                case WM_RBUTTONUP:
                    MouseState.RemovePressedElement(MouseKeyCode.RightButton, PressHold);
                    break;

                case WM_MBUTTONDOWN:
                    MouseState.AddPressedElement(MouseKeyCode.MiddleButton);
                    break;

                case WM_MBUTTONUP:
                    MouseState.RemovePressedElement(MouseKeyCode.MiddleButton, PressHold);
                    break;

                case WM_MOUSEWHEEL:
                    subCode = HiWord(info.MouseData);

                    if (subCode == 120)
                        MouseState.AddScrollDirection(MouseScrollKeyCode.ScrollUp);
                    if (subCode == 65416)
                        MouseState.AddScrollDirection(MouseScrollKeyCode.ScrollDown);

                    break;
                case WM_MOUSEHWHEEL:
                    subCode = HiWord(info.MouseData);

                    if (subCode == 120)
                        MouseState.AddScrollDirection(MouseScrollKeyCode.ScrollRight);
                    if (subCode == 65416)
                        MouseState.AddScrollDirection(MouseScrollKeyCode.ScrollLeft);

                    break;
                case WM_XBUTTONDOWN:
                    subCode = HiWord(info.MouseData);

                    if (subCode == XBUTTON1)
                        MouseState.AddPressedElement(MouseKeyCode.X1Button);
                    if (subCode == XBUTTON2)
                        MouseState.AddPressedElement(MouseKeyCode.X2Button);

                    break;
                case WM_XBUTTONUP:
                    subCode = HiWord(info.MouseData);

                    if (subCode == XBUTTON1)
                        MouseState.RemovePressedElement(MouseKeyCode.X1Button, PressHold);
                    if (subCode == XBUTTON2)
                        MouseState.RemovePressedElement(MouseKeyCode.X2Button, PressHold);

                    break;
                case WM_MOUSEMOVE:
                    MouseState.RegisterLocation(new System.Drawing.Point(info.Point.X, info.Point.Y), info.Time);

                    break;
            }

            if (modalUiNestCount == 0 && trapEnabled && TrapMouse)
                return 1;

            return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
        }

        #endregion Mouse Hook

        #region Keyboard Hook

        private static HookProc keyboardDelegate;

        private static int keyboardHookHandle;

        private static int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode < 0) return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);

            var info = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

            var extended = (info.Flags & LLKHF_EXTENDED) != 0;
            var code = extended && info.VirtualKeyCode == VK_RETURN ? 1025 : info.VirtualKeyCode;

            switch (wParam)
            {
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                    KeyboardState.AddPressedElement(code, PressHold);
                    break;
                case WM_KEYUP:
                case WM_SYSKEYUP:
                    KeyboardState.RemovePressedElement(code, PressHold);

                    if (code == TrapToggleKeyCode) trapEnabled = !trapEnabled;
                    break;
            }

            if (KeyboardInsert?.Invoke(code) ?? false
                || (modalUiNestCount == 0 && trapEnabled && TrapKeyboard))
                return 1;

            return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        #endregion keyboard Hook
    }
}
