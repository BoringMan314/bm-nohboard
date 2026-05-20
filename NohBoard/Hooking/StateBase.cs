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
    using System.Diagnostics;
    using System.Linq;

    public class StateBase<T>
    {
        protected struct KeyPress
        {
            public long startTime { get; set; }

            public bool removed { get; set; }
        }

        protected static readonly Dictionary<T, KeyPress> pressedKeys = new Dictionary<T, KeyPress>();

        protected static bool updated;

        protected static Stopwatch keyHoldStopwatch;

        public static bool Updated
        {
            get
            {
                if (!updated) return false;

                updated = false;
                return true;
            }
        }

        public static IReadOnlyList<T> PressedKeys
        {
            get { lock (pressedKeys) return pressedKeys.Keys.ToList().AsReadOnly(); }
        }

        public static void CheckKeyHolds(int hold)
        {
            lock (pressedKeys)
            {
                if (!pressedKeys.Where(t => t.Value.removed).Any()) return;

                updated = true;

                var time = keyHoldStopwatch.ElapsedMilliseconds;

                foreach (var key in pressedKeys
                    .Where(t => t.Value.removed)
                    .Where(t => t.Value.startTime + hold < time)
                    .Select(t => t.Key).ToList())
                {
                    pressedKeys.Remove(key);
                }

                TryStopStopwatch();
            }
        }

        protected static void EnsureStopwatchRunning()
        {
            if (keyHoldStopwatch is null || !keyHoldStopwatch.IsRunning)
                keyHoldStopwatch = Stopwatch.StartNew();
        }

        protected static void TryStopStopwatch()
        {
            if (!pressedKeys.Any())
            {
                keyHoldStopwatch.Stop();
            }
        }
    }
}
