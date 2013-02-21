using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Spectrum
{
	public class VirtualReceiver : Receiver
	{
		public VirtualReceiver(VirtualTransmitter transmitter, double sampleRate)
		{
			m_BufferNumSamples = 16384;
			m_SampleRate = sampleRate;
			m_SampleBuffer = new Complex[m_BufferNumSamples];
			m_CopyBuffer = Marshal.AllocHGlobal(m_BufferNumSamples * 8 * 2);
			m_Transmitter = transmitter;
		}

		public override bool StartReceiving(Action<IntPtr, int> receiveHandler)
		{
			m_ReceiveHandler = receiveHandler;
			m_StreamingTask = Task.Factory.StartNew(() => StreamHandler(), TaskCreationOptions.LongRunning);
			return true;
		}

		private void StreamHandler()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			Int64 numReceived = 0;
			Random random = new Random();

			while (true)
			{
				long elapsedMs = stopwatch.ElapsedMilliseconds;
				Int64 expectedTransmitted = (Int64)((elapsedMs / 1000.0) * m_SampleRate);
				while (expectedTransmitted > numReceived)
				{
					unsafe
					{
						Complex* samples = (Complex*)m_CopyBuffer;
						int count = 0;
						while (count < m_BufferNumSamples)
						{
							Complex sample;
							if (m_Transmitter.TransmitQueue.TryDequeue(out sample))
							{
								sample.Real += (random.NextDouble() * 2.0f - 1.0f) * 0.01;
								sample.Imag += (random.NextDouble() * 2.0f - 1.0f) * 0.01;
								samples[count] = sample;
								count++;
							}
							else
							{
								System.Threading.Thread.Sleep(1);
							}
						}
					}
					numReceived += m_BufferNumSamples;
					m_ReceiveHandler(m_CopyBuffer, m_BufferNumSamples);
				}
				System.Threading.Thread.Sleep(10);
			}
		}

		private Task m_StreamingTask;
		private VirtualTransmitter m_Transmitter;
		private Action<IntPtr, int> m_ReceiveHandler;
		private IntPtr m_CopyBuffer;
		private Complex[] m_SampleBuffer;
		private double m_SampleRate;
		private int m_BufferNumSamples;
	}
}