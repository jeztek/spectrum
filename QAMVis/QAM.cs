using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace QAMVis
{
	public class Constellation
	{
		public struct Point
		{
			public Point(float mag, float phase)
			{
				m_Magnitude = mag;
				m_Phase = phase;
			}

			private float m_Magnitude;
			public float Magnitude
			{
				get { return m_Magnitude; }
				set { m_Magnitude = value; }
			}

			private float m_Phase;
			public float Phase
			{
				get { return m_Phase; }
				set { m_Phase = value; }
			}
		}

		public Constellation()
		{

		}

		// TODO: Subclasses can do this much more efficiently, since they know the layout.
		public int FindSymbol(float mag, float phase)
		{
			float shortestDist = 100000.0f;
			int shortestSymbol = 0;
			for (int i = 0; i < m_Points.Count; i++)
			{
				Point point = m_Points[i];
				float dist = (float)Math.Sqrt((mag * mag + point.Magnitude * point.Magnitude) - (2.0f * mag * point.Magnitude * (float)Math.Cos(phase - point.Phase)));
				if (dist < shortestDist)
				{
					shortestSymbol = i;
					shortestDist = dist;
				}
			}
			return shortestSymbol;
		}

		protected List<Point> m_Points = new List<Point>();
		public List<Point> Points
		{
			get { return m_Points; }
			set { m_Points = value; }
		}

		public int NumSymbols
		{
			get { return m_Points.Count; }
		}
	}

	public class CircularConstellation : Constellation
	{
		public CircularConstellation(int numAmplitudes, int numPhases)
		{
			float baseAmplitude = 1.0f / (float)numAmplitudes;
			float phaseDist = 1.0f / (numPhases);
			float phaseOffset = phaseDist * 0.5f;
			for (int a = 0; a < numAmplitudes; a++)
			{
				for (int p = 0; p < numPhases; p++)
				{
					float amplitude = numAmplitudes == 1 ? 1.0f : baseAmplitude + ((float)a / (float)(numAmplitudes - 1)) * (1.0f - baseAmplitude);
					float phase = ((phaseOffset + phaseDist * (float)p) * 2.0f - 1.0f) * (float)Math.PI;
					m_Points.Add(new Point(amplitude, phase));
				}
			}
		}
	}

	public class RectangularConstellation : Constellation
	{
		public RectangularConstellation(int numAmplitudes, int numPhases)
		{
			float ampDist = 1.0f / (numAmplitudes);
			float ampOffset = ampDist * 0.5f;
			float phaseDist = 1.0f / (numPhases);
			float phaseOffset = phaseDist * 0.5f;
			for (int a = 0; a < numAmplitudes; a++)
			{
				for (int p = 0; p < numPhases; p++)
				{
					float x = ((ampOffset + ampDist * (float)a) * 2.0f - 1.0f);
					float y = ((phaseOffset + phaseDist * (float)p) * 2.0f - 1.0f);
					float phase = (float)Math.Atan2(y, x);
					float amplitude = (float)Math.Sqrt(x * x + y * y);
					m_Points.Add(new Point(amplitude, phase));
				}
			}
		}
	}

	public class QAM
	{
		public QAM(Constellation constellation, int frequencies, int frameLength)
		{
			m_Constellation = constellation;
			m_NumFrequencies = frequencies;
			m_FrameLength = frameLength;
			m_FFT = new FFT(frameLength);
		}

		public float[] Modulate(List<uint> symbols)
		{
			int subSymbolBits = (int)Math.Log(m_Constellation.NumSymbols, 2);
			float[] result = new float[m_FrameLength];
			float nrm = 1.0f / (float)m_NumFrequencies;
			for (int f = 0; f < Math.Min(symbols.Count, m_NumFrequencies); f++)
			{
                int subSymbol = (int)symbols[f];
				Constellation.Point point = m_Constellation.Points[subSymbol];
				float amplitude = point.Magnitude;
				float phase = point.Phase;
				for (int i = 0; i < m_FrameLength; i++)
				{
					float t = (float)i / (float)(m_FrameLength);
					result[i] += (float)Math.Sin(t * Math.PI * 2.0f * ((float)f + 1.0f) + phase) * amplitude * nrm;
				}
			}
			return result;
		}

		public List<uint> Demodulate(float[] data)
		{
			float nrm = (float)m_NumFrequencies;
			int subSymbolBits = (int)Math.Log(m_Constellation.NumSymbols, 2);
			List<FFT.Complex> coeffs = m_FFT.DFT(data, nrm).ToList();
			coeffs = coeffs.OrderBy(x => -x.Magnitude).ToList();
			coeffs = coeffs.GetRange(0, m_NumFrequencies);
			coeffs = coeffs.OrderBy(x => x.freq).ToList();
			List<uint> resultSymbols = new List<uint>();
			float magNrm = 2.0f / (float)data.Length;
			for (int f = 0; f < m_NumFrequencies; f++)
			{
				uint subSymbol = (uint)m_Constellation.FindSymbol(coeffs[f].Magnitude * magNrm, coeffs[f].Angle);
                resultSymbols.Add(subSymbol);
			}
            return resultSymbols;
			/*
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
			 */
			return resultSymbols;
		}



		public int NumSymbols
		{
			get
			{
				return m_Constellation.NumSymbols;
			}
		}

		private int m_NumFrequencies;
		public int NumFrequencies
		{
			get { return m_NumFrequencies; }
			set { m_NumFrequencies = value; }
		}

		private Constellation m_Constellation;
		private FFT m_FFT;
		private int m_FrameLength;
	}
}
