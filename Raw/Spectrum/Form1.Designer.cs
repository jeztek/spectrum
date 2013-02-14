﻿namespace Spectrum
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
			this.components = new System.ComponentModel.Container();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.paintArea1 = new Spectrum.PaintArea();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			this.SuspendLayout();
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 33;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// numericUpDown1
			// 
			this.numericUpDown1.DecimalPlaces = 1;
			this.numericUpDown1.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.numericUpDown1.Location = new System.Drawing.Point(0, 0);
			this.numericUpDown1.Maximum = new decimal(new int[] {
            925,
            0,
            0,
            0});
			this.numericUpDown1.Minimum = new decimal(new int[] {
            88,
            0,
            0,
            0});
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(120, 20);
			this.numericUpDown1.TabIndex = 1;
			this.numericUpDown1.Value = new decimal(new int[] {
            88,
            0,
            0,
            0});
			this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// paintArea1
			// 
			this.paintArea1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.paintArea1.Location = new System.Drawing.Point(0, 0);
			this.paintArea1.Name = "paintArea1";
			this.paintArea1.Size = new System.Drawing.Size(872, 480);
			this.paintArea1.TabIndex = 0;
			this.paintArea1.Paint += new System.Windows.Forms.PaintEventHandler(this.paintArea1_Paint);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(872, 480);
			this.Controls.Add(this.numericUpDown1);
			this.Controls.Add(this.paintArea1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private PaintArea paintArea1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.NumericUpDown numericUpDown1;
	}
}

