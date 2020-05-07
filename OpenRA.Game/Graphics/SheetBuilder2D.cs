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
using OpenRA.FileFormats;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{

	public sealed class SheetBuilder2D : IDisposable
	{
		public readonly SheetType SheetStoreType;
		public readonly List<Sheet2D> sheetsOnCpu = new List<Sheet2D>();
		readonly Func<Sheet2D> allocateSheetDelegate;

		public int TextureArrayIndex = 0;

		Sheet2D currentSheet2D;
		TextureChannel channel;
		int rowHeight = 0;
		int2 p;
		private TextureArray textureArray;

		public Sheet2D CreateNewSheet(SheetType type, int sheetSize)
		{
			return new Sheet2D(type, new Size(sheetSize, sheetSize), textureArray, TextureArrayIndex);
		}
		public Sheet2D CreateNewSheet(SheetType type, int w, int h)
		{
			return new Sheet2D(type, new Size(w, h), textureArray, TextureArrayIndex);
		}
		public SheetBuilder2D(SheetType t)
			: this(t, Game.Settings.Graphics.SheetSize) { }

		public SheetBuilder2D(SheetType t, int sheetSize)
		{
			channel = t == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
			SheetStoreType = t;
			currentSheet2D = CreateNewSheet(t, sheetSize);
			sheetsOnCpu.Add(currentSheet2D);
		}

		public SheetBuilder2D(SheetType t, int w, int h)
		{
			channel = t == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
			SheetStoreType = t;
			currentSheet2D = CreateNewSheet(t, w, h);
			sheetsOnCpu.Add(currentSheet2D);
		}

		public SheetBuilder2D(SheetType t, Func<Sheet2D> allocateSheetDelegate)
		{
			channel = t == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
			SheetStoreType = t;
			currentSheet2D = allocateSheetDelegate();
			sheetsOnCpu.Add(currentSheet2D);
			this.allocateSheetDelegate = allocateSheetDelegate;
		}
		/// <summary>
		/// ДОбавляет байты из ISpriteFrame класса в массив байтов текстуры
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public Sprite Add(ISpriteFrame frame) 
		{ 
			return Add(frame.Data, frame.Size, 0, frame.Offset); 
		}
		public Sprite Add(byte[] src, Size size) 
		{ 
			return Add(src, size, 0, float3.Zero); 
		}

		/// <summary>
		/// Добавляет байты в массив байтов текстуры, которая привязан к данному SheetBuilder.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="size"></param>
		/// <param name="zRamp"></param>
		/// <param name="spriteOffset"></param>
		/// <returns></returns>
		public Sprite Add(byte[] src, Size size, float zRamp, float3 spriteOffset)
		{
			// Don't bother allocating empty sprites
			if (size.Width == 0 || size.Height == 0)
				return new Sprite(currentSheet2D, Rectangle.Empty, 0, spriteOffset, channel, BlendMode.Alpha);

			Sprite rect = Allocate(size, zRamp, spriteOffset);
			Util.FastCopyIntoChannel(rect, src);
			currentSheet2D.CommitBufferedData();
			return rect;
		}
		public Sprite AddRGBA(byte[] src, Size size)
		{
			// Don't bother allocating empty sprites
			if (size.Width == 0 || size.Height == 0)
				return new Sprite(currentSheet2D, Rectangle.Empty, 0, float3.Zero, channel, BlendMode.Alpha);

			var rect = Allocate(size, 0, float3.Zero);
			Util.FastCopyIntoRGBA(rect, src);
			currentSheet2D.CommitBufferedData();
			return rect;
		}
		public Sprite Add(Png src)
		{
			var rect = Allocate(new Size(src.Width, src.Height));
			Util.FastCopyIntoSprite(rect, src);
			currentSheet2D.CommitBufferedData();
			return rect;
		}

		public Sprite Add(Size size, byte paletteIndex)
		{
			var data = new byte[size.Width * size.Height];
			for (var i = 0; i < data.Length; i++)
				data[i] = paletteIndex;

			return Add(data, size);
		}

		TextureChannel? NextChannel(TextureChannel t)
		{
			var nextChannel = (int)t + (int)SheetStoreType;
			if (nextChannel > (int)TextureChannel.Alpha)
				return null;

			return (TextureChannel)nextChannel;
		}
		int NextSheet()
		{
			TextureArrayIndex += 1;
			textureArray.CapacityReserved = TextureArrayIndex;

			return TextureArrayIndex;
		}

		public Sprite Allocate(Size imageSize)
		{ 
			return Allocate(imageSize, 0, float3.Zero);
		}

		/// <summary>
		/// Резервирует место под SPrite в текстуре opengl
		/// Определяет в какой канал текстуры будут записаны байты
		/// </summary>
		/// <param name="imageSize"></param>
		/// <param name="zRamp"></param>
		/// <param name="spriteOffset"></param>
		/// <returns></returns>
		public Sprite Allocate(Size imageSize, float zRamp, float3 spriteOffset)
		{

			//используется одномерный массив. Поэтому все разбито по Height(row) и смещение внутри row.
			if (imageSize.Width + p.X > currentSheet2D.Size.Width) //если дошли до смещения равного ширине картинки, то сбрасываем смещение до 0 и делаем переход на высоту равную предыдущей высоте.
			{
				p = new int2(0, p.Y + rowHeight);
				rowHeight = imageSize.Height;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (p.Y + imageSize.Height > currentSheet2D.Size.Height) //если вышли за пределы высоты в одном канале, то переходим в другой канал. и скидываем p=int2.Zero
			{
				//когда дошли по конца массива, то переходи в следующий Sheet , а не в канал.

				//var next = NextChannel(channel);

				//if (next == null)
				//{
				//	NextSheet();
				//}
				//if (next == null) //если закончились каналы внутри текстуры , то создаем новый Sheet.
				//{

				//	NextSheet();
				//	currentSheet2D.ReleaseBuffer();
				//	currentSheet2D = CreateNewSheet(SheetStoreType, Game.Settings.Graphics.SheetSize); //унаследует TextureArrayIndex в новый Sheet2D
				//	sheetsOnCpu.Add(currentSheet2D);
				//	channel = SheetStoreType == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
				//}
				//else
				//	channel = next.Value;
			
				currentSheet2D.ReleaseBuffer();
				textureArray = currentSheet2D.texture; // присваиваем текстуру от первого sheet ,так как текстуры имеютс вязь sheet<->texture
				NextSheet();
			
				currentSheet2D = CreateNewSheet(SheetStoreType, Game.Settings.Graphics.SheetSize); //унаследует TextureArrayIndex в новый Sheet2D
				sheetsOnCpu.Add(currentSheet2D);
				channel = SheetStoreType == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA; // пишем теперь всегда в канал Red


				rowHeight = imageSize.Height;
				p = int2.Zero;
			}

			var rect = new Sprite(currentSheet2D, new Rectangle(p.X, p.Y, imageSize.Width, imageSize.Height), zRamp, spriteOffset, channel, BlendMode.Alpha);
			p += new int2(imageSize.Width, 0); // увеливаем свещение на ширину картинки у p.X параметра

			return rect;
		}

		public Sheet2D Current { get { return currentSheet2D; } }
		public TextureChannel CurrentChannelInSheet { get { return channel; } }
		public IEnumerable<Sheet2D> AllSheets { get { return sheetsOnCpu; } }

		public void Dispose()
		{
			currentSheet2D.Dispose();
		}
	}
}
