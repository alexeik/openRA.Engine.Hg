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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Graphics
{
	public sealed class SpriteFontMSDF : IDisposable
	{
		public int TopOffset { get; private set; }
		readonly int size;
		readonly SheetBuilder builder;
		readonly Func<string, float> lineWidth;
		readonly IFont font;
		readonly Cache<Pair<char, Color>, GlyphInfo> glyphs;
		public FontMSDF Mfont;

		float deviceScale;

		public SpriteFontMSDF(string name, byte[] data, int size, float scale, SheetBuilder builder)
		{
			Mfont = new FontMSDF();
			Mfont.LoadFontTexturesAsPng(name);

			if (builder.Type != SheetType.BGRA)
				throw new ArgumentException("The sheet builder must create BGRA sheets.", "builder");

			deviceScale = scale;
			this.size = size;
			this.builder = builder;
			Console.WriteLine("Font {0} : {1}", name, size);
			font = Game.Renderer.CreateFont(data); // FreeTypeFont библиотека создает IFont структуру, где есть байтовое представление символа.
			font.SetSize(size, deviceScale);

			glyphs = new Cache<Pair<char, Color>, GlyphInfo>(CreateGlyph, Pair<char, Color>.EqualityComparer);

			// PERF: Cache these delegates for Measure calls.
			Func<char, float> characterWidth = character => glyphs[Pair.New(character, Color.White)].Advance; // это одна функция с аргументом character, а телом из glyphs[Pair.New(character, Color.White)].Advance

			lineWidth = line => line.Sum(characterWidth) / deviceScale; // тоже функция как и characterWidth

			//if (size <= 24) // пытается, подобрать size? чтобы в строку влезло 24 символа.

			PrecacheColor(Color.White, name);

			TopOffset = size - font.Height;
		}

		public void SetScale(float scale)
		{
			deviceScale = scale;

			font.SetSize(size, scale);
			glyphs.Clear();

			TopOffset = size - font.Height;
		}

		/// <summary>
		/// Этим методом генерируется обращение к каждой букве в шрифте, что заставляет SpriteFont сгенерировать картинки букв.
		/// </summary>
		/// <param name="c">буква.</param>
		/// <param name="name">название шрифта.</param>
		void PrecacheColor(Color c, string name)
		{
			using (new PerfTimer("PrecacheColor {0} {1}px {2}".F(name, size, c)))

				for (var n = (char)0x20; n < (char)0x7f; n++)
				{
					if (glyphs[Pair.New(n, c)] == null)
					{
						throw new InvalidOperationException();
					}
				}
		}

		public void DrawText(string text, float2 location, Color c)
		{
			if (text.Contains("Cyril"))
			{

			}
			// Offset from the baseline position to the top-left of the glyph for rendering
			location += new float2(0, size);
			var p = new float2(location.X, location.Y);

			foreach (var s in text)
			{
				float scale = 1;
				scale = (FontMSDF.Size * font.Height) / 18f;
				if (s == '\n')
				{
					location += new float2(0, size);
					p = location;
					continue;
				}

				GlyphInfo gli = glyphs[Pair.New(s, c)];

				//размер квада буквы равен размеру текстуры * масштаб. 

				float3 tempXY = new float2();

				if (gli.Sprite != null)
				{
					gli.Sprite.SpriteType = 1; // 1 будет для FontMSDF
					gli.Sprite.SpriteArrayNum = (int)s;


					//scale = (FontMSDF.Size * gli.fg.Size.Height) / 18f;
					float scalex = (FontMSDF.Size * gli.fg.Size.Width) / 18f;
					//scale = (FontMSDF.Size / this.size);
					///scale = 64;
					//tempXY = new float2((int)Math.Round(p.X -6 , 0) / deviceScale, (p.Y - scale+6) / deviceScale);
					tempXY = new float2((p.X - gli.Offset.X / 64f - 5) / deviceScale, (p.Y - scale - gli.Offset.Y *5/ 64f + 5) / deviceScale); // -5 -5 потому, что -translate 5 5 был задан в msdfgen
																																			 // деление на 64 ,это приведение к шкале глифа SDF, который 64 на 64
																																			 //tempXY = new float2((int)Math.Round(p.X , 0) / deviceScale, (p.Y ) / deviceScale);

					gli.Sprite.Left = 0f;
					gli.Sprite.Bottom = 0f;
					gli.Sprite.Top = 1f;
					gli.Sprite.Right = 1f;
					//Sprite ничего не значит тут. Он нужен лишь для определения размера полигона
					Game.Renderer.FontSpriteRenderer.SetFontMSDF(Mfont.Texture); //assign texture arg for shader text.vert.

					Game.Renderer.FontSpriteRenderer.DrawTextSprite(gli.Sprite, tempXY, 0, new float3(scale, scale, 0));


				}
				float coof = (FontMSDF.Size * font.Height) / 18f;
				if (s == 'm')
				{
					//gli.Advance = 15;
				}
				p += new float2((gli.Advance ) / deviceScale, 0);

			}
			Game.Renderer.FontSpriteRenderer.SetTextColor(c);

			// + добавить юсда передачу параметра в шейдер , цвет шрифта.
		}

		public void DrawTextWithContrast(string text, float2 location, Color fg, Color bg, int offset)
		{
			if (offset > 0)
			{
				//DrawText(text, location + new float2(-offset / deviceScale, 0), bg);
				//DrawText(text, location + new float2(offset / deviceScale, 0), bg);
				//DrawText(text, location + new float2(0, -offset / deviceScale), bg);
				//DrawText(text, location + new float2(0, offset / deviceScale), bg);
			}

			DrawText(text, location, fg);
		}

		public void DrawTextWithContrast(string text, float2 location, Color fg, Color bgDark, Color bgLight, int offset)
		{
			DrawTextWithContrast(text, location, fg, GetContrastColor(fg, bgDark, bgLight), offset);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bg, int offset)
		{
			if (offset != 0)
				//DrawText(text, location + new float2(offset, offset), bg);

				DrawText(text, location, fg);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bgDark, Color bgLight, int offset)
		{
			DrawTextWithShadow(text, location, fg, GetContrastColor(fg, bgDark, bgLight), offset);
		}

		public int2 Measure(string text)
		{
			if (string.IsNullOrEmpty(text))
				return new int2(0, size);

			var lines = text.Split('\n');
			return new int2((int)Math.Ceiling(lines.Max(lineWidth)), lines.Length * size);
		}

		/// <summary>
		/// Запускается из PrecacheColor.
		/// </summary>
		/// <param name="c">Буква шрифта.</param>
		/// <returns>Один визуальный образ буквы.</returns>
		GlyphInfo CreateGlyph(Pair<char, Color> c)
		{
			FontGlyph glyph = font.CreateGlyph(c.First);

			if (glyph.Data == null)
			{
				return new GlyphInfo
				{
					Sprite = null,
					Advance = 0,
					Offset = int2.Zero
				};
			}

			var s = builder.Allocate(glyph.Size);
			var g = new GlyphInfo
			{
				Sprite = s,
				Advance = glyph.Advance,
				Offset = glyph.Bearing,
				fg = glyph
			};

			//var dest = s.Sheet.GetData();
			//var destStride = s.Sheet.Size.Width * 4;

			//for (var j = 0; j < s.Size.Y; j++)
			//{
			//	for (var i = 0; i < s.Size.X; i++)
			//	{
			//		// тут происходит копирование байтов из глифа в "p", а после в dest общий массив текстуры.
			//		var p = glyph.Data[j * glyph.Size.Width + i];
			//		if (p != 0)
			//		{
			//			var q = destStride * (j + s.Bounds.Top) + 4 * (i + s.Bounds.Left);
			//			var pmc = Util.PremultiplyAlpha(Color.FromArgb(p, c.Second));

			//			dest[q] = pmc.B;
			//			dest[q + 1] = pmc.G;
			//			dest[q + 2] = pmc.R;
			//			dest[q + 3] = pmc.A;
			//		}
			//	}
			//}

			//s.Sheet.CommitBufferedData();

			return g;
		}

		static Color GetContrastColor(Color fgColor, Color bgDark, Color bgLight)
		{
			return fgColor == Color.White || fgColor.GetBrightness() > 0.33 ? bgDark : bgLight;
		}

		public void Dispose()
		{
			font.Dispose();
		}
	}

	class GlyphInfo
	{
		public float Advance;
		public int2 Offset;
		public Sprite Sprite;
		public FontGlyph fg;
	}
}
