using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Spectrum
{
	public class USRPNative
	{
		[DllImport("RadioDriver.dll")]
		public static extern void Init(double frequency, double rate);
		[DllImport("RadioDriver.dll")]
		public static extern uint GetSamples(IntPtr samples, uint maxSamples);
	}

	public class USRPSource : RadioSource
	{
		public bool Init(double frequency, double rate)
		{
			//m_Radio = new UHD();
			//m_Radio.Init(frequency, samplesPerSecond);
			USRPNative.Init(frequency, rate);
			IntPtr sampleBuffer = Marshal.AllocHGlobal(1024 * 4 * 2);
			float[] samples = new float[1024 * 2];
			while (true)
			{
				int numSamples = (int)USRPNative.GetSamples(sampleBuffer, 1024);
				Marshal.Copy(sampleBuffer, samples, 0, (int)numSamples);
				for (int i = 0; i < numSamples; i++)
				{
					Console.WriteLine(samples[i]);
				}
			}
			return true;
		}

		//private UHD m_Radio;
	}
}
