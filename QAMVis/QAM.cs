using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace QAMVis
{
	public class QAM
	{
		public QAM(int amplitudes, int phases, int frequencies, int frameLength)
		{
			m_NumFrequencies = frequencies;
			m_NumAmplitudes = amplitudes;
			m_NumPhases = phases;
			m_FrameLength = frameLength;
			m_FFT = new FFT(frameLength);
		}

		public float[] Modulate(UInt64 symbol)
		{
			int subSymbolBits = (int)Math.Log(m_NumAmplitudes * m_NumPhases, 2);
			float baseAmplitude = 1.0f / (float)m_NumAmplitudes;
			float phaseDist = 1.0f / (m_NumPhases);
			float phaseOffset = phaseDist * 0.5f;
			float[] result = new float[m_FrameLength];
			float nrm = 1.0f / (float)m_NumFrequencies;
			for (int f = 0; f < m_NumFrequencies; f++)
			{
				UInt64 subSymbol = (symbol >> (subSymbolBits * f)) & (UInt64)((1 << subSymbolBits) - 1);
				int amplitudeIndex = (int)(subSymbol / (UInt64)m_NumPhases);
				int phaseIndex = (int)(subSymbol % (UInt64)m_NumPhases);
				float amplitude = m_NumAmplitudes == 1 ? 1.0f : baseAmplitude + ((float)amplitudeIndex / (float)(m_NumAmplitudes - 1)) * (1.0f - baseAmplitude);
				float phase = (phaseOffset + phaseDist * (float)phaseIndex) * 2.0f - 1.0f;
				for (int i = 0; i < m_FrameLength; i++)
				{
					float t = (float)i / (float)(m_FrameLength);
					result[i] += (float)Math.Sin(t * Math.PI * 2.0f * ((float)f + 1.0f) + phase * Math.PI) * amplitude * nrm;
				}
			}
			return result;
		}

		public UInt64 Demodulate(float[] data)
		{
			float nrm = (float)m_NumFrequencies;
			int subSymbolBits = (int)Math.Log(m_NumAmplitudes * m_NumPhases, 2);
			float magStep = (float)data.Length / (float)(m_NumAmplitudes * 2);
			List<FFT.Complex> coeffs = m_FFT.DFT(data, nrm).ToList();
			coeffs = coeffs.OrderBy(x => -x.Magnitude).ToList();
			coeffs = coeffs.GetRange(0, m_NumFrequencies);
			coeffs = coeffs.OrderBy(x => x.freq).ToList();
			UInt64 finalSymbol = 0;
			float phaseDist = 1.0f / (m_NumPhases);
			float phaseOffset = phaseDist * 0.5f;
			float amplitudeDist = 1.0f / (m_NumAmplitudes);
			float amplitudeOffset = amplitudeDist * 0.5f;
			for (int f = 0; f < m_NumFrequencies; f++)
			{
				float basePhase = (coeffs[f].Angle / (float)Math.PI) * 0.5f + 0.5f;
				float phase = (float)Math.Round((basePhase - phaseOffset) / phaseDist);
				float baseAmplitude = coeffs[f].Magnitude / ((float)data.Length * 0.5f);
				float phaseStep = (float)Math.PI / (float)(m_NumPhases - 1);
				float amplitude = (float)Math.Round((coeffs[f].Magnitude - magStep) / magStep);
				UInt64 symbol = (UInt64)Math.Min((float)NumSymbols, Math.Max(0.0, Math.Round(amplitude * (float)m_NumPhases + phase)));
				finalSymbol |= symbol << (subSymbolBits * f);
			}

			return finalSymbol;
		}

		public byte[] DecodeSymbols(UInt64[] symbols)
		{
			int bitsPerSymbol = (int)Math.Log(NumSymbols, 2);
			int symbolIndex = 0;
			int bitsRemaining = bitsPerSymbol;
			UInt64 currentSymbol = symbols[0];
			List<byte> resultStream = new List<byte>();
			while (symbolIndex < symbols.Length)
			{
				UInt64 currentByte = 0;
				int currentBits = 0;
				while (currentBits < 8)
				{
					int needBits = 8 - currentBits;
					int useBits = Math.Min(bitsRemaining, needBits);
					UInt64 useMask = (1U << useBits) - 1U;
					currentByte |= (currentSymbol & useMask) << currentBits;
					currentBits += useBits;
					bitsRemaining -= useBits;
					currentSymbol >>= useBits;
					if (bitsRemaining == 0)
					{
						symbolIndex++;
						if (symbolIndex < symbols.Length)
						{
							bitsRemaining = bitsPerSymbol;
							currentSymbol = symbols[symbolIndex];
						}
						else
						{
							break;
						}
					}
				}
				resultStream.Add((byte)currentByte);
			}
			return resultStream.ToArray();
		}

		public UInt64[] EncodeSymbols(byte[] input)
		{
			int bitsPerSymbol = (int)Math.Log(NumSymbols, 2);
			int byteIndex = 0;
			int bitsRemaining = 8;
			UInt64 currentByte = input[0];
			List<UInt64> resultStream = new List<UInt64>();
			while (byteIndex < input.Length)
			{
				UInt64 currentSymbol = 0;
				int currentBits = 0;
				while (currentBits < bitsPerSymbol)
				{
					int needBits = bitsPerSymbol - currentBits;
					int useBits = Math.Min(bitsRemaining, needBits);
					UInt64 useMask = (1U << useBits) - 1U;
					currentSymbol |= (currentByte & useMask) << currentBits;
					currentBits += useBits;
					bitsRemaining -= useBits;
					currentByte >>= useBits;
					if (bitsRemaining == 0)
					{
						byteIndex++;
						if (byteIndex < input.Length)
						{
							bitsRemaining = 8;
							currentByte = input[byteIndex];
						}
						else
						{
							break;
						}
					}
				}
				resultStream.Add(currentSymbol);
			}
			return resultStream.ToArray();
		}

		public int NumSubSymbols
		{
			get
			{
				return m_NumAmplitudes * m_NumPhases;
			}
		}

		public UInt64 NumSymbols
		{
			get
			{
				return (UInt64)Math.Pow(m_NumAmplitudes * m_NumPhases, m_NumFrequencies);
			}
		}

		private int m_NumFrequencies;
		public int NumFrequencies
		{
			get { return m_NumFrequencies; }
			set { m_NumFrequencies = value; }
		}

		private FFT m_FFT;
		private int m_FrameLength;
		private int m_NumAmplitudes;
		private int m_NumPhases;
	}
}
