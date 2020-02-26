#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Primitives
{
	public struct WPos : IScriptBindable, IEquatable<WPos>
	{
		public readonly int X, Y, Z;

		public WPos(int x, int y, int z) { X = x; Y = y; Z = z; }
		public WPos(WDist x, WDist y, WDist z) { X = x.Length; Y = y.Length; Z = z.Length; }

		public static readonly WPos Zero = new WPos(0, 0, 0);

		public static explicit operator WVec(WPos a) { return new WVec(a.X, a.Y, a.Z); }

		public static WPos operator +(WPos a, WVec b) { return new WPos(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
		public static WPos operator -(WPos a, WVec b) { return new WPos(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
		public static WVec operator -(WPos a, WPos b) { return new WVec(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }

		public static bool operator ==(WPos me, WPos other) { return me.X == other.X && me.Y == other.Y && me.Z == other.Z; }
		public static bool operator !=(WPos me, WPos other) { return !(me == other); }

		/// <summary>
		/// Returns the linear interpolation between points 'a' and 'b'
		/// </summary>
		public static WPos Lerp(WPos a, WPos b, int mul, int div) { return a + (b - a) * mul / div; }

		/// <summary>
		/// Returns the linear interpolation between points 'a' and 'b'
		/// </summary>
		public static WPos Lerp(WPos a, WPos b, long mul, long div)
		{
			// The intermediate variables may need more precision than
			// an int can provide, so we can't use WPos.
			var x = (int)(a.X + (b.X - a.X) * mul / div);
			var y = (int)(a.Y + (b.Y - a.Y) * mul / div);
			var z = (int)(a.Z + (b.Z - a.Z) * mul / div);

			return new WPos(x, y, z);
		}

		public static WPos LerpQuadratic(WPos a, WPos b, WAngle pitch, int mul, int div)
		{
			// Start with a linear lerp between the points
			var ret = Lerp(a, b, mul, div);

			if (pitch.Angle == 0)
				return ret;

			// Add an additional quadratic variation to height
			// Uses decimal to avoid integer overflow
			var offset = (decimal)(b - a).Length * pitch.Tan() * mul * (div - mul) / (1024 * div * div);
			var clampedOffset = (int)(offset + (decimal)ret.Z).Clamp<decimal>((decimal)int.MinValue, (decimal)int.MaxValue);

			return new WPos(ret.X, ret.Y, clampedOffset);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode(); }

		public bool Equals(WPos other) { return other == this; }
		public override bool Equals(object obj) { return obj is WPos && Equals((WPos)obj); }

		public override string ToString() { return X + "," + Y + "," + Z; }

	
	}

	public static class IEnumerableExtensions
	{
		public static WPos Average(this IEnumerable<WPos> source)
		{
			var length = source.Count();
			if (length == 0)
				return WPos.Zero;

			var x = 0L;
			var y = 0L;
			var z = 0L;
			foreach (var pos in source)
			{
				x += pos.X;
				y += pos.Y;
				z += pos.Z;
			}

			x /= length;
			y /= length;
			z /= length;

			return new WPos((int)x, (int)y, (int)z);
		}
	}
}
