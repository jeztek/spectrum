using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Spectrum
{
	public class RadioDriver
	{
		public class USRP
		{
			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int Init();

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int InitRX();

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int InitTX();

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int SetRXFrequency(double frequency);

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int SetTXFrequency(double frequency);

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int SetRXSampleRate(double rate);
			
			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int SetTXSampleRate(double rate);

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int SetRXGain(double rate);
			
			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int SetTXGain(double rate);

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int StartReceiving();

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int ReceiveSamples(IntPtr samples, int maxSamples);

			[DllImport("RadioDriver.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int SendSamples(IntPtr samples, int numSamples);
		}
	}
}
