using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Spectrum
{
	public class VirtualTransmitter : Transmitter
	{
		public VirtualTransmitter(double sampleRate)
		{
			m_SampleRate = sampleRate;
			m_BufferNumSamples = 16384;
			m_CopyBuffer = Marshal.AllocHGlobal(m_BufferNumSamples * 8 * 2);
		}

		public override bool StartTransmitting(Func<IntPtr, int, int> sendHandler)
		{
			m_SendHandler = sendHandler;
			m_StreamingTask = Task.Factory.StartNew(() => StreamHandler(), TaskCreationOptions.LongRunning);
			return true;
		}

		private void StreamHandler()
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			Int64 numTransmitted = 0;
			while (true)
			{
				long elapsedMs = stopwatch.ElapsedMilliseconds;
				Int64 expectedTransmitted = (Int64)((elapsedMs / 1000.0) * m_SampleRate);
				while (expectedTransmitted > numTransmitted)
				{
					int numSamples = m_SendHandler(m_CopyBuffer, m_BufferNumSamples);
					numTransmitted += numSamples;
					unsafe
					{
						Complex* samples = (Complex*)m_CopyBuffer;
						for (int i = 0; i < numSamples; i++)
						{
							m_TransmitQueue.Enqueue(samples[i]);
						}
					}
				}
				System.Threading.Thread.Sleep(10);
			}
		}

		private ConcurrentQueue<Complex> m_TransmitQueue = new ConcurrentQueue<Complex>();
		public ConcurrentQueue<Complex> TransmitQueue
		{
			get { return m_TransmitQueue; }
		}

		private IntPtr m_CopyBuffer;
		private Func<IntPtr, int, int> m_SendHandler;
		private int m_BufferNumSamples;
		private double m_SampleRate;
		private Task m_StreamingTask;
	}
}
