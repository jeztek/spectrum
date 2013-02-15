using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Spectrum
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Complex
	{
		public Complex(double r, double i)
		{
			Real = r;
			Imag = i;
		}

		public Complex(Complex c)
		{
			Real = c.Real;
			Imag = c.Imag;
		}

		public double Magnitude()
		{
			return Math.Sqrt(Real * Real + Imag * Imag);
		}

		public double Angle()
		{
			return Math.Atan2(Imag, Real);
		}

		public static Complex operator +(Complex a)
		{
			return a;
		}

		public static Complex operator -(Complex a)
		{
			a.Real = -a.Real;
			a.Imag = -a.Imag;
			return a;
		}

		public static Complex operator +(Complex a, double f)
		{
			a.Real = (double)(a.Real + f);
			return a;
		}

		public static Complex operator +(double f, Complex a)
		{
			a.Real = (double)(a.Real + f);
			return a;
		}

		public static Complex operator +(Complex a, Complex b)
		{
			a.Real = a.Real + b.Real;
			a.Imag = a.Imag + b.Imag;
			return a;
		}

		public static Complex operator -(Complex a, double f)
		{
			a.Real = (double)(a.Real - f);
			return a;
		}

		public static Complex operator -(double f, Complex a)
		{
			a.Real = (float)(f - a.Real);
			a.Imag = (float)(0.0 - a.Imag);
			return a;
		}

		public static Complex operator -(Complex a, Complex b)
		{
			a.Real = a.Real - b.Real;
			a.Imag = a.Imag - b.Imag;
			return a;
		}

		public static Complex operator *(Complex a, double f)
		{
			a.Real = (double)(a.Real * f);
			a.Imag = (double)(a.Imag * f);
			return a;
		}

		public static Complex operator *(double f, Complex a)
		{
			a.Real = (double)(a.Real * f);
			a.Imag = (double)(a.Imag * f);

			return a;
		}

		public static Complex operator *(Complex a, Complex b)
		{
			double x = a.Real, y = a.Imag;
			double u = b.Real, v = b.Imag;
			a.Real = (double)(x * u - y * v);
			a.Imag = (double)(x * v + y * u);
			return a;
		}

		public static Complex operator /(Complex a, double f)
		{
			a.Real = (double)(a.Real / f);
			a.Imag = (double)(a.Imag / f);
			return a;
		}

		public static Complex operator /(Complex a, Complex b)
		{
			double x = a.Real, y = a.Imag;
			double u = b.Real, v = b.Imag;
			double denom = u * u + v * v;
			a.Real = (double)((x * u + y * v) / denom);
			a.Imag = (double)((y * u - x * v) / denom);
			return a;
		}
		
		public double Real;
		public double Imag;
	}
}
