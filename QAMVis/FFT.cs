using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QAMVis
{
	public class FFT
	{
		public struct Complex
		{
			public Complex(int f, double r, double i)
			{
				freq = f;
				real = r;
				imag = i;
			}

			public float Angle
			{
				get
				{
					return (float)Math.Atan2(real, imag);
				}
			}

			public float Magnitude
			{
				get
				{
					return (float)Math.Sqrt(real * real + imag * imag);
				}
			}

			public int freq;
			public double real;
			public double imag;
		}

		public FFT(int length)
		{
			m_SinTable = new double[length / 2][];
			m_CosTable = new double[length / 2][];
			for (int c = 0; c < length / 2; c++)
			{
				double[] sinTable = new double[length];
				double[] cosTable = new double[length];
				for (int x = 0; x < length; x++)
				{
					double t = (double)x / (double)length;
					sinTable[x] = Math.Sin(t * Math.PI * 2.0 * (double)c);
					cosTable[x] = Math.Cos(t * Math.PI * 2.0 * (double)c);
				}
				m_SinTable[c] = sinTable;
				m_CosTable[c] = cosTable;
			}
		}

		public Complex[] DFT(float[] values, float nrm)
		{
			Complex[] coeffs = new Complex[values.Length / 2];
			for (int c = 0; c < coeffs.Length; c++)
			{
				coeffs[c].freq = c;
				for (int x = 0; x < values.Length; x++)
				{
					double t = (double)x / (double)values.Length;
					coeffs[c].real += m_CosTable[c][x] * values[x] * nrm;
					coeffs[c].imag += m_SinTable[c][x] * values[x] * nrm;
				}
			}
			return coeffs;
		}

		private double[][] m_SinTable;
		private double[][] m_CosTable;
	}
}
