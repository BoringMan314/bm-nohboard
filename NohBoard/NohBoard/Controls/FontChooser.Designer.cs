namespace ThoNohT.NohBoard.Controls
{
    partial class FontChooser
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            DisplayLabel = new System.Windows.Forms.Label();
            lblLink = new System.Windows.Forms.Label();
            txtLink = new System.Windows.Forms.TextBox();
            SuspendLayout();
            DisplayLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            DisplayLabel.AutoEllipsis = true;
            DisplayLabel.BackColor = System.Drawing.SystemColors.Control;
            DisplayLabel.Location = new System.Drawing.Point(5, 0);
            DisplayLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            DisplayLabel.Name = "DisplayLabel";
            DisplayLabel.Size = new System.Drawing.Size(180, 50);
            DisplayLabel.TabIndex = 0;
            DisplayLabel.Text = "Pick a Font";
            DisplayLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            DisplayLabel.DoubleClick += FontChooser_DoubleClick;
            DisplayLabel.Layout += DisplayLabel_Layout;
            lblLink.Location = new System.Drawing.Point(5, 55);
            lblLink.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblLink.Name = "lblLink";
            lblLink.Size = new System.Drawing.Size(60, 23);
            lblLink.TabIndex = 1;
            lblLink.Text = "Link:";
            lblLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            txtLink.Location = new System.Drawing.Point(70, 55);
            txtLink.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtLink.Name = "txtLink";
            txtLink.Size = new System.Drawing.Size(115, 23);
            txtLink.TabIndex = 2;
            txtLink.TextChanged += txtLink_TextChanged;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            Controls.Add(txtLink);
            Controls.Add(lblLink);
            Controls.Add(DisplayLabel);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "FontChooser";
            Size = new System.Drawing.Size(190, 85);
            DoubleClick += FontChooser_DoubleClick;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label DisplayLabel;
        private System.Windows.Forms.Label lblLink;
        private System.Windows.Forms.TextBox txtLink;
    }
}
