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
    using System.Collections.Generic;
    using System.Linq;
    using static Interop.Defines;
    using static Interop.FunctionImports;

    public class KeyboardState : StateBase<int>
    {
        private static Dictionary<int, int> StateKeys = new Dictionary<int, int>
        {
            { VK_CAPITAL, 1026 },
            { VK_NUMLOCK, 1027 },
            { VK_SCROLL, 1028 }
        };

        static KeyboardState()
        {
            foreach (var key in StateKeys)
            {
                if (CheckStateKey(key.Key)) AddPressedElement(key.Value, 0);
            }
        }

        public static bool ShiftDown
        {
            get { lock (pressedKeys) return pressedKeys.ContainsKey(VK_LSHIFT) || pressedKeys.ContainsKey(VK_RSHIFT); }
        }

        public static bool CtrlDown
        {
            get { lock (pressedKeys) return pressedKeys.ContainsKey(VK_LCTRL) || pressedKeys.ContainsKey(VK_RCTRL); }
        }

        public static bool AltDown
        {
            get { lock (pressedKeys) return pressedKeys.ContainsKey(VK_LALT) || pressedKeys.ContainsKey(VK_RALT); }
        }

        public static bool CapsActive => CheckStateKey(VK_CAPITAL);

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

        public static void AddPressedElement(int keyCode, int hold)
        {
            lock (pressedKeys)
            {
                EnsureStopwatchRunning();

                var time = keyHoldStopwatch.ElapsedMilliseconds;

                TryToggleStateKey(keyCode, hold);

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

        private static void TryToggleStateKey(int keyCode, int hold)
        {
            if (!StateKeys.TryGetValue(keyCode, out var stateKey)) return;

            if (!CheckStateKey(keyCode))
            {
                AddPressedElement(stateKey, hold);
            }
            else
            {
                RemovePressedElement(stateKey, hold);
            }
        }

        public static void RemovePressedElement(int keyCode, int hold)
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

        private static bool CheckStateKey(int keyCode)
        {
            return (GetKeyState(keyCode) & 0x1) != 0;
        }

        private static bool KeyIsUp(int keyCode)
        {
            if (StateKeys.Values.Contains(keyCode))
            {
                var actualCode = StateKeys.Single(k => k.Value == keyCode).Key;
                return !CheckStateKey(actualCode);
            }

            return GetKeyState(keyCode) >= 0;
        }
    }
}
