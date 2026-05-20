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
    using System.Collections;
    using System.Collections.Generic;

    public class CircleBuffer<T> : IEnumerable<T>
    {
        private readonly Queue<T> state;

        public CircleBuffer(int size, T defaultElem)
        {
            this.Size = size;
            this.state = new Queue<T>(size);
            for (var i = 0; i < size; i++)
                this.state.Enqueue(defaultElem);
        }

        public void Add(T elem)
        {
            lock (this.state)
            {
                this.state.Dequeue();
                this.state.Enqueue(elem);
            }
        }

        public int Size { get; }

        public IEnumerator<T> GetEnumerator()
        {
            lock (this.state)
            {
                foreach (var elem in this.state) yield return elem;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
