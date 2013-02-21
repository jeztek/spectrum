using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Spectrum
{
	public class FileReceiver : Receiver
	{
		public FileReceiver(string filePath, double sampleRate)
		{
			m_File = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			m_BufferNumSamples = 1024;
			m_SampleRate = sampleRate;
			m_BytesPerSample = 8 * 2;
			m_ByteBuffer = new byte[m_BufferNumSamples * m_BytesPerSample];
			m_SampleBuffer = new Complex[m_BufferNumSamples];
			m_CopyBuffer = Marshal.AllocHGlobal(m_BufferNumSamples * m_BytesPerSample);
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
					int bytesToRead = m_BufferNumSamples * m_BytesPerSample;
					int bytesRead = m_File.Read(m_ByteBuffer, 0, bytesToRead);
					if (bytesRead != bytesToRead)
					{
						m_File.Seek(0, SeekOrigin.Begin);
						int remainingBytes = bytesToRead - bytesRead;
						m_File.Read(m_ByteBuffer, bytesRead, remainingBytes);
					}
					Marshal.Copy(m_ByteBuffer, 0, m_CopyBuffer, bytesToRead);
					m_ReceiveHandler(m_CopyBuffer, m_BufferNumSamples);
					numReceived += m_BufferNumSamples;
				}
				System.Threading.Thread.Sleep(10);
			}
		}

		private Task m_StreamingTask;
		private Action<IntPtr, int> m_ReceiveHandler;
		private IntPtr m_CopyBuffer;
		private Complex[] m_SampleBuffer;
		private byte[] m_ByteBuffer;
		private int m_BufferNumSamples;
		private int m_BytesPerSample;
		private double m_SampleRate;
		private FileStream m_File;
	}
}
