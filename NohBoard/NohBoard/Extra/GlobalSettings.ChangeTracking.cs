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

namespace ThoNohT.NohBoard.Extra
{
    using System.Collections.Generic;
    using ThoNohT.NohBoard.Keyboard;

    public partial class GlobalSettings
    {
        private Stack<(KeyboardDefinition, KeyboardStyle, ChangeType)> undoHistory;

        private Stack<(KeyboardDefinition, KeyboardStyle, ChangeType)> redoHistory;

        #region Helper properties

        private (KeyboardDefinition, KeyboardStyle, ChangeType) LastUndo => this.undoHistory.Peek();

        private bool CanUndo => this.undoHistory.Count > 1;

        private bool CanRedo => this.redoHistory.Count > 0;

        #endregion Helper properties

        public void UpdateDefinition(KeyboardDefinition newDefinition, bool pushUndoHistory)
        {
            GlobalSettings.CurrentDefinition = newDefinition;
            if (pushUndoHistory) this.PushUndoHistory(ChangeType.Definition);
        }

        public void UpdateStyle(KeyboardStyle newStyle, bool pushUndoHistory)
        {
            GlobalSettings.CurrentStyle = newStyle;
            if (pushUndoHistory) this.PushUndoHistory(ChangeType.Style);
        }

        public void UpdateBoth(KeyboardDefinition newDefinition, KeyboardStyle newStyle, bool pushUndoHistory)
        {
            GlobalSettings.CurrentDefinition = newDefinition;
            GlobalSettings.CurrentStyle = newStyle;
            if (pushUndoHistory) this.PushUndoHistory(ChangeType.Both);
        }

        public void InitUndoHistory()
        {
            this.undoHistory = new Stack<(KeyboardDefinition, KeyboardStyle, ChangeType)>();
            this.redoHistory = new Stack<(KeyboardDefinition, KeyboardStyle, ChangeType)>();

            GlobalSettings.UnsavedDefinitionChanges = false;
            GlobalSettings.UnsavedStyleChanges = false;

            this.undoHistory.Push((GlobalSettings.CurrentDefinition, GlobalSettings.CurrentStyle.Clone(), ChangeType.None));
        }

        private void PushUndoHistory(ChangeType changeType)
        {
            var (lastDef, lastStyle, _) = this.LastUndo;

            if (!GlobalSettings.CurrentDefinition.IsChanged(lastDef) &&
                !GlobalSettings.CurrentStyle.IsChanged(lastStyle))
            {
                return;
            }

            this.undoHistory.Push((GlobalSettings.CurrentDefinition, GlobalSettings.CurrentStyle.Clone(), changeType));

            this.redoHistory.Clear();

            if ((changeType & ChangeType.Definition) > 0) GlobalSettings.UnsavedDefinitionChanges = true;
            if ((changeType & ChangeType.Style) > 0) GlobalSettings.UnsavedStyleChanges = true;
        }

        public bool Undo()
        {
            if (!this.CanUndo) return false;

            this.redoHistory.Push(this.undoHistory.Pop());

            var (defToRevertTo, styleToRevertTo, _) = this.LastUndo;

            GlobalSettings.CurrentDefinition = defToRevertTo;
            GlobalSettings.CurrentStyle = styleToRevertTo;

            if (!this.CanUndo)
            {
                GlobalSettings.UnsavedDefinitionChanges = false;
                GlobalSettings.UnsavedStyleChanges = false;
            }

            return true;
        }

        public bool Redo()
        {
            if (!this.CanRedo) return false;

            var (defToRevertTo, styleToRevertTo, typeToRevert) = this.redoHistory.Pop();

            this.undoHistory.Push((defToRevertTo, styleToRevertTo, typeToRevert));

            GlobalSettings.CurrentDefinition = defToRevertTo;
            GlobalSettings.CurrentStyle = styleToRevertTo;

            GlobalSettings.UnsavedDefinitionChanges |= (typeToRevert & ChangeType.Definition) != 0;
            GlobalSettings.UnsavedStyleChanges |= (typeToRevert & ChangeType.Style) != 0;

            return true;
        }
    }
}
