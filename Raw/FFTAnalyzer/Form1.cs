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
using Spectrum;

namespace FFTAnalyzer
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			m_Source = new USRPReceiver();
			if (!m_Source.Init())
			{
				MessageBox.Show("Unable to initialize radio!");
				Close();
				return;
			}
			if (!m_Source.SetFrequency(m_Frequency))
			{
				MessageBox.Show("Unable to tune radio!");
				Close();
				return;
			}
		
			m_ReceiveQueue = new ReceiveQueue();
			m_Source.SetSampleRate(m_SampleRate);
			m_Source.SetGain(m_Gain);
			m_Source.StartReceiving(m_ReceiveQueue.Handler);
			m_FFTTask = Task.Factory.StartNew(FFTHandler, TaskCreationOptions.LongRunning);
			m_FFT = new LomontFFT();
			timer1.Interval = (int)m_FFTRate;
		}

		
		int sc = 0;
		private void SendHandler(IntPtr samples, int numSamples)
		{
			double waveHz = (double)numericUpDown2.Value * 100.0;
			double f = waveHz / m_SampleRate;
			unsafe
			{
				double* sampleBuffer = (double*)samples;
				for (int i = 0; i < numSamples; i++)
				{
					double t = Math.PI * 2.0 * f * (double)sc;
					sampleBuffer[i * 2 + 0] = Math.Cos(t) * 0.5;
					sampleBuffer[i * 2 + 1] = -Math.Sin(t) * 0.5;
					sc++;
				}
			}
		}

		private void FFTHandler()
		{
			Complex[] samples = new Complex[m_FFTSize];
			double[] avg = new double[m_FFTSize];
			double[] window = new double[m_FFTSize];
			double[] coeffs = { 0.35875, 0.48829, 0.14128, 0.01168 };
			double windowPower = 0;
			for (int i = 0; i < m_FFTSize; i++)
			{
				for (int c = 0; c < coeffs.Length; c++)
				{
					window[i] += Math.Pow(-1.0, (double)c) * coeffs[c] * Math.Cos(2.0 * (double)c * Math.PI * ((double)i + 0.5) / ((double)m_FFTSize - 1.0));
				}
				windowPower += window[i] * window[i];
			}
			
			int skip = (int)(m_SampleRate / (double)m_FFTSize / m_FFTRate);
			int n = 0;
			while (true)
			{
				m_ReceiveQueue.GetSamples(samples);
				n++;
				if (n == skip)
				{
					double[] fftSamples = new double[m_FFTSize * 2];
					for (int i = 0; i < samples.Length; i++)
					{
						fftSamples[i * 2 + 0] = samples[i].Real * window[i];
						fftSamples[i * 2 + 1] = samples[i].Imag * window[i];
					}
					m_FFT.TableFFT(fftSamples, true);
					double alpha = 0.55;// 6.0 / (double)m_FFTRate;
					double[] values = new double[m_FFTSize];
					for (int i = 0; i < m_FFTSize; i++)
					{
						double offset = -10 * Math.Log10(m_FFTSize) - 10 * Math.Log10(windowPower / (double)m_FFTSize);
						Complex fftSample = new Complex(fftSamples[i * 2 + 0], fftSamples[i * 2 + 1]);
						double value = fftSample.Magnitude();
						double avgValue = avg[i] * alpha + value * (1.0 - alpha);
						double db = 20.0 * Math.Log10(avgValue);
						values[i] = db;
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
				double[] paintArray = new double[m_FFTSize];
				for (int i = 0; i < m_FFTSize / 2; i++)
				{
					paintArray[m_FFTSize - (i + m_FFTSize / 2) - 1] = m_LastPaintValues[i];
					paintArray[m_FFTSize - i - 1] = m_LastPaintValues[i + m_FFTSize / 2];
				}
				float scale = (float)e.ClipRectangle.Width / (float)m_FFTSize;
				for (int i = 1; i < paintArray.Length; i++)
				{
					float cdb = (float)paintArray[i] / -120.0f;
					float pdb = (float)paintArray[i - 1] / -120.0f;
					g.DrawLine(Pens.Cyan, new PointF((float)(i - 1) * scale, 200.0f + pdb * 200.0f), new PointF((float)(i) * scale, 200.0f + cdb * 200.0f));
				}
			}
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			paintArea1.Invalidate();
		}

		int m_FFTSize = 1024;
		double m_SampleRate = 2e6;
		double m_FFTRate = 30;
		double m_Frequency = 93.3e6;
		double m_Gain = 25;
		LomontFFT m_FFT;
		Task m_FFTTask;
		ReceiveQueue m_ReceiveQueue;
		USRPReceiver m_Source;
		double[] m_LastPaintValues;

		ConcurrentQueue<double[]> m_PaintQueue = new ConcurrentQueue<double[]>();

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			if (m_Source != null)
			{
				m_Frequency = (double)numericUpDown1.Value * 1e6;
				m_Source.SetFrequency(m_Frequency);
			}
		}

		private void numericUpDown2_ValueChanged(object sender, EventArgs e)
		{
			if (m_Source != null)
			{
				m_SampleRate = (double)numericUpDown2.Value * 1e3;
				m_Source.SetSampleRate(m_SampleRate);
			}
		}

		private void numericUpDown3_ValueChanged(object sender, EventArgs e)
		{
			if (m_Source != null)
			{
				m_Gain = (double)numericUpDown3.Value;
				m_Source.SetGain(m_Gain);
			}
		}

	}
}
