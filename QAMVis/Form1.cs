﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QAMVis
{
    public partial class Form1 : Form
    {
		private Random m_Random = new Random();
		private Font m_Font = new System.Drawing.Font("Arial", 10, FontStyle.Regular);

        public Form1()
        {
            InitializeComponent();
        }

		private void DrawGraph(Graphics g, float x, float y, float[] values, float xScale, float yScale, Pen pen, Pen pen2, int wordLength, int activeWord, out int height)
		{
			int minPos = 100000;
			int maxPos = 0;
			for (int i = 1; i < values.Length; i++)
			{
				int yPos = (int)(values[i] * yScale);
				minPos = Math.Min(minPos, yPos);
				maxPos = Math.Max(maxPos, yPos);
			}
			for (int i = 1; i < values.Length; i++)
			{
				Pen usePen = (i / wordLength) % 2 == 0 ? pen : pen2;
				if ((i / wordLength) == activeWord)
				{
					usePen = Pens.Yellow;
				}
				g.DrawLine(usePen, new PointF(x + (float)(i - 1) * xScale, y + values[i - 1] * yScale - minPos), new PointF(x + (float)i * xScale, y + values[i] * yScale - minPos));
			}
			height = maxPos - minPos;
		}

        private void Form1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
			comboBox1.SelectedIndex = 1;
			timer1.Interval = 33;
			timer1.Enabled = true;
        }

		private void timer1_Tick (object sender, EventArgs e)
		{
			paintControl1.Invalidate ();
		}

		private void DrawConstellation(Graphics g, Constellation c, float cx, float cy, float scale, float markerScale)
		{
			g.DrawLine(Pens.Gray, cx + -scale, cy + -scale, cx + scale, cy + -scale);
			g.DrawLine(Pens.Gray, cx + scale, cy + -scale, cx + scale, cy + scale);
			g.DrawLine(Pens.Gray, cx + scale, cy + scale, cx + -scale, cy + scale);
			g.DrawLine(Pens.Gray, cx + -scale, cy + scale, cx + -scale, cy + -scale);
			g.DrawLine(Pens.Gray, cx, cy + -scale, cx, cy + scale);
			g.DrawLine(Pens.Gray, cx + -scale, cy, cx + scale, cy);
			foreach (Constellation.Point p in c.Points)
			{
				float x = (float)Math.Cos(p.Phase) * p.Magnitude * scale;
				float y = (float)Math.Sin(p.Phase) * p.Magnitude * scale;
				g.DrawEllipse(Pens.Aqua, cx + x - markerScale * 0.5f, cy + y - markerScale * 0.5f, markerScale, markerScale);
			}
		}

		private void paintControl1_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Color.Black);
			int frameLength = 64;
			int numFreqs = (int)numericUpDown4.Value;
			int numAmps = (int)Math.Pow(2.0f, (float)numericUpDown1.Value);
			int numPhases = (int)Math.Pow(2.0f, (float)numericUpDown2.Value);
			Constellation constellation = comboBox1.SelectedIndex == 0 ? (Constellation)new CircularConstellation(numAmps, numPhases) : (Constellation)new RectangularConstellation(numAmps, numPhases);
			SymbolStream symbolStream = new SymbolStream(constellation.NumSymbols);
			QAM modulator = new QAM(constellation, numFreqs, frameLength);

			string inString = "Hello World!";
			byte[] inBytes = Encoding.ASCII.GetBytes(inString);
			List<uint> inSymbols = symbolStream.EncodeSymbols(inBytes);
			List<float> stream = new List<float>();
			int symbolIndex = 0;
			m_NumFrames = 0;

			while (symbolIndex < inSymbols.Count)
			{
				int symbolCount = Math.Min(inSymbols.Count - symbolIndex, numFreqs);
				List<uint> subSymbols = inSymbols.GetRange(symbolIndex, symbolCount);
				subSymbols.Add(0);
				float[] fragment = modulator.Modulate(subSymbols);
				stream.AddRange(fragment);
				symbolIndex += symbolCount;
				m_NumFrames++;
			}

			float noiseScale = (float)numericUpDown3.Value / 20.0f;
			float[] noise = Noise.GenerateNoise(stream.Count, (float)numericUpDown3.Value / 20.0f);
			List<float> noisyStream = new List<float>();
			int shifter = (int)(Math.Round((m_Random.NextDouble() * 2.0f - 1.0f) * (float)numericUpDown5.Value));
			for (int i = 0; i < stream.Count; i++)
			{
				noisyStream.Add(noise[i] + stream[Math.Max(0, Math.Min(stream.Count - 1, i + shifter))]);
			}

			List<uint> outSymbols = new List<uint>();
			for (int i = 0; i < noisyStream.Count; i += frameLength)
			{
				float[] fragment = noisyStream.GetRange(i, frameLength).ToArray();
				List<uint> subSymbols = modulator.Demodulate(fragment);
				outSymbols.AddRange(subSymbols);
			}

			byte[] outBytes = symbolStream.DecodeSymbols(outSymbols);
			string outString = Encoding.ASCII.GetString(outBytes);

			float curY = 100.0f;
			float streamScale = 840.0f / stream.Count;
			float streamSize = frameLength * streamScale;
			float symbolsPerByte = m_NumFrames / (float)inBytes.Length;
			for (int i = 0; i < inString.Length; i++)
			{
				float symbol = (float)i * symbolsPerByte;
				g.DrawString(inString[i].ToString(), m_Font, Brushes.White, new PointF(20.0f + symbol * streamSize, curY));
			}
			curY += 16.0f;

			for (int f = 0; f < m_NumFrames; f++)
			{
				int symbolStart = f * numFreqs;
				int symbolEnd = Math.Min(inSymbols.Count - 1, symbolStart + numFreqs);
				float frameStart = streamSize * f;
				float Y = 0.0f;
				float X = 0.0f;
				for (int i = symbolStart; i < symbolEnd; i++)
				{
					string str = inSymbols[i].ToString();
					SizeF strSize = g.MeasureString(str, m_Font);
					if (X >= streamSize - 10.0f)
					{
						X = 0.0f;
						Y += strSize.Height + 2;
					}
					g.DrawString(str, m_Font, Brushes.White, new PointF(20.0f + frameStart + X, curY + Y));
					X += strSize.Width;
				}
			}

			int graphMax = 0;
			curY += 26.0f;
			m_SourceY = curY;
			m_SourceX = 20.0f;
			m_SourceWidth = streamScale * (float)stream.Count;
			int useFrame = Math.Min(m_NumFrames - 1, m_ActiveFrame);
			DrawGraph(g, 20, curY, stream.ToArray(), streamScale, 32.0f, Pens.Aqua, Pens.SteelBlue, frameLength, useFrame, out graphMax);
			m_SourceHeight = graphMax + 10.0f;
			curY += graphMax + 10.0f;
			DrawGraph(g, 20, curY, noise, streamScale, 32.0f, Pens.Red, Pens.Red, 1, -1, out graphMax);
			curY += 32.0f * noiseScale * 1.5f + 10.0f;
			DrawGraph(g, 20, curY, noisyStream.ToArray(), streamScale, 32.0f, Pens.Yellow, Pens.LightYellow, frameLength, -1, out graphMax);
			curY += graphMax + 10.0f;

			for (int f = 0; f < m_NumFrames; f++)
			{
				int symbolStart = f * numFreqs;
				int symbolEnd = Math.Min(inSymbols.Count - 1, symbolStart + numFreqs);
				float frameStart = streamSize * f;
				float Y = 0.0f;
				float X = 0.0f;
				for (int i = symbolStart; i < symbolEnd; i++)
				{
					string str = outSymbols[i].ToString();
					SizeF strSize = g.MeasureString(str, m_Font);
					if (X >= streamSize - 10.0f)
					{
						X = 0.0f;
						Y += strSize.Height + 2;
					}
					Brush useBrush = outSymbols[i] != inSymbols[i] ? Brushes.Red : new SolidBrush(Color.FromArgb(0, 255, 0));
					g.DrawString(str, m_Font, useBrush, new PointF(20.0f + frameStart + X, curY + Y));
					X += strSize.Width;
				}
			}
			curY += 16.0f;

			for (int i = 0; i < inString.Length; i++)
			{
				float symbol = (float)i * symbolsPerByte;
				Brush useBrush = outString[i] != inString[i] ? Brushes.Red : new SolidBrush(Color.FromArgb(0, 255, 0));
				g.DrawString(outString[i].ToString(), m_Font, useBrush, new PointF(20.0f + symbol * streamSize, curY));
			}
			curY += 16.0f;

			g.DrawString("amps", m_Font, Brushes.Yellow, new PointF(5, 25));
			g.DrawString("phases", m_Font, Brushes.Yellow, new PointF(65, 25));
			g.DrawString("freqs", m_Font, Brushes.Yellow, new PointF(125, 25));
			g.DrawString("noise", m_Font, Brushes.Yellow, new PointF(205, 25));
			g.DrawString(String.Format("QAM-{0} x{1}, {2} bits per frame, {3} message len", modulator.NumSymbols, modulator.NumFrequencies, Math.Log(Math.Pow(modulator.NumSymbols, modulator.NumFrequencies), 2), m_NumFrames), m_Font, Brushes.Yellow, new PointF(310, 25));

			float markerSize = constellation.NumSymbols > 64 ? 6 : 12;
			DrawConstellation(g, constellation, 200, 550, 150.0f, markerSize);

			{
				int symbolStart = useFrame * numFreqs;
				int symbolEnd = Math.Min(inSymbols.Count - 1, symbolStart + numFreqs);
				FFT fft = new FFT(frameLength);
				float[] data = noisyStream.GetRange(useFrame * frameLength, frameLength).ToArray();
				float nrm = (float)numFreqs;
				List<FFT.Complex> coeffs = fft.DFT(data, nrm).ToList();
				coeffs = coeffs.OrderBy(x => -x.Magnitude).ToList();
				coeffs = coeffs.GetRange(0, numFreqs);
				coeffs = coeffs.OrderBy(x => x.freq).ToList();
				float magNrm = 2.0f / (float)frameLength;
				List<uint> correctSymbols = inSymbols.GetRange(symbolStart, symbolEnd - symbolStart);
				for (int f = 0; f < correctSymbols.Count; f++)
				{
					uint subSymbol = (uint)constellation.FindSymbol(coeffs[f].Magnitude * magNrm, coeffs[f].Angle);
					float x = (float)Math.Cos(coeffs[f].Angle) * coeffs[f].Magnitude * magNrm * 150.0f;
					float y = (float)Math.Sin(coeffs[f].Angle) * coeffs[f].Magnitude * magNrm * 150.0f;
					x += 200.0f;
					y += 550.0f;
					Pen usePen = subSymbol != correctSymbols[f] ? Pens.Red : new Pen(Color.FromArgb(0, 255, 0));
					g.DrawLine(usePen, x - markerSize * 0.5f, y, x + markerSize * 0.5f, y);
					g.DrawLine(usePen, x, y - markerSize * 0.5f, x, y + markerSize * 0.5f);
				}
			}
		}

		private void paintControl1_MouseClick(object sender, MouseEventArgs e)
		{
			float mx = ((float)e.X - m_SourceX) / m_SourceWidth;
			float my = ((float)e.Y - m_SourceY) / m_SourceHeight;
			if (mx > 0.0f && my > 0.0f && mx < 1.0f && my < 1.0f)
			{
				m_ActiveFrame = (int)Math.Floor(mx * (float)m_NumFrames);
			}
		}

		private FFT m_FFT;
		private int m_NumFrames;
		private int m_ActiveFrame;
		private float m_SourceX;
		private float m_SourceY;
		private float m_SourceWidth;
		private float m_SourceHeight;
    }
}
