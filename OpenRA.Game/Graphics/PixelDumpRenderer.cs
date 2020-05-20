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

using OpenRA.Platforms.Default;
using OpenRA.Primitives;
using System;

namespace OpenRA.Graphics
{
	public class PixelDumpRenderer : SpriteRenderer
	{
		readonly SpriteRenderer parent;
		string defaultpalette = "";
		public PaletteReference pr;
		public FrameBuffer fb;
		public bool fbcreated = false;
	

		public PixelDumpRenderer(string renderID, Renderer r, Shader sh ) : base("", r, sh)
		{
			
		}

		public void Setup()
		{
			
			if (fbcreated)
			{
				return;
			}
			fb = Game.Renderer.Context.CreateFrameBuffer(new Size(2048, 2048));
			fbcreated = true;
			SetViewportParams(new Size(2048, 2048), 0f, 0f, 1f, int2.Zero);
		}
		public void Setup(Size s)
		{

			if (fbcreated)
			{
				return;
			}
		
			fb = Game.Renderer.Context.CreateFrameBuffer(new Size(s.Width, s.Height));
			fbcreated = true;
			SetViewportParams(new Size(s.Width, s.Height), 0f, 0f, 1f, int2.Zero);
		}

		public void DrawSprite(Sprite s, float3 location, float3 size,PaletteReference p)
		{
			//if (s.Channel != TextureChannel.RGBA)
			//	throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			base.DrawSprite(s, location, p, size);
		}
		public void DrawSprite(Sprite s, float3 location, float3 size)
		{
			//if (s.Channel != TextureChannel.RGBA)
			//	throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			base.DrawSprite(s, location, 0, size);
		}

		public void DrawSprite(Sprite s, float3 location)
		{
			
			base.DrawSprite(s, location, 0, s.Size);
		}

		public void DrawSprite(Sprite s, float3 a, float3 b, float3 c, float3 d)
		{
			if (s.Channel != TextureChannel.RGBA)
				throw new InvalidOperationException("DrawRGBASprite requires a RGBA sprite.");

			parent.DrawSprite(s, a, b, c, d);
		}
	}
}
