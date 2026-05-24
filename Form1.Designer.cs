
namespace FolderIcon
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.finishBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.previewBox = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.folderPathBox = new System.Windows.Forms.TextBox();
            this.addFolderBtn = new System.Windows.Forms.Button();
            this.ClearBtn = new System.Windows.Forms.Button();
            this.restoreButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.previewBox)).BeginInit();
            this.SuspendLayout();
            // 
            // finishBtn
            // 
            this.finishBtn.Font = new System.Drawing.Font("Microsoft YaHei", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.finishBtn.Location = new System.Drawing.Point(122, 561);
            this.finishBtn.Name = "finishBtn";
            this.finishBtn.Size = new System.Drawing.Size(285, 49);
            this.finishBtn.TabIndex = 0;
            this.finishBtn.Text = "Finish";
            this.finishBtn.UseVisualStyleBackColor = true;
            this.finishBtn.Click += new System.EventHandler(this.finishBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(87, 173);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(353, 29);
            this.label1.TabIndex = 1;
            this.label1.Text = "Drag and drop your image here:";
            // 
            // previewBox
            // 
            this.previewBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.previewBox.Location = new System.Drawing.Point(35, 215);
            this.previewBox.Name = "previewBox";
            this.previewBox.Size = new System.Drawing.Size(459, 326);
            this.previewBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.previewBox.TabIndex = 2;
            this.previewBox.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Calibri", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(35, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 19);
            this.label2.TabIndex = 3;
            this.label2.Text = "Select Folder:";
            // 
            // folderPathBox
            // 
            this.folderPathBox.AllowDrop = true;
            this.folderPathBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.folderPathBox.Location = new System.Drawing.Point(35, 57);
            this.folderPathBox.Multiline = true;
            this.folderPathBox.Name = "folderPathBox";
            this.folderPathBox.Size = new System.Drawing.Size(458, 103);
            this.folderPathBox.TabIndex = 4;
            this.folderPathBox.TextChanged += new System.EventHandler(this.folderPathBox_TextChanged);
            this.folderPathBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.folderPathBox_DragDrop);
            this.folderPathBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.folderPathBox_DragEnter);
            this.folderPathBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.folderPathBox_MouseDoubleClick);
            // 
            // addFolderBtn
            // 
            this.addFolderBtn.Location = new System.Drawing.Point(382, 13);
            this.addFolderBtn.Name = "addFolderBtn";
            this.addFolderBtn.Size = new System.Drawing.Size(111, 35);
            this.addFolderBtn.TabIndex = 5;
            this.addFolderBtn.Text = "Add";
            this.addFolderBtn.UseVisualStyleBackColor = true;
            this.addFolderBtn.Click += new System.EventHandler(this.addFolderBtn_Click);
            // 
            // ClearBtn
            // 
            this.ClearBtn.Location = new System.Drawing.Point(261, 13);
            this.ClearBtn.Name = "ClearBtn";
            this.ClearBtn.Size = new System.Drawing.Size(111, 35);
            this.ClearBtn.TabIndex = 6;
            this.ClearBtn.Text = "Clear";
            this.ClearBtn.UseVisualStyleBackColor = true;
            this.ClearBtn.Click += new System.EventHandler(this.ClearBtn_Click);
            // 
            // restoreButton
            // 
            this.restoreButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.restoreButton.Font = new System.Drawing.Font("Microsoft YaHei", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.restoreButton.Location = new System.Drawing.Point(34, 561);
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size(82, 49);
            this.restoreButton.TabIndex = 7;
            this.restoreButton.Text = "Restore";
            this.restoreButton.UseVisualStyleBackColor = false;
            this.restoreButton.Click += new System.EventHandler(this.restoreButton_Click);
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(519, 627);
            this.Controls.Add(this.restoreButton);
            this.Controls.Add(this.ClearBtn);
            this.Controls.Add(this.addFolderBtn);
            this.Controls.Add(this.folderPathBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.previewBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.finishBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "Form1";
            this.Text = "FolderIcon";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.previewBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button finishBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox previewBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox folderPathBox;
        private System.Windows.Forms.Button addFolderBtn;
        private System.Windows.Forms.Button ClearBtn;
        private System.Windows.Forms.Button restoreButton;
    }
}

