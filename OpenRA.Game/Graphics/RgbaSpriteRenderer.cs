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
using System;

namespace OpenRA.Graphics
{
	public class RgbaSpriteRenderer
	{
		readonly SpriteRenderer parent;
		string defaultpalette = "";
		public PaletteReference pr;

		public RgbaSpriteRenderer(SpriteRenderer parent)
		{
			this.parent = parent;
		}

		public void Setup()
		{
			if (Game.ModData.Manifest.ChromeDefaultPalette.Length > 0)
			{
				defaultpalette = Game.ModData.Manifest.ChromeDefaultPalette[0];
				pr = Game.worldRenderer.Palette(defaultpalette);
			}
		}

		public void DrawSprite(Sprite s, float3 location, float3 size)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, location, 0, size);
		}

		public void DrawSprite(Sprite s, float3 location)
		{
			if (s.Channel != TextureChannel.RGBA && pr != null)
			{
				parent.DrawSprite(s, location, pr, s.Size);
				return;
			}
				if (s.Channel != TextureChannel.RGBA && pr == null)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite and ChromeDefaultPalette(mod.yaml) is null.");

			parent.DrawSprite(s, location, 0, s.Size);
		}

		public void DrawSprite(Sprite s, float3 a, float3 b, float3 c, float3 d)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, a, b, c, d);
		}
	}
}
