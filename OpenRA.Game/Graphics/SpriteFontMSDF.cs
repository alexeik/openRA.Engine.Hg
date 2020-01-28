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

		float deviceScale;

		public SpriteFontMSDF(string name, byte[] data, int size, float scale, SheetBuilder builder)
		{
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

			if (size <= 24) // пытается, подобрать size? чтобы в строку влезло 24 символа.
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
			// Offset from the baseline position to the top-left of the glyph for rendering
			location += new float2(0, size);
			if (text.Contains("Settings"))
			{

			}
			var p =new float2( location.X ,location.Y);

			foreach (var s in text)
			{
				if (s == '\n')
				{
					location += new float2(0, size);
					p = location;
					continue;
				}

				GlyphInfo gli = glyphs[Pair.New(s, c)];
			
				//тут левый верхний спрайта - в каждом спрайте есть смещение самой буквы от краев спрайта. 

				float3 tempXY = new float2();//= new float2((int)Math.Round(p.X * deviceScale + glp.Offset.X, 0) / deviceScale, p.Y - 15 - (Math.Max(glp.fg.Size.Height - glp.fg.Bearing.Y, 1)) / deviceScale); ;
											 //= new float2((int)Math.Round(p.X * deviceScale + glp.Offset.X, 0) / deviceScale, p.Y-15- (Math.Max(glp.fg.Size.Height - glp.fg.Bearing.Y,1)) / deviceScale);

				// float3 tempXY = new float2((int)Math.Round(p.X * deviceScale + g.Offset.X, 0) / deviceScale, p.Y + g.Offset.Y / deviceScale);

				float onepixel = 1f / 32f;

				if (gli.Sprite != null)
				{
					gli.Sprite.SpriteType = 1; // 1 будет для FontMSDF
					gli.Sprite.SpriteArrayNum = (int)s;

					if ((int)s <= 64)
					{
						tempXY = new float2((int)Math.Round(p.X * deviceScale + gli.fg.Bearing.X, 0) , (p.Y - (gli.fg.Size.Height - gli.fg.Bearing.Y) - gli.fg.Bearing.Y));
						gli.Sprite.Left = 6 * onepixel;
						gli.Sprite.Bottom = 5 * onepixel - (gli.fg.Size.Height - gli.fg.Bearing.Y) * onepixel;
						gli.Sprite.Top = gli.Sprite.Bottom + gli.fg.Size.Height * onepixel;
						gli.Sprite.Right = gli.Sprite.Left + gli.fg.Size.Width * onepixel + 1 * onepixel;
					}
					if ((int)s >= 97)
					{
						if (gli.fg.Size.Height - gli.fg.Bearing.Y > 1)
						{
							tempXY = new float2((int)Math.Round(p.X * deviceScale + gli.Offset.X, 0) / deviceScale, (p.Y - (gli.fg.Size.Height - gli.fg.Bearing.Y) - gli.fg.Bearing.Y) / deviceScale);
						}
						else if (gli.fg.Size.Height - gli.fg.Bearing.Y == 1)
						{
							tempXY = new float2((int)Math.Round(p.X * deviceScale + gli.Offset.X, 0) / deviceScale, (p.Y - (gli.fg.Size.Height - gli.fg.Bearing.Y) - gli.fg.Bearing.Y) / deviceScale);
						}
						else
						{
							tempXY = new float2((int)Math.Round(p.X * deviceScale + gli.Offset.X, 0) / deviceScale, (p.Y - (gli.fg.Size.Height - gli.fg.Bearing.Y) - gli.fg.Bearing.Y) / deviceScale);
						}
						if ((int)s == 105 || (int)s == 108 || (int)s == 116)
						{
							tempXY = new float2((int)Math.Round(p.X * deviceScale + gli.Offset.X, 0) / deviceScale, (p.Y - (gli.fg.Size.Height - gli.fg.Bearing.Y) - gli.fg.Bearing.Y) / deviceScale);
						}
						
						gli.Sprite.Left = 4 * onepixel ;
						gli.Sprite.Bottom = 5 * onepixel - (gli.fg.Size.Height - gli.fg.Bearing.Y) *onepixel;
						gli.Sprite.Right = gli.Sprite.Left+ gli.fg.Size.Width * onepixel + 2 * onepixel;
						gli.Sprite.Top = gli.Sprite.Bottom + gli.fg.Size.Height * onepixel;
						//Game.Renderer.FontSpriteRenderer.DrawSprite(glp.Sprite, tempXY, 0, new float3(27, 27,27));
					}
					if ((int)s >= 65 && (int)s <= 90)
					{
						tempXY = new float2((int)Math.Round(p.X * deviceScale + gli.Offset.X, 0) / deviceScale, (p.Y - (gli.fg.Size.Height - gli.fg.Bearing.Y) - gli.fg.Bearing.Y) / deviceScale);
						
						gli.Sprite.Left = 3 * onepixel;
						gli.Sprite.Bottom = 5 * onepixel - (gli.fg.Size.Height - gli.fg.Bearing.Y) * onepixel;
						gli.Sprite.Top = gli.Sprite.Bottom+ gli.fg.Size.Height * onepixel;
						gli.Sprite.Right = gli.Sprite.Left + 3* onepixel + gli.fg.Size.Width * onepixel;
						//Game.Renderer.FontSpriteRenderer.DrawSprite(glp.Sprite, tempXY, 0, new float3(27, 27, 27));
					}
					//Game.Renderer.FontSpriteRenderer.DrawSprite(gli.Sprite, tempXY, 0, new float3(gli.fg.Size.Width , gli.fg.Size.Height , gli.fg.Size.Height));
					//Game.Renderer.FontSpriteRenderer.DrawSprite(gli.Sprite, tempXY, 0, new float3(font.Height, font.Height, font.Height));
					//Game.Renderer.FontSpriteRenderer.DrawSprite(gli.Sprite, tempXY, 0, gli.Sprite.Size);
					tempXY = new float2((int)Math.Round(p.X * deviceScale , 0) / deviceScale, (p.Y - (gli.fg.Size.Height)+ (gli.fg.Size.Height - gli.fg.Bearing.Y)) / deviceScale);
					float coof = gli.Sprite.Size.X / gli.Sprite.Size.Y;
					
					Game.Renderer.FontSpriteRenderer.DrawSprite(gli.Sprite, tempXY, 0, new float3(gli.Sprite.Size.X, gli.Sprite.Size.Y, 0));
				}
				//p += new float2(gli.Advance-1 / deviceScale, 0);
				p += new float2(gli.Advance / deviceScale, 0);
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

			var dest = s.Sheet.GetData();
			var destStride = s.Sheet.Size.Width * 4;

			for (var j = 0; j < s.Size.Y; j++)
			{
				for (var i = 0; i < s.Size.X; i++)
				{
					// тут происходит копирование байтов из глифа в "p", а после в dest общий массив текстуры.
					var p = glyph.Data[j * glyph.Size.Width + i];
					if (p != 0)
					{
						var q = destStride * (j + s.Bounds.Top) + 4 * (i + s.Bounds.Left);
						var pmc = Util.PremultiplyAlpha(Color.FromArgb(p, c.Second));

						dest[q] = pmc.B;
						dest[q + 1] = pmc.G;
						dest[q + 2] = pmc.R;
						dest[q + 3] = pmc.A;
					}
				}
			}

			s.Sheet.CommitBufferedData();

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
