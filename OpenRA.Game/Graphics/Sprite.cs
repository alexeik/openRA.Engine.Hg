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
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class Sprite
	{
		public readonly Rectangle Bounds;
		public readonly Sheet Sheet;
		public readonly Sheet2D Sheet2D;
		public readonly BlendMode BlendMode;
		public readonly TextureChannel Channel;
		public readonly float ZRamp;
		public  float3 Size;
		public readonly float3 Offset;
		public readonly float3 FractionalOffset;
		/// <summary>
		/// ���������� ������ ��������. vTexCoord
		/// </summary>
		public float Top, Left, Bottom, Right;
		public int SpriteType;
		public int SpriteArrayNum;
		public int Rotate;
		public bool Stretched;
		public int TextureArrayIndex=-1;

		public Sprite(Sheet sheet, Rectangle bounds, TextureChannel channel)
			: this(sheet, bounds, 0, float2.Zero, channel) { }

		public Sprite(Sheet sheet, Rectangle bounds, float zRamp, float3 offset, TextureChannel channel, BlendMode blendMode = BlendMode.Alpha)
		{
			Sheet = sheet;
			Bounds = bounds;
			Offset = offset;
			ZRamp = zRamp;
			Channel = channel;
			Size = new float3(bounds.Size.Width, bounds.Size.Height, bounds.Size.Height * zRamp);
			BlendMode = blendMode;
			FractionalOffset = Size.Z != 0 ? offset / Size :
				new float3(offset.X / Size.X, offset.Y / Size.Y, 0);

			Left = (float)Math.Min(bounds.Left, bounds.Right) / sheet.Size.Width;
			Top = (float)Math.Min(bounds.Top, bounds.Bottom) / sheet.Size.Height;
			Right = (float)Math.Max(bounds.Left, bounds.Right) / sheet.Size.Width;
			Bottom = (float)Math.Max(bounds.Top, bounds.Bottom) / sheet.Size.Height;
		}
		public Sprite(Sheet2D sheet, Rectangle bounds, TextureChannel channel)
			: this(sheet, bounds, 0, float2.Zero, channel) { }

		public Sprite(Sheet2D sheet, Rectangle bounds, float zRamp, float3 offset, TextureChannel channel, BlendMode blendMode = BlendMode.Alpha)
		{
			TextureArrayIndex = sheet.textureArrayIndex;
			Sheet2D = sheet;
			Bounds = bounds;
			Offset = offset;
			ZRamp = zRamp;
			Channel = channel;
			Size = new float3(bounds.Size.Width, bounds.Size.Height, bounds.Size.Height * zRamp);
			BlendMode = blendMode;
			FractionalOffset = Size.Z != 0 ? offset / Size :
				new float3(offset.X / Size.X, offset.Y / Size.Y, 0);

			Left = (float)Math.Min(bounds.Left, bounds.Right) / sheet.Size.Width;
			Top = (float)Math.Min(bounds.Top, bounds.Bottom) / sheet.Size.Height;
			Right = (float)Math.Max(bounds.Left, bounds.Right) / sheet.Size.Width;
			Bottom = (float)Math.Max(bounds.Top, bounds.Bottom) / sheet.Size.Height;
		}
	}

	public class SpriteWithSecondaryData : Sprite
	{
		public readonly Sheet SecondarySheet;
		public readonly Sheet2D SecondarySheet2D;
		public readonly Rectangle SecondaryBounds;
		public readonly TextureChannel SecondaryChannel;
		public readonly float SecondaryTop, SecondaryLeft, SecondaryBottom, SecondaryRight;

		public SpriteWithSecondaryData(Sprite s, Sheet secondarySheet, Rectangle secondaryBounds, TextureChannel secondaryChannel)
			: base(s.Sheet, s.Bounds, s.ZRamp, s.Offset, s.Channel, s.BlendMode)
		{
			SecondarySheet = secondarySheet;
			SecondaryBounds = secondaryBounds;
			SecondaryChannel = secondaryChannel;
			SecondaryLeft = (float)Math.Min(secondaryBounds.Left, secondaryBounds.Right) / s.Sheet.Size.Width;
			SecondaryTop = (float)Math.Min(secondaryBounds.Top, secondaryBounds.Bottom) / s.Sheet.Size.Height;
			SecondaryRight = (float)Math.Max(secondaryBounds.Left, secondaryBounds.Right) / s.Sheet.Size.Width;
			SecondaryBottom = (float)Math.Max(secondaryBounds.Top, secondaryBounds.Bottom) / s.Sheet.Size.Height;
		}
	}

	public enum TextureChannel : byte
	{
		Red = 0,
		Green = 1,
		Blue = 2,
		Alpha = 3,
		RGBA = 4
	}
}
