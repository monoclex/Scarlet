using System;
using System.Collections.Generic;

namespace Scarlet
{
	public static class Config
	{
		public static Range Scale { get; set; } = 1..4;

		public static IEnumerable<int> EnumerateScale { get; set; } = EnumerateScaleImpl();

		public static int ClampToScale(int number)
			=> Math.Clamp(number, Scale.Start.GetOffset(0), Scale.End.GetOffset(0));

		private static IEnumerable<int> EnumerateScaleImpl()
		{
			for (var i = Scale.Start.GetOffset(0); i < Scale.End.GetOffset(0); i++)
			{
				yield return i;
			}
		}
	}
}