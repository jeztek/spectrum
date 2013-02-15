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
	public class USRPReceiver : RadioReceiver
	{
		public bool Init()
		{
			return RadioDriver.USRP.Init() == 0 && RadioDriver.USRP.InitRX() == 0; 
		}

		public bool SetFrequency(double frequency)
		{
			return RadioDriver.USRP.SetRXFrequency(frequency) == 0;
		}

		public bool SetSampleRate(double rate)
		{
			return RadioDriver.USRP.SetRXSampleRate(rate) == 0;
		}

		public bool SetGain(double gain)
		{
			return RadioDriver.USRP.SetRXGain(gain) == 0;
		}

		public bool StartReceiving(Action<IntPtr, int> receiveHandler)
		{
			RadioDriver.USRP.StartReceiving();
			m_StreamingTask = Task.Factory.StartNew(() => StreamHandler(receiveHandler), TaskCreationOptions.LongRunning);
			return true;
		}

		private void StreamHandler(Action<IntPtr, int> receiveHandler)
		{
			int bufferSize = 1024;
			IntPtr sampleBuffer = Marshal.AllocHGlobal(bufferSize * 8 * 2);
			while (true)
			{
				int numSamples = RadioDriver.USRP.ReceiveSamples(sampleBuffer, bufferSize);
				receiveHandler(sampleBuffer, numSamples);
			}
		}

		private Task m_StreamingTask;
	}
}
