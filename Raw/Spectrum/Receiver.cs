using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spectrum
{
	public abstract class Receiver
	{
		public abstract bool StartReceiving(Action<IntPtr, int> receiveHandler);
	}
}
