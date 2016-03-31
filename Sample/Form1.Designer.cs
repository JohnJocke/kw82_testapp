namespace Sample
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
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxJ2534 = new System.Windows.Forms.ComboBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.buttonReadInfo = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBoxLog
            // 
            this.textBoxLog.Location = new System.Drawing.Point(260, 12);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLog.Size = new System.Drawing.Size(662, 324);
            this.textBoxLog.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 344);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "J2534:";
            // 
            // comboBoxJ2534
            // 
            this.comboBoxJ2534.FormattingEnabled = true;
            this.comboBoxJ2534.Location = new System.Drawing.Point(74, 341);
            this.comboBoxJ2534.Name = "comboBoxJ2534";
            this.comboBoxJ2534.Size = new System.Drawing.Size(133, 21);
            this.comboBoxJ2534.TabIndex = 8;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(260, 342);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(662, 23);
            this.progressBar1.TabIndex = 10;
            // 
            // buttonReadInfo
            // 
            this.buttonReadInfo.Location = new System.Drawing.Point(18, 12);
            this.buttonReadInfo.Name = "buttonReadInfo";
            this.buttonReadInfo.Size = new System.Drawing.Size(195, 37);
            this.buttonReadInfo.TabIndex = 12;
            this.buttonReadInfo.Text = "Connect";
            this.buttonReadInfo.UseVisualStyleBackColor = true;
            this.buttonReadInfo.Click += new System.EventHandler(this.buttonReadInfo_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(18, 80);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(76, 20);
            this.textBox1.TabIndex = 13;
            this.textBox1.Text = "64";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(18, 163);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(195, 37);
            this.button1.TabIndex = 14;
            this.button1.Text = "Read DTC";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(18, 119);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(195, 37);
            this.button2.TabIndex = 15;
            this.button2.Text = "Read Identifier";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 64);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "ECU address:";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(18, 207);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(195, 37);
            this.button3.TabIndex = 17;
            this.button3.Text = "Stop";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 425);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.buttonReadInfo);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxJ2534);
            this.Controls.Add(this.textBoxLog);
            this.Name = "Form1";
            this.Text = "KW82";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxJ2534;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button buttonReadInfo;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button3;
    }
}

