using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace Spectrum
{
	public class ReceiveQueue
	{
		public ReceiveQueue()
		{

		}

		private void ReceiveHandler(IntPtr samples, int numSamples)
		{
			unsafe
			{
				Complex* sampleArray = (Complex*)samples;
				for (int i = 0; i < numSamples; i++)
				{
					m_Queue.Enqueue(sampleArray[i]);
				}
			}
		}

		public void GetSamples(Complex[] samples)
		{
			int count = 0;
			while (count < samples.Length)
			{
				Complex sample;
				if (m_Queue.TryDequeue(out sample))
				{
					samples[count] = sample;
					count++;
				}
				else
				{
					System.Threading.Thread.Sleep(10);
				}
			}
		}

		public Action<IntPtr, int> Handler
		{
			get
			{
				return (IntPtr samples, int numSamples) => ReceiveHandler(samples, numSamples);
			}
		}

		public ConcurrentQueue<Complex> m_Queue = new ConcurrentQueue<Complex>();
	}
}
