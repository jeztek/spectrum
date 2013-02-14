using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Numerics;
namespace Spectrum
{
	public class USRPNative
	{
		[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Init();

		[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int Tune(double frequency);

		[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int StartStreaming(double rate);

		[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetSamples(IntPtr samples, uint maxSamples);
	}

	public class USRPSource : RadioSource
	{
		public bool Init()
		{
			return USRPNative.Init() == 0;
		}

		public bool Tune(double frequency)
		{
			return USRPNative.Tune(frequency) == 0;
		}

		public bool StartStreaming(double rate)
		{
			USRPNative.StartStreaming(rate);
			m_StreamingTask = Task.Factory.StartNew(StreamHandler, TaskCreationOptions.LongRunning);
			return true;
		}

		private void StreamHandler()
		{
			IntPtr sampleBuffer = Marshal.AllocHGlobal(1024 * 8 * 2);
			unsafe
			{
				double* samples = (double*)sampleBuffer;
				int q = sizeof(Complex);
				while (true)
				{
					int numSamples = (int)USRPNative.GetSamples(sampleBuffer, 1024);	
					for (int i = 0; i < numSamples; i++)
					{
						m_Samples.Enqueue(samples[i]);
					}
				}
			}
		}

		public void GetSamples(double[] buffer)
		{
			int sampleCount = 0;
			double sample;
			while (sampleCount < buffer.Length)
			{
				if (m_Samples.TryDequeue(out sample))
				{
					buffer[sampleCount] = sample;
					sampleCount++;
				}
			}
		}

		private ConcurrentQueue<double> m_Samples = new ConcurrentQueue<double>();

		private Task m_StreamingTask;
	}
}
