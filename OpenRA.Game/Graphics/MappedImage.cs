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
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	class MappedImage
	{
		public  Rectangle rect = Rectangle.Empty;
		public readonly string Src;
		public int Rotate;
		public bool Stretched;
		public int OffsetTop, OffsetLeft; //�������� ������ opengl ��������.
		public MappedImage(string defaultSrc, MiniYaml info)
		{
			FieldLoader.LoadField(this, "rect", info.Value);
			FieldLoader.Load(this, info);
			if (Src == null)
				Src = defaultSrc;

			if (info.Nodes.Count > 0)
			{
				foreach (MiniYamlNode my in info.Nodes)
				{
					if (my.Key == "rotate")
						Rotate = Convert.ToInt32(my.Value.Value);

					if (my.Key == "stretched")
						Stretched = Convert.ToBoolean(my.Value.Value);
				}
			}
		}

		public Sprite GetImage(Sheet s)
		{
			return new Sprite(s, rect, TextureChannel.RGBA);
		}
		public Sprite GetImage(Sheet2D s)
		{
			rect.X += OffsetLeft;
			rect.Y += OffsetTop;
			return new Sprite(s, rect, TextureChannel.RGBA);
		}
		public Sprite GetImage(Sheet s, Rectangle r)
		{
			return new Sprite(s, r, TextureChannel.Red);
		}

		public MiniYaml Save(string defaultSrc)
		{
			var root = new List<MiniYamlNode>();
			if (defaultSrc != Src)
				root.Add(new MiniYamlNode("Src", Src));

			return new MiniYaml(FieldSaver.FormatValue(this, GetType().GetField("rect")), root);
		}
	}
}
