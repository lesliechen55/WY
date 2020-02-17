namespace EmailWY
{
    partial class FrmWY
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
            this.BrEmail = new System.Windows.Forms.WebBrowser();
            this.RtxtLogger = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // BrEmail
            // 
            this.BrEmail.Location = new System.Drawing.Point(12, 48);
            this.BrEmail.MinimumSize = new System.Drawing.Size(20, 20);
            this.BrEmail.Name = "BrEmail";
            this.BrEmail.Size = new System.Drawing.Size(1252, 750);
            this.BrEmail.TabIndex = 0;
            this.BrEmail.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.BrEmail_DocumentCompleted);
            this.BrEmail.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.BrEmail_Navigated);
            this.BrEmail.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.BrEmail_Navigating);
            // 
            // RtxtLogger
            // 
            this.RtxtLogger.Location = new System.Drawing.Point(1270, 5);
            this.RtxtLogger.Name = "RtxtLogger";
            this.RtxtLogger.Size = new System.Drawing.Size(301, 793);
            this.RtxtLogger.TabIndex = 1;
            this.RtxtLogger.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(26, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            //this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FrmTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1575, 810);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.RtxtLogger);
            this.Controls.Add(this.BrEmail);
            this.Name = "FrmTest";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FrmTest";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser BrEmail;
        private System.Windows.Forms.RichTextBox RtxtLogger;
        private System.Windows.Forms.Button button1;
    }
}