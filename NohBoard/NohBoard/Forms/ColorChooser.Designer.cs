namespace ThoNohT.NohBoard.Forms
{
    partial class ColorChooser
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.DisplayLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.DisplayLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DisplayLabel.AutoSize = false;
            this.DisplayLabel.AutoEllipsis = true;
            this.DisplayLabel.BackColor = System.Drawing.SystemColors.Control;
            this.DisplayLabel.Location = new System.Drawing.Point(40, 0);
            this.DisplayLabel.Name = "DisplayLabel";
            this.DisplayLabel.Size = new System.Drawing.Size(180, 23);
            this.DisplayLabel.TabIndex = 0;
            this.DisplayLabel.Text = "Pick a color";
            this.DisplayLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.DisplayLabel.DoubleClick += new System.EventHandler(this.ColorChooser_DoubleClick);
            this.DisplayLabel.Layout += new System.Windows.Forms.LayoutEventHandler(this.DisplayLabel_Layout);
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.DisplayLabel);
            this.Name = "ColorChooser";
            this.Size = new System.Drawing.Size(220, 23);
            this.DoubleClick += new System.EventHandler(this.ColorChooser_DoubleClick);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label DisplayLabel;
    }
}
