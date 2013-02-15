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
	public class USRPTransmitter : RadioReceiver
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

		public bool StartSending(Action<IntPtr, int> sendHandler)
		{
			m_StreamingTask = Task.Factory.StartNew(() => StreamHandler(sendHandler), TaskCreationOptions.LongRunning);
			return true;
		}

		private void StreamHandler(Action<IntPtr, int> sendHandler)
		{
			int bufferSize = 1024;
			IntPtr sampleBuffer = Marshal.AllocHGlobal(bufferSize * 8 * 2);
			while (true)
			{
				sendHandler(sampleBuffer, bufferSize);
				RadioDriver.USRP.SendSamples(sampleBuffer, bufferSize);
			}
		}

		private ConcurrentQueue<double> m_Samples = new ConcurrentQueue<double>();
		private Task m_StreamingTask;
	}
}
