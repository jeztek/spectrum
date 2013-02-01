using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QAMVis
{
	public class Noise
	{
		public static float[] GenerateNoise(int length, float scale)
		{
			float[] noise = new float[length];
			Random random = new Random();
			for (int i = 0; i < length; i++)
			{
				noise[i] = ((float)random.NextDouble() * 2.0f - 1.0f) * scale;
			}
			return noise;
		}
	}
}
