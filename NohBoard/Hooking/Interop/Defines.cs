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
    public static class Defines
    {
        #region Hooks

        public const int WH_MOUSE_LL = 14;

        public const int WH_KEYBOARD_LL = 13;

        #endregion Hooks

        #region Mouse messages

        public const int WM_MOUSEMOVE = 0x200;

        public const int WM_LBUTTONDOWN = 0x201;

        public const int WM_RBUTTONDOWN = 0x204;

        public const int WM_MBUTTONDOWN = 0x207;

        public const int WM_LBUTTONUP = 0x202;

        public const int WM_RBUTTONUP = 0x205;

        public const int WM_MBUTTONUP = 0x208;

        public const int WM_MOUSEWHEEL = 0x020A;

        public const int WM_MOUSEHWHEEL = 0x020E;

        public const int WM_XBUTTONDOWN = 0x020B;

        public const int WM_XBUTTONUP = 0x020C;

        #endregion Mouse messages

        #region Keyboard messages

        public const int WM_KEYDOWN = 0x100;

        public const int WM_KEYUP = 0x101;

        public const int WM_SYSKEYDOWN = 0x104;

        public const int WM_SYSKEYUP = 0x105;

        #endregion Keyboard messages

        #region Key codes

        #region Mouse

        public const byte VK_LBUTTON = 0x1;

        public const byte VK_RBUTTON = 0x2;

        public const byte VK_MBUTTON = 0x04;

        public const byte VK_XBUTTON1 = 0x05;

        public const byte VK_XBUTTON2 = 0x06;

        public const byte XBUTTON1 = 0x1;

        public const byte XBUTTON2 = 0x2;

        #endregion Mouse

        #region Keyboard

        public const byte VK_SHIFT = 0x10;

        public const byte VK_CAPITAL = 0x14;

        public const byte VK_NUMLOCK = 0x90;

        public const byte VK_SCROLL = 0x91;

        public const byte VK_RETURN = 0XD;

        public const byte VK_LSHIFT = 0XA0;

        public const byte VK_RSHIFT = 0XA1;

        public const byte VK_LCTRL = 0XA2;

        public const byte VK_RCTRL = 0XA3;

        public const byte VK_LALT = 0XA4;

        public const byte VK_RALT = 0XA5;

        #endregion Keyboard

        #endregion Key codes

        #region Flags

        public const int LLKHF_EXTENDED = KF_EXTENDED >> 8;

        public const int KF_EXTENDED = 0x0100;

        #endregion Flags
    }
}
