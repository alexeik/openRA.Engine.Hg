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
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	[Serializable]
	public class SheetOverflowException : Exception
	{
		public SheetOverflowException(string message)
			: base(message) { }
	}

	// The enum values indicate the number of channels used by the type
	// They are not arbitrary IDs!
	public enum SheetType
	{
		Indexed = 1,
		BGRA = 4,
	}

	public sealed class SheetBuilder : IDisposable
	{
		public readonly SheetType Type;
		public readonly List<Sheet> sheets = new List<Sheet>();
		readonly Func<Sheet> allocateSheetDelegate;

		Sheet currentSheet;
		TextureChannel channel;
		int rowHeight = 0;
		int2 p;

		public static Sheet AllocateSheet(SheetType type, int sheetSize)
		{
			return new Sheet(type, new Size(sheetSize, sheetSize));
		}
		public static Sheet AllocateSheet(SheetType type, int w, int h)
		{
			return new Sheet(type, new Size(w, h));
		}
		public SheetBuilder(SheetType t)
			: this(t, Game.Settings.Graphics.SheetSize) { }

		public SheetBuilder(SheetType t, int sheetSize)
			: this(t, () => AllocateSheet(t, sheetSize)) { }

		public SheetBuilder(SheetType t, int w, int h)
			: this(t, () => AllocateSheet(t, w, h)) { }

		public SheetBuilder(SheetType t, Func<Sheet> allocateSheetDelegate)
		{
			channel = t == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
			Type = t;
			currentSheet = allocateSheetDelegate();
			sheets.Add(currentSheet);
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
				return new Sprite(currentSheet, Rectangle.Empty, 0, spriteOffset, channel, BlendMode.Alpha);

			Sprite rect = Allocate(size, zRamp, spriteOffset);
			Util.FastCopyIntoChannel(rect, src);
			currentSheet.CommitBufferedData();
			return rect;
		}
		public Sprite AddRGBA(byte[] src, Size size)
		{
			// Don't bother allocating empty sprites
			if (size.Width == 0 || size.Height == 0)
				return new Sprite(currentSheet, Rectangle.Empty, 0, float3.Zero, channel, BlendMode.Alpha);

			var rect = Allocate(size, 0, float3.Zero);
			Util.FastCopyIntoRGBA(rect, src);
			currentSheet.CommitBufferedData();
			return rect;
		}
		public Sprite Add(Png src)
		{
			var rect = Allocate(new Size(src.Width, src.Height));
			Util.FastCopyIntoSprite(rect, src);
			currentSheet.CommitBufferedData();
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
			var nextChannel = (int)t + (int)Type;
			if (nextChannel > (int)TextureChannel.Alpha)
				return null;

			return (TextureChannel)nextChannel;
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
			if (imageSize.Width + p.X > currentSheet.Size.Width) //если дошли до смещения равного ширине картинки, то сбрасываем смещение до 0 и делаем переход на высоту равную предыдущей высоте.
			{
				p = new int2(0, p.Y + rowHeight);
				rowHeight = imageSize.Height;
			}

			if (imageSize.Height > rowHeight)
				rowHeight = imageSize.Height;

			if (p.Y + imageSize.Height > currentSheet.Size.Height) //если вышли за пределы высоты в одном канале, то переходим в другой канал. и скидываем p=int2.Zero
			{
				var next = NextChannel(channel);
				if (next == null) //если закончились каналы внутри текстуры , то создаем новый Sheet.
				{
					currentSheet.ReleaseBuffer();
					currentSheet = allocateSheetDelegate();
					sheets.Add(currentSheet);
					channel = Type == SheetType.Indexed ? TextureChannel.Red : TextureChannel.RGBA;
				}
				else
					channel = next.Value;

				rowHeight = imageSize.Height;
				p = int2.Zero;
			}

			var rect = new Sprite(currentSheet, new Rectangle(p.X, p.Y, imageSize.Width, imageSize.Height), zRamp, spriteOffset, channel, BlendMode.Alpha);
			p += new int2(imageSize.Width, 0);

			return rect;
		}

		public Sheet Current { get { return currentSheet; } }
		public TextureChannel CurrentChannel { get { return channel; } }
		public IEnumerable<Sheet> AllSheets { get { return sheets; } }

		public void Dispose()
		{
			foreach (var sheet in sheets)
				sheet.Dispose();
			sheets.Clear();
		}
	}
}
