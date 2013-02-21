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

namespace WaveSender
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			m_Transmitter = new USRPTransmitter();
			if (!m_Transmitter.Init())
			{
				MessageBox.Show("Unable to initialize radio!");
				Close();
				return;
			}
			if (!m_Transmitter.SetFrequency(m_Frequency))
			{
				MessageBox.Show("Unable to tune radio!");
				Close();
				return;
			}
			System.Threading.Thread.Sleep(500);
			m_Transmitter.SetSampleRate(m_SampleRate);
			m_Transmitter.SetGain(m_Gain);
			m_Transmitter.StartTransmitting((IntPtr samples, int maxSamples) => SendHandler(samples, maxSamples));
		}

		
		int sc = 0;
		private int SendHandler(IntPtr samples, int maxSamples)
		{
			int numSamples = 1024;
			double waveHz = (double)numericUpDown4.Value * 1000.0;
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
			return numSamples;
		}

		private void paintArea1_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Color.Black);
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
		}

		double m_SampleRate = 2e6;
		double m_Frequency = 902e6;
		double m_Gain = 25;
		USRPTransmitter m_Transmitter;

		ConcurrentQueue<double[]> m_PaintQueue = new ConcurrentQueue<double[]>();

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			if (m_Transmitter != null)
			{
				m_Frequency = (double)numericUpDown1.Value * 1e6;
				m_Transmitter.SetFrequency(m_Frequency);
			}
		}

		private void numericUpDown2_ValueChanged(object sender, EventArgs e)
		{
			if (m_Transmitter != null)
			{
				m_SampleRate = (double)numericUpDown2.Value * 1e3;
				m_Transmitter.SetSampleRate(m_SampleRate);
			}
		}

		private void numericUpDown3_ValueChanged(object sender, EventArgs e)
		{
			if (m_Transmitter != null)
			{
				m_Gain = (double)numericUpDown3.Value;
				m_Transmitter.SetGain(m_Gain);
			}
		}

	}
}
