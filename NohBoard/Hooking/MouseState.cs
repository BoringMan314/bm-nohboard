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

namespace ThoNohT.NohBoard.Hooking
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using Interop;
    using static Interop.Defines;
    using static Interop.FunctionImports;

    public class MouseState : StateBase<MouseKeyCode>
    {
        #region Configuration

        private static int updateInterval = 33;

        private static int mouseSmooth = 5;

        private static bool mouseFromCenter = false;

        #endregion Configuration

        #region State

        private static List<(Rectangle screen, Point center)> ScreenCenters = new List<(Rectangle, Point)>();

        private static readonly CircleBuffer<SizeF> speedHistory = new CircleBuffer<SizeF>(mouseSmooth, default(SizeF));

        private static Point lastLocation;

        private static int? lastLocationUpdate;

        private static readonly long[] scrollTimers = new long[4];

        private static readonly int[] scrollCounts = new int[4];

        private static Stopwatch scrollStopwatch;

        #endregion State

        #region Properties

        public static IReadOnlyList<int> ScrollCounts
        {
            get { lock (scrollCounts) return scrollCounts.ToList().AsReadOnly(); }
        }

        public static SizeF AverageSpeed
        {
            get
            {
                var sum = speedHistory.Aggregate((acc, elem) => acc + elem);
                return new SizeF(sum.Width / speedHistory.Size, sum.Height / speedHistory.Size);
            }
        }

        #endregion Properties

        public static void SetMouseFromCenter(bool activate, List<(Rectangle screen, Point center)> screenCenters)
        {
            mouseFromCenter = activate;

            if (activate) ScreenCenters = screenCenters;
        }

        public static void CheckKeys(int hold)
        {
            lock (pressedKeys)
            {
                if (!pressedKeys.Any()) return;

                var time = keyHoldStopwatch.ElapsedMilliseconds;

                foreach (var key in pressedKeys.Where(t => KeyIsUp(t.Key)).Select(t => t.Key).ToList())
                {
                    var pressed = pressedKeys[key];

                    if (pressed.startTime + hold < time)
                    {
                        pressedKeys.Remove(key);
                    }
                    else
                    {
                        pressed.removed = true;
                        pressedKeys[key] = pressed;
                    }

                    updated = true;
                }

                TryStopStopwatch();
            }
        }

        public static void CheckScrollAndMovement()
        {
            if (!speedHistory.All(x => x.IsEmpty))
            {
                speedHistory.Add(new SizeF(0, 0));
                updated = true;
            }

            if (scrollStopwatch == null) return;

            lock (scrollCounts)
            {
                var stillActiveKeys = 0;
                for (var i = 0; i < 4; i++)
                {
                    if (scrollTimers[i] < scrollStopwatch.ElapsedMilliseconds)
                    {
                        scrollCounts[i] = 0;
                        updated = true;
                    }
                    else
                    {
                        stillActiveKeys++;
                    }
                }

                if (stillActiveKeys == 0)
                {
                    scrollStopwatch.Stop();
                    scrollStopwatch = null;
                }
            }
        }

        public static void AddScrollDirection(MouseScrollKeyCode keyCode)
        {
            lock (scrollCounts)
            {
                if (scrollStopwatch == null) scrollStopwatch = Stopwatch.StartNew();

                scrollCounts[(int)keyCode] += 1;
                scrollTimers[(int)keyCode] = scrollStopwatch.ElapsedMilliseconds + HookManager.ScrollHold;
                updated = true;
            }
        }

        public static void AddPressedElement(MouseKeyCode keyCode)
        {
            lock (pressedKeys)
            {
                EnsureStopwatchRunning();

                var time = keyHoldStopwatch.ElapsedMilliseconds;

                if (pressedKeys.TryGetValue(keyCode, out var pressed))
                {
                    pressed.startTime = time;
                    pressed.removed = false;
                    pressedKeys[keyCode] = pressed;
                }
                else
                {
                    pressedKeys.Add(
                        keyCode,
                        new KeyPress
                        {
                            startTime = keyHoldStopwatch.ElapsedMilliseconds,
                            removed = false
                        });

                    updated = true;
                }

            }
        }

        public static void RemovePressedElement(MouseKeyCode keyCode, int hold)
        {
            lock (pressedKeys)
            {
                if (!pressedKeys.ContainsKey(keyCode)) return;

                var time = keyHoldStopwatch.ElapsedMilliseconds;

                var pressed = pressedKeys[keyCode];

                if (pressed.startTime + hold < time)
                {
                    pressedKeys.Remove(keyCode);
                }
                else
                {
                    pressed.removed = true;
                    pressedKeys[keyCode] = pressed;
                }

                updated = true;

                TryStopStopwatch();
            }
        }

        public static void RegisterLocation(Point location, int time)
        {
            if (lastLocationUpdate == null)
            {
                lastLocation = mouseFromCenter ? GetScreenCenterForPoint(location) ?? location : location;
                lastLocationUpdate = time;
                return;
            }

            if (time - lastLocationUpdate.Value < updateInterval) return;

            speedHistory.Add(GetSpeed(location, lastLocation, time - lastLocationUpdate.Value));

            lastLocationUpdate = time;
            lastLocation = mouseFromCenter ? GetScreenCenterForPoint(location) ?? location : location;

            updated = true;
        }

        private static Point? GetScreenCenterForPoint(Point point)
        {
            Func<Rectangle, Point, bool> contains =
                (r, p) => p.X >= r.Left && p.X <= r.Right && p.Y >= r.Top && p.Y <= r.Bottom;

            var result = ScreenCenters.Where(t => contains(t.screen, point))
                .Select(t => (Point?)t.center).FirstOrDefault();

            return result;
        }

        private static SizeF GetSpeed(Point target, Point source, int dt)
        {
            var dx = target.X - source.X;
            var dy = target.Y - source.Y;

            return new SizeF((float)dx / dt, (float)dy / dt);
        }

        private static bool KeyIsUp(MouseKeyCode key)
        {
            switch (key)
            {
                case MouseKeyCode.LeftButton:
                    return GetKeyState(VK_LBUTTON) >= 0;

                case MouseKeyCode.RightButton:
                    return GetKeyState(VK_RBUTTON) >= 0;

                case MouseKeyCode.MiddleButton:
                    return GetKeyState(VK_MBUTTON) >= 0;

                case MouseKeyCode.X1Button:
                    return GetKeyState(VK_XBUTTON1) >= 0;

                case MouseKeyCode.X2Button:
                    return GetKeyState(VK_XBUTTON2) >= 0;
            }

            return false;
        }
    }
}
