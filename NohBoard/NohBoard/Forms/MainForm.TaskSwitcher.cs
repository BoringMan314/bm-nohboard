/*
Copyright (C) 2016 by Eric Bataille <e.c.p.bataille@gmail.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.
*/

namespace ThoNohT.NohBoard.Forms
{
    using System;
    using System.Runtime.InteropServices;

    public partial class MainForm
    {
        private const int DwmwaForceIconicRepresentation = 7;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            ref int pvAttribute,
            int cbAttribute);

        private void ApplyTaskSwitcherIconicPreviewPreference()
        {
            if (!this.IsHandleCreated || this.Handle == IntPtr.Zero)
                return;

            try
            {
                var enabled = 1;
                DwmSetWindowAttribute(
                    this.Handle,
                    DwmwaForceIconicRepresentation,
                    ref enabled,
                    Marshal.SizeOf(typeof(int)));
            }
            catch
            {
            }
        }
    }
}
