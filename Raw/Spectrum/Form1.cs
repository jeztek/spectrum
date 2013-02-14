using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Numerics;

namespace Spectrum
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			m_Source = new USRPSource();
			m_Source.Init();
			m_Source.Tune(93.3e6);
			m_Source.StartStreaming(250e3);
			m_PaintFFT = new double[3][];
			m_PaintFFT[0] = new double[1024 * 2];
			m_PaintFFT[1] = new double[1024 * 2];
			m_PaintFFT[2] = new double[1024 * 2];
			m_IIR = new double[1024];
			m_FFTTask = Task.Factory.StartNew(FFTHandler, TaskCreationOptions.LongRunning);
			m_FFT = new LomontFFT();
		}

		private void FFTHandler()
		{
			while (true)
			{
				double[] which = m_PaintFFT[m_WhichPaint];
				m_Source.GetSamples(which);
				m_FFT.TableFFT(which, true);
			}
		}

		private void paintArea1_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Color.Black);
			double[] source = m_PaintFFT[(m_WhichPaint + 1) % 3];
			double[] values = new double[1024];
			double max = 0.0001;
			double alpha = 2.0 / 10.0;
			for (int i = 1; i < 1024; i++)
			{
				int i2 = i;
				double value = Math.Sqrt(source[i2 * 2 + 0] * source[i2 * 2 + 0] + source[i2 * 2 + 1] * source[i2 * 2 + 1]);
				double bv = m_IIR[i] * alpha + value * (1.0 - alpha);
				values[i] = Math.Exp(bv) - 1.0f;
				m_IIR[i] = bv;
			}
			for (int i = 1; i < values.Length; i++)
			{
				g.DrawLine(Pens.Cyan, new PointF(100.0f + (float)(i - 1) * 0.5f, 400.0f - (float)(values[i - 1] / max) * 200.0f), new PointF(100.0f + (float)(i) * 0.5f, 400.0f - (float)(values[i] / max) * 200.0f));
			}
			m_WhichPaint = (m_WhichPaint + 1) % 3;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			paintArea1.Invalidate();
		}

		LomontFFT m_FFT;
		Task m_FFTTask;
		USRPSource m_Source;
		double[][] m_PaintFFT;
		double[] m_IIR;
		int m_WhichPaint;

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			m_Source.Tune((float)numericUpDown1.Value * 1e6);
		}

	}
}
