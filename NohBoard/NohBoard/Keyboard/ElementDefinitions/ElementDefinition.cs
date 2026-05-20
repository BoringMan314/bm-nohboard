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

namespace ThoNohT.NohBoard.Keyboard.ElementDefinitions
{
    using System;
    using System.Drawing;
    using System.Runtime.Serialization;

    [DataContract(Name = "ElementDefinition", Namespace = "")]
    [KnownType(typeof(KeyDefinition))]
    [KnownType(typeof(MouseSpeedIndicatorDefinition))]
    public abstract class ElementDefinition : IEquatable<ElementDefinition>
    {
        protected int StyleVersion = 0;

        protected ElementManipulation CurrentManipulation;

        protected ElementManipulation PreviewManipulation;

        public ElementManipulation RelevantManipulation => this.PreviewManipulation ?? this.CurrentManipulation;

        [DataMember]
        public int Id { get; protected set; }

        public abstract Rectangle GetBoundingBox();

        public abstract ElementDefinition Translate(int dx, int dy);

        public abstract bool Inside(Point point);

        public abstract bool StartManipulating(Point point, bool altDown, bool preview = false, bool translateOnly = false);
        
        public abstract void RenderEditing(Graphics g);

        public abstract void RenderHighlight(Graphics g);

        public abstract void RenderSelected(Graphics g);

        public abstract ElementDefinition Manipulate(Size diff);

        public abstract ElementDefinition Clone();

        protected void SetManipulation(ElementManipulation manipulation, bool preview)
        {
            if (preview) this.PreviewManipulation = manipulation;
            else
            {
                this.CurrentManipulation = manipulation;
                this.PreviewManipulation = null;
            }
        }

        #region Equality

        public bool Equals(ElementDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return this.Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((ElementDefinition) obj);
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        #endregion Equality

        public ElementDefinition SetId(int newId)
        {
            var newElement = this.Clone();
            newElement.Id = newId;
            return newElement;
        }

        public abstract bool IsChanged(ElementDefinition other);
    }
}
