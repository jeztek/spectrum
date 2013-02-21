using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spectrum
{
	public abstract class Transmitter
	{
		public abstract bool StartTransmitting(Func<IntPtr, int, int> sendHandler);
	}
}
