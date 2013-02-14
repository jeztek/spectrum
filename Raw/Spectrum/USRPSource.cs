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
		public static extern int SetSampleRate(double rate);

		[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int StartStreaming();

		[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int GetSamples(IntPtr samples, uint maxSamples);
	}

	public class USRPSource : RadioSource
	{
		public bool Init()
		{
			return USRPNative.Init() == 0;
		}

		public bool SetFrequency(double frequency)
		{
			return USRPNative.Tune(frequency) == 0;
		}

		public bool SetSampleRate(double rate)
		{
			return USRPNative.SetSampleRate(rate) == 0;
		}

		public bool StartStreaming()
		{
			USRPNative.StartStreaming();
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
				int f = 0;

				while (true)
				{
					
					int numSamples = (int)USRPNative.GetSamples(sampleBuffer, 1024);	
					for (int i = 0; i < numSamples / 2; i += 2)
					{
					//	double z = 4;
					//	m_Samples.Enqueue(Math.Cos(Math.PI * 2.0f * (double)f / z));
					//	m_Samples.Enqueue(-Math.Sin(Math.PI * 2.0f * (double)f / z));
					//	f++;
						m_Samples.Enqueue(samples[i]);
						m_Samples.Enqueue(samples[i + 1]);
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
