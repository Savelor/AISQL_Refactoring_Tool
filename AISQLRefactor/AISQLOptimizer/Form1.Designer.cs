namespace AISQLOptimizer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            richTextBox1 = new RichTextBox();
            label3 = new Label();
            label4 = new Label();
            webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            richTextBox2 = new RichTextBox();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            treeView1 = new TreeView();
            label5 = new Label();
            label6 = new Label();
            comboMetric = new ComboBox();
            label8 = new Label();
            label9 = new Label();
            label10 = new Label();
            label7 = new Label();
            label11 = new Label();
            label12 = new Label();
            ((System.ComponentModel.ISupportInitialize)webView21).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F);
            label1.ForeColor = Color.White;
            label1.Location = new Point(321, 40);
            label1.Margin = new Padding(3, 0, 0, 0);
            label1.Name = "label1";
            label1.Size = new Size(158, 38);
            label1.TabIndex = 1;
            label1.Text = "SQL Server:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 12F);
            label2.ForeColor = Color.White;
            label2.Location = new Point(318, 97);
            label2.Name = "label2";
            label2.Size = new Size(137, 38);
            label2.TabIndex = 2;
            label2.Text = "Database:";
            // 
            // richTextBox1
            // 
            richTextBox1.Font = new Font("Cascadia Mono", 9.5F);
            richTextBox1.Location = new Point(304, 154);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(614, 1083);
            richTextBox1.TabIndex = 3;
            richTextBox1.Text = "";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 12F);
            label3.ForeColor = Color.White;
            label3.Location = new Point(478, 52);
            label3.Name = "label3";
            label3.Size = new Size(0, 38);
            label3.TabIndex = 4;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 12F);
            label4.ForeColor = Color.White;
            label4.Location = new Point(489, 90);
            label4.Name = "label4";
            label4.Size = new Size(0, 38);
            label4.TabIndex = 5;
            // 
            // webView21
            // 
            webView21.AllowExternalDrop = true;
            webView21.CreationProperties = null;
            webView21.DefaultBackgroundColor = Color.White;
            webView21.Location = new Point(978, 154);
            webView21.Name = "webView21";
            webView21.Size = new Size(1256, 956);
            webView21.TabIndex = 6;
            webView21.ZoomFactor = 1D;
            // 
            // richTextBox2
            // 
            richTextBox2.Location = new Point(968, 1157);
            richTextBox2.Name = "richTextBox2";
            richTextBox2.Size = new Size(919, 63);
            richTextBox2.TabIndex = 7;
            richTextBox2.Text = "";
            // 
            // button2
            // 
            button2.BackColor = Color.FromArgb(64, 64, 64);
            button2.Cursor = Cursors.Hand;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Segoe UI", 13F);
            button2.ForeColor = Color.White;
            button2.Location = new Point(1899, 1160);
            button2.Name = "button2";
            button2.Padding = new Padding(3);
            button2.Size = new Size(108, 61);
            button2.TabIndex = 8;
            button2.Text = "GO !";
            button2.UseCompatibleTextRendering = true;
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click_1;
            // 
            // button3
            // 
            button3.BackColor = Color.FromArgb(64, 64, 64);
            button3.Cursor = Cursors.Hand;
            button3.FlatAppearance.BorderColor = Color.DimGray;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Font = new Font("Segoe UI", 13F);
            button3.ForeColor = Color.White;
            button3.Location = new Point(608, 26);
            button3.Name = "button3";
            button3.Size = new Size(183, 55);
            button3.TabIndex = 9;
            button3.Text = "Optimize code in window";
            button3.UseCompatibleTextRendering = true;
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.BackColor = Color.FromArgb(64, 64, 64);
            button4.Cursor = Cursors.Hand;
            button4.FlatAppearance.BorderColor = Color.DimGray;
            button4.FlatAppearance.BorderSize = 0;
            button4.FlatStyle = FlatStyle.Flat;
            button4.Font = new Font("Segoe UI", 13F);
            button4.ForeColor = Color.White;
            button4.Location = new Point(824, 11);
            button4.Name = "button4";
            button4.Size = new Size(268, 55);
            button4.TabIndex = 10;
            button4.Text = "Optimize selected objects";
            button4.UseCompatibleTextRendering = true;
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // treeView1
            // 
            treeView1.CheckBoxes = true;
            treeView1.Font = new Font("Segoe UI", 11.5F);
            treeView1.Location = new Point(26, 154);
            treeView1.Name = "treeView1";
            treeView1.ShowLines = false;
            treeView1.Size = new Size(240, 1016);
            treeView1.TabIndex = 14;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 12F);
            label5.ForeColor = Color.White;
            label5.Location = new Point(1386, 62);
            label5.Name = "label5";
            label5.Size = new Size(228, 38);
            label5.TabIndex = 15;
            label5.Text = "Input Tokens     : ";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.White;
            label6.Location = new Point(1386, 97);
            label6.Name = "label6";
            label6.Size = new Size(227, 38);
            label6.TabIndex = 16;
            label6.Text = "Output Tokens  : ";
            // 
            // comboMetric
            // 
            comboMetric.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            comboMetric.ForeColor = SystemColors.HotTrack;
            comboMetric.FormattingEnabled = true;
            comboMetric.Location = new Point(26, 103);
            comboMetric.Name = "comboMetric";
            comboMetric.Size = new Size(187, 49);
            comboMetric.TabIndex = 17;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI", 12F);
            label8.ForeColor = Color.White;
            label8.Location = new Point(1386, 0);
            label8.Name = "label8";
            label8.Size = new Size(215, 38);
            label8.TabIndex = 19;
            label8.Text = "Foundry Agent :";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 12F);
            label9.ForeColor = Color.White;
            label9.Location = new Point(1386, 33);
            label9.Name = "label9";
            label9.Size = new Size(215, 38);
            label9.TabIndex = 20;
            label9.Text = "AI Model          :";
            // 
            // label10
            // 
            label10.BackColor = Color.Transparent;
            label10.Font = new Font("Segoe UI", 22F);
            label10.ForeColor = SystemColors.Window;
            label10.Location = new Point(12, 10);
            label10.Name = "label10";
            label10.Size = new Size(549, 86);
            label10.TabIndex = 21;
            label10.Text = "AI  SQL  Refactoring Tool";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Cursor = Cursors.Hand;
            label7.Font = new Font("Segoe UI", 16F, FontStyle.Bold | FontStyle.Underline);
            label7.ForeColor = Color.White;
            label7.Location = new Point(2074, 38);
            label7.Name = "label7";
            label7.Size = new Size(0, 51);
            label7.TabIndex = 18;
            label7.Click += label7_Click;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            label11.ForeColor = Color.DarkOliveGreen;
            label11.Location = new Point(1190, 36);
            label11.Name = "label11";
            label11.Size = new Size(156, 38);
            label11.TabIndex = 22;
            label11.Text = "Connected";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.BackColor = Color.Transparent;
            label12.Font = new Font("Segoe UI", 11F);
            label12.ForeColor = Color.White;
            label12.Location = new Point(26, 82);
            label12.Name = "label12";
            label12.Size = new Size(581, 36);
            label12.TabIndex = 23;
            label12.Text = "Analyze • Optimize • Modernize SQL Server Code";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(15F, 38F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientInactiveCaption;
            ClientSize = new Size(1698, 956);
            Controls.Add(label12);
            Controls.Add(label11);
            Controls.Add(label10);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(comboMetric);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(treeView1);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(richTextBox2);
            Controls.Add(webView21);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(richTextBox1);
            Controls.Add(label2);
            Controls.Add(label1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            Text = "AISQLOptimizer";
            ((System.ComponentModel.ISupportInitialize)webView21).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label1;
        private Label label2;
        private RichTextBox richTextBox1;
        private Label label3;
        private Label label4;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private RichTextBox richTextBox2;
        private Button button2;
        private Button button3;
        private Button button4;
        private TreeView treeView1;
        private Label label5;
        private Label label6;
        private ComboBox comboMetric;
        private Label label8;
        private Label label9;
        private Label label10;
        private Label label7;
        private Label label11;
        private Label label12;
    }
}
