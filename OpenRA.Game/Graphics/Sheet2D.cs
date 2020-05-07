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
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public sealed class Sheet2D : IDisposable
	{
		bool dirty;
		bool releaseBufferOnCommit;
		public TextureArray texture;
		byte[] data;
		public int textureArrayIndex;
		public readonly Size Size;
		public readonly SheetType Type;

		/// <summary>
		/// Дает ссылку на byte[] data , куда с помощью FastCopyIntoChannel копируются данные внутри SheetBuilder метод Add() 
		/// </summary>
		/// <returns></returns>
		public byte[] GetData()
		{
			CreateBuffer();
			return data;
		}

		public bool Buffered { get { return data != null || texture == null; } }

		public Sheet2D(SheetType type, Size size, TextureArray texture, int TextureArrayIndex)
		{
			Type = type;
			Size = size;
			this.texture = texture;
			textureArrayIndex = TextureArrayIndex;
		}

		public Sheet2D(SheetType type, TextureArray texture, int TextureArrayIndex)
		{
			Type = type;
			this.texture = texture;
			Size = texture.Size;
			textureArrayIndex = TextureArrayIndex;
		}

		/// <summary>
		/// Сделано, только для потоков байтов типа <see cref="Png"/> .
		/// </summary>
		/// <param name="type">тип листа хранящего байты из потока</param>
		/// <param name="stream">поток байтов.</param>
		public Sheet2D(SheetType type, Stream stream)
		{
			var png = new Png(stream);
			Size = new Size(png.Width, png.Height);
			data = new byte[4 * Size.Width * Size.Height];
			Util.FastCopyIntoSprite(new Sprite(this, new Rectangle(0, 0, png.Width, png.Height), TextureChannel.Red), png);

			Type = type;
			ReleaseBuffer();
		}

		/// <summary>
		/// Создает текстуру в OPenGL для данного Sheet
		/// </summary>
		/// <returns></returns>
		public ITexture AssignOrGetOrSetDataGLTexture()
		{
			if (texture == null)
			{
				texture = Game.Renderer.Context.CreateTexture2DArray();
				texture.scaleFilter = TextureScaleFilter.Nearest;
				texture.SetEmpty(Size.Width, Size.Width, 10);
				dirty = true;
			}

			if (data != null && dirty)
			{
				//Flush CPU data to GPU data(OpenGL Texture)
				texture.SetData(data, Size.Width, Size.Width, textureArrayIndex);
				dirty = false;
				if (releaseBufferOnCommit)
					data = null;
			}

			return texture;
		}

		public Png AsPng()
		{
			return new Png(GetData(), Size.Width, Size.Height);
		}

		public Png AsPng(TextureChannel channel, IPalette pal)
		{
			var d = GetData();
			var plane = new byte[Size.Width * Size.Height];
			var dataStride = 4 * Size.Width;
			var channelOffset = (int)channel;

			for (var y = 0; y < Size.Height; y++)
				for (var x = 0; x < Size.Width; x++)
					plane[y * Size.Width + x] = d[y * dataStride + channelOffset + 4 * x];

			var palColors = new Color[Palette.Size];
			for (var i = 0; i < Palette.Size; i++)
				palColors[i] = pal.GetColor(i);

			return new Png(plane, Size.Width, Size.Height, palColors);
		}

		public void CreateBuffer()
		{
			if (data != null)
				return;
			if (texture == null)
				data = new byte[4 * Size.Width * Size.Height];
			else
				data = texture.GetData(textureArrayIndex);
			releaseBufferOnCommit = false;
		}

		public void CommitBufferedData()
		{
			if (!Buffered)
				throw new InvalidOperationException(
					"This sheet is unbuffered. You cannot call CommitBufferedData on an unbuffered sheet. " +
					"If you need to completely replace the texture data you should set data into the texture directly. " +
					"If you need to make only small changes to the texture data consider creating a buffered sheet instead.");

			dirty = true;
		}

		public void ReleaseBuffer()
		{
			if (!Buffered)
				return;
			dirty = true;
			releaseBufferOnCommit = true;

			// Commit data from the buffer to the texture, allowing the buffer to be released and reclaimed by GC.
			if (Game.Renderer != null)
				AssignOrGetOrSetDataGLTexture();
		}

		public void Dispose()
		{
			if (texture != null)
			{
				texture.Dispose();
				texture = null;
			}
		}
	}
}
