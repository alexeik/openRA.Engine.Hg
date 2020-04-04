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

using OpenRA.Primitives;
using System.Runtime.InteropServices;

namespace OpenRA.Graphics
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vertex
	{
		public readonly float X, Y, Z, S, T, U, V, P, C, Drawmode, Option, ColorTypeValue1, ColorTypeValue2, ColorTypeValue3, ColorTypeValue4;

		public Vertex(float3 xyz, float s, float t, float u, float v, float p, float c, float drawMode, float Option, float colorTypeValue1, float colorTypeValue2, float colorTypeValue3, float colorTypeValue4)
			: this(xyz.X, xyz.Y, xyz.Z, s, t, u, v, p, c, drawMode, Option, colorTypeValue1, colorTypeValue2,  colorTypeValue3,  colorTypeValue4) { }

		public Vertex(float x, float y, float z, float s, float t, float u, float v, float p, float c, float drawMode,float option, float colorTypeValue1, float colorTypeValue2, float colorTypeValue3, float colorTypeValue4)
		{
			//!!! при расширении в вертексе через  glVertexAttribPointer, нужно его обязательно создать прям тут, чтобы сходилось число позиции через glVertexAttribPointer и public readonly float
			X = x; Y = y; Z = z; // shader input in vec3 aVertexPosition;
			S = s; T = t; //aVertexTexCoord
			U = u; V = v; //aVertexTexCoord
			P = p; C = c; //aVertexTexMetadata
			Drawmode = drawMode; //aVertexTexMetadata +1 free
			Option = option;
			ColorTypeValue1 = colorTypeValue1;ColorTypeValue2 = colorTypeValue2; ColorTypeValue3 = colorTypeValue3;ColorTypeValue4 = colorTypeValue4; //aVertexColorInfo
		}
	}
	public struct Vertex2
	{
		public readonly float X, Y, Z, S, T, U, V, P, C, Drawmode, Option, Option2, ColorTypeValue1, ColorTypeValue2, ColorTypeValue3, ColorTypeValue4;

		public Vertex2(float3 xyz, float ShaderID, float CurrentFrame, float TotalFrames, float iTime, float TotalTime, float iResolutionX, float iResolutionY, float TextureInputSlot,float TextureStoreChannel, float SpriteUVCoordX, float SpriteUVCoodY, float PaletteIndex, float temp2)
			: this(xyz.X, xyz.Y, xyz.Z, ShaderID, CurrentFrame, TotalFrames, iTime, TotalTime, iResolutionX, iResolutionY, TextureInputSlot, TextureStoreChannel, SpriteUVCoordX, SpriteUVCoodY, PaletteIndex, temp2) { }

		public Vertex2(float x, float y, float z, float s, float t, float u, float v, float p, float c, float drawMode, float option,float option2, float colorTypeValue1, float colorTypeValue2, float colorTypeValue3, float colorTypeValue4)
		{
			//!!! при расширении в вертексе через  glVertexAttribPointer, нужно его обязательно создать прям тут, чтобы сходилось число позиции через glVertexAttribPointer и public readonly float
			X = x; Y = y; Z = z; // shader input in vec3 aVertexPosition;
			S = s; T = t; //aVertexTexCoord
			U = u; V = v; //aVertexTexCoord
			P = p; C = c; //aVertexTexMetadata
			Drawmode = drawMode; //aVertexTexMetadata +1 free
			Option = option; //vTextureInputSlot
			Option2 = option2; //vTextureStoreChannel
			ColorTypeValue1 = colorTypeValue1; ColorTypeValue2 = colorTypeValue2; // vec2 vSpriteUVCoords
			ColorTypeValue3 = colorTypeValue3; ColorTypeValue4 = colorTypeValue4; //vPaletteIndex , temp
		}
	}
}
