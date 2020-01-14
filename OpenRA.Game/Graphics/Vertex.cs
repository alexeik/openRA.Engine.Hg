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

using System.Runtime.InteropServices;

namespace OpenRA.Graphics
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vertex
	{
		public readonly float X, Y, Z, S, T, U, V, P, Drawmode, ColorTypeValue1, ColorTypeValue2, ColorTypeValue3, ColorTypeValue4;

		public Vertex(float3 xyz, float s, float t, float u, float v, float p, float drawMode, float colorTypeValue1, float colorTypeValue2, float colorTypeValue3, float colorTypeValue4)
			: this(xyz.X, xyz.Y, xyz.Z, s, t, u, v, p, drawMode, colorTypeValue1, colorTypeValue2,  colorTypeValue3,  colorTypeValue4) { }

		public Vertex(float x, float y, float z, float s, float t, float u, float v, float p, float drawMode, float colorTypeValue1, float colorTypeValue2, float colorTypeValue3, float colorTypeValue4)
		{
			X = x; Y = y; Z = z;
			S = s; T = t;
			U = u; V = v;
			P = p; Drawmode = drawMode;
			ColorTypeValue1 = colorTypeValue1;ColorTypeValue2 = colorTypeValue2; ColorTypeValue3 = colorTypeValue3;ColorTypeValue4 = colorTypeValue4;
		}
	}
}
