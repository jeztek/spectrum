using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Spectrum
{
	public class USRPTransmitter : Transmitter
	{
		public bool Init()
		{
			return RadioDriver.USRP.Init() == 0 && RadioDriver.USRP.InitTX() == 0; 
		}

		public bool SetFrequency(double frequency)
		{
			return RadioDriver.USRP.SetTXFrequency(frequency) == 0;
		}

		public bool SetSampleRate(double rate)
		{
			return RadioDriver.USRP.SetTXSampleRate(rate) == 0;
		}

		public bool SetGain(double gain)
		{
			return RadioDriver.USRP.SetTXGain(gain) == 0;
		}

		public override bool StartTransmitting(Func<IntPtr, int, int> sendHandler)
		{
			m_StreamingTask = Task.Factory.StartNew(() => StreamHandler(sendHandler), TaskCreationOptions.LongRunning);
			return true;
		}

		private void StreamHandler(Func<IntPtr, int, int> sendHandler)
		{	
			int bufferSize = 4096;
			IntPtr sampleBuffer = Marshal.AllocHGlobal(bufferSize * 8 * 2);
			while (true)
			{
				int numSamples = sendHandler(sampleBuffer, bufferSize);
				RadioDriver.USRP.SendSamples(sampleBuffer, numSamples);
			}
		}

		private Task m_StreamingTask;
	}
}
