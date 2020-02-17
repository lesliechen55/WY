namespace EmailWY
{
    partial class FrmTest
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
            this.btnStart = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnOpenEdmLog = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lbClientType = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.rbTypeTwo = new System.Windows.Forms.RadioButton();
            this.rbTypeOne = new System.Windows.Forms.RadioButton();
            this.SlbRemark = new System.Windows.Forms.Label();
            this.SLblVersion = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.BrEmail = new System.Windows.Forms.WebBrowser();
            this.button1 = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(30, 3);
            this.btnStart.Margin = new System.Windows.Forms.Padding(30, 3, 3, 3);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(101, 43);
            this.btnStart.TabIndex = 12;
            this.btnStart.TabStop = false;
            this.btnStart.Text = "测试一";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(518, 3);
            this.btnOpen.Margin = new System.Windows.Forms.Padding(30, 3, 3, 3);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(101, 43);
            this.btnOpen.TabIndex = 13;
            this.btnOpen.TabStop = false;
            this.btnOpen.Text = "测试二";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnOpenEdmLog
            // 
            this.btnOpenEdmLog.Location = new System.Drawing.Point(1097, 3);
            this.btnOpenEdmLog.Margin = new System.Windows.Forms.Padding(30, 3, 3, 3);
            this.btnOpenEdmLog.Name = "btnOpenEdmLog";
            this.btnOpenEdmLog.Size = new System.Drawing.Size(101, 43);
            this.btnOpenEdmLog.TabIndex = 14;
            this.btnOpenEdmLog.TabStop = false;
            this.btnOpenEdmLog.Text = "测试三";
            this.btnOpenEdmLog.UseVisualStyleBackColor = true;
            this.btnOpenEdmLog.Click += new System.EventHandler(this.btnOpenEdmLog_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.SystemColors.Control;
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 488F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23.29957F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20.81377F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 24.41315F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31.46552F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 372F));
            this.tableLayoutPanel1.Controls.Add(this.lbClientType, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnStart, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnOpen, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnOpenEdmLog, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1389, 796);
            this.tableLayoutPanel1.TabIndex = 24;
            // 
            // lbClientType
            // 
            this.lbClientType.AutoSize = true;
            this.lbClientType.Location = new System.Drawing.Point(824, 20);
            this.lbClientType.Margin = new System.Windows.Forms.Padding(30, 20, 30, 10);
            this.lbClientType.Name = "lbClientType";
            this.lbClientType.Size = new System.Drawing.Size(77, 12);
            this.lbClientType.TabIndex = 20;
            this.lbClientType.Text = "客户端类型：";
            // 
            // panel3
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panel3, 4);
            this.panel3.Controls.Add(this.button1);
            this.panel3.Controls.Add(this.rbTypeTwo);
            this.panel3.Controls.Add(this.rbTypeOne);
            this.panel3.Controls.Add(this.SlbRemark);
            this.panel3.Controls.Add(this.SLblVersion);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(3, 749);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1383, 44);
            this.panel3.TabIndex = 18;
            // 
            // rbTypeTwo
            // 
            this.rbTypeTwo.AutoSize = true;
            this.rbTypeTwo.Location = new System.Drawing.Point(1052, 14);
            this.rbTypeTwo.Margin = new System.Windows.Forms.Padding(2);
            this.rbTypeTwo.Name = "rbTypeTwo";
            this.rbTypeTwo.Size = new System.Drawing.Size(47, 16);
            this.rbTypeTwo.TabIndex = 7;
            this.rbTypeTwo.TabStop = true;
            this.rbTypeTwo.Text = "产品";
            this.rbTypeTwo.UseVisualStyleBackColor = true;
            // 
            // rbTypeOne
            // 
            this.rbTypeOne.AutoSize = true;
            this.rbTypeOne.Location = new System.Drawing.Point(921, 12);
            this.rbTypeOne.Margin = new System.Windows.Forms.Padding(2);
            this.rbTypeOne.Name = "rbTypeOne";
            this.rbTypeOne.Size = new System.Drawing.Size(47, 16);
            this.rbTypeOne.TabIndex = 6;
            this.rbTypeOne.TabStop = true;
            this.rbTypeOne.Text = "一般";
            this.rbTypeOne.UseVisualStyleBackColor = true;
            // 
            // SlbRemark
            // 
            this.SlbRemark.AutoSize = true;
            this.SlbRemark.Location = new System.Drawing.Point(649, 16);
            this.SlbRemark.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.SlbRemark.Name = "SlbRemark";
            this.SlbRemark.Size = new System.Drawing.Size(41, 12);
            this.SlbRemark.TabIndex = 5;
            this.SlbRemark.Text = "label2";
            // 
            // SLblVersion
            // 
            this.SLblVersion.AutoSize = true;
            this.SLblVersion.Location = new System.Drawing.Point(263, 16);
            this.SLblVersion.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.SLblVersion.Name = "SLblVersion";
            this.SLblVersion.Size = new System.Drawing.Size(41, 12);
            this.SLblVersion.TabIndex = 4;
            this.SLblVersion.Text = "label1";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.richTextBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(1070, 55);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(316, 688);
            this.panel1.TabIndex = 19;
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.Color.SteelBlue;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(0, 0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(316, 688);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            // 
            // panel2
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panel2, 3);
            this.panel2.Controls.Add(this.BrEmail);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 55);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1061, 688);
            this.panel2.TabIndex = 21;
            // 
            // BrEmail
            // 
            this.BrEmail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BrEmail.Location = new System.Drawing.Point(0, 0);
            this.BrEmail.MinimumSize = new System.Drawing.Size(20, 20);
            this.BrEmail.Name = "BrEmail";
            this.BrEmail.Size = new System.Drawing.Size(1061, 688);
            this.BrEmail.TabIndex = 0;
            this.BrEmail.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.BrEmail_DocumentCompleted);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1188, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(83, 38);
            this.button1.TabIndex = 8;
            this.button1.Text = "测试四";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FrmTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1389, 796);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FrmTest";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FrmTest";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnOpenEdmLog;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lbClientType;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.RadioButton rbTypeTwo;
        private System.Windows.Forms.RadioButton rbTypeOne;
        private System.Windows.Forms.Label SlbRemark;
        private System.Windows.Forms.Label SLblVersion;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.WebBrowser BrEmail;
        private System.Windows.Forms.Button button1;
    }
}