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
			if (!m_Source.Init())
			{
				MessageBox.Show("Unable to initialize radio!");
				Close();
				return;
			}
			if (!m_Source.SetFrequency(93.3e6))
			{
				MessageBox.Show("Unable to tune radio!");
				Close();
				return;
			}
			m_Source.SetSampleRate(m_SampleRate);
			m_Source.StartStreaming();
			m_FFTTask = Task.Factory.StartNew(FFTHandler, TaskCreationOptions.LongRunning);
			m_FFT = new LomontFFT();
			timer1.Interval = (int)m_FFTRate;
		}

		private void FFTHandler()
		{
			double[] samples = new double[m_FFTSize * 2];
			double[] avg = new double[m_FFTSize];
			double[] window = new double[m_FFTSize];
			double[] coeffs = { 0.35875, 0.48829, 0.14128, 0.01168 };
			for (int i = 0; i < m_FFTSize; i++)
			{
				for (int c = 0; c < coeffs.Length; c++)
				{
					window[i] += Math.Pow(-1.0, (double)c) * coeffs[c] * Math.Cos(2.0 * (double)c * Math.PI * ((double)i + 0.5) / ((double)m_FFTSize - 1.0));
				}
			}

			int skip = (int)(m_SampleRate / (double)m_FFTSize / m_FFTRate);
			int n = 0;
			while (true)
			{
				m_Source.GetSamples(samples);
				n++;
				if (n == skip)
				{
					for (int i = 0; i < samples.Length; i++)
					{
						samples[i] *= window[i / 2];
					}
					m_FFT.TableFFT(samples, true);
					double alpha = 2.0 / (double)m_FFTRate;
					double[] values = new double[m_FFTSize];
					for (int i = 0; i < 1024; i++)
					{
						int i2 = i;
						double value = Math.Sqrt(samples[i2 * 2 + 0] * samples[i2 * 2 + 0] + samples[i2 * 2 + 1] * samples[i2 * 2 + 1]);
						double avgValue = avg[i] * alpha + value * (1.0 - alpha);
						values[i] = Math.Pow(Math.Min(1.0, value * 15000.0), 3.0f);
						avg[i] = avgValue;
					}
					m_PaintQueue.Enqueue(values);
					n = 0;
				}
			}
		}

		private void paintArea1_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Color.Black);
			double[] values = null;
			if (m_PaintQueue.TryDequeue(out values))
			{
				m_LastPaintValues = values;
			}
			if (m_LastPaintValues != null)
			{
				double max = 1.0;
				double[] paintArray = new double[1024];
				for (int i = 0; i < m_FFTSize / 2; i++)
				{
					paintArray[i + m_FFTSize / 2] = m_LastPaintValues[i];
					paintArray[i] = m_LastPaintValues[i + m_FFTSize / 2];
				}
				for (int i = 1; i < paintArray.Length; i++)
				{
					g.DrawLine(Pens.Cyan, new PointF(100.0f + (float)(i - 1) * 0.5f, 400.0f - (float)(paintArray[i - 1] / max) * 200.0f), new PointF(100.0f + (float)(i) * 0.5f, 400.0f - (float)(paintArray[i] / max) * 200.0f));
				}
			}
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			paintArea1.Invalidate();
		}

		int m_FFTSize = 1024;
		double m_SampleRate = 250e3;
		double m_FFTRate = 30;
		double m_Frequency;
		LomontFFT m_FFT;
		Task m_FFTTask;
		USRPSource m_Source;
		double[] m_LastPaintValues;

		ConcurrentQueue<double[]> m_PaintQueue = new ConcurrentQueue<double[]>();

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			m_Frequency = (double)numericUpDown1.Value * 1e6;
			m_Source.SetFrequency(m_Frequency);
		}

		private void numericUpDown2_ValueChanged(object sender, EventArgs e)
		{
			m_SampleRate = (double)numericUpDown2.Value * 1e3;
			m_Source.SetSampleRate(m_SampleRate);
		}

	}
}
