using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QAMVis
{
	public class SymbolStream
	{
		public SymbolStream(int numSymbols)
		{
			m_NumSymbols = numSymbols;
		}

		public byte[] DecodeSymbols(List<uint> symbols)
		{
			int bitsPerSymbol = (int)Math.Log(NumSymbols, 2);
			int symbolIndex = 0;
			int bitsRemaining = bitsPerSymbol;
			uint currentSymbol = symbols[0];
			List<byte> resultStream = new List<byte>();
			while (symbolIndex < symbols.Count)
			{
				uint currentByte = 0;
				int currentBits = 0;
				while (currentBits < 8)
				{
					int needBits = 8 - currentBits;
					int useBits = Math.Min(bitsRemaining, needBits);
					uint useMask = (1U << useBits) - 1U;
					currentByte |= (currentSymbol & useMask) << currentBits;
					currentBits += useBits;
					bitsRemaining -= useBits;
					currentSymbol >>= useBits;
					if (bitsRemaining == 0)
					{
						symbolIndex++;
						if (symbolIndex < symbols.Count)
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

		public List<uint> EncodeSymbols(byte[] input)
		{
			int bitsPerSymbol = (int)Math.Log(NumSymbols, 2);
			int byteIndex = 0;
			int bitsRemaining = 8;
			uint currentByte = input[0];
			List<uint> resultStream = new List<uint>();
			while (byteIndex < input.Length)
			{
				uint currentSymbol = 0;
				int currentBits = 0;
				while (currentBits < bitsPerSymbol)
				{
					int needBits = bitsPerSymbol - currentBits;
					int useBits = Math.Min(bitsRemaining, needBits);
					uint useMask = (1U << useBits) - 1U;
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
			return resultStream;
		}

		private int m_NumSymbols;
		public int NumSymbols
		{
			get { return m_NumSymbols; }
			set { m_NumSymbols = value; }
		}
	}
}
