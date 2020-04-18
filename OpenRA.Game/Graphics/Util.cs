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
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public static class Util
	{
		// yes, our channel order is nuts.
		static readonly int[] ChannelMasks = { 2, 1, 0, 3 };

		public static void FastCreateQuad(Vertex[] vertices, float3 o, Sprite r, int2 samplers, float paletteTextureIndex, int nv, float3 size)
		{
			var b = new float3(o.X + size.X, o.Y, o.Z);
			var c = new float3(o.X + size.X, o.Y + size.Y, o.Z + size.Z);
			var d = new float3(o.X, o.Y + size.Y, o.Z + size.Z);
			FastCreateQuad(vertices, o, b, c, d, r, samplers, paletteTextureIndex, nv);
		}
		public static void FastCreateQuadImGui(Vertex[] vertices, float3 o, Sprite r, int2 samplers, float paletteTextureIndex, int nv, float3 size)
		{
			var b = new float3(o.X + size.X, o.Y, o.Z);
			var c = new float3(o.X + size.X, o.Y + size.Y, o.Z + size.Z);
			var d = new float3(o.X, o.Y + size.Y, o.Z + size.Z);
			FastCreateQuadImGui(vertices, o, b, c, d, r, samplers, paletteTextureIndex, nv);
		}
		public static void FastCreateQuad(Vertex[] vertices, float3 a, float3 b, float3 c, float3 d, Sprite r, int2 samplers, float paletteTextureIndex, int nv)
		{
			float sl = 0;
			float st = 0;
			float sr = 0;
			float sb = 0;

			// See shp.vert for documentation on the channel attribute format
			float ct1 = 0, ct2 = 0, ct3 = 0, ct4 = 0;

			// тут r трактуется как класс Sprite
			if (r.Channel == TextureChannel.RGBA)
			{
				ct1 = 4f; // тут нужно указать, в каком канале текстуры R,G,B зашиты данные о цвете. для RGBA текстуры нужно цифру 2, это в шейдере 
						  // укажет на использование 1.1.1.1 маски
			}
			else
			{
				ct1 = (byte)r.Channel; // тут нужно понять, в каком канале лежат данные о цвете, так как SheetBiulder распределяет данные картинок
									   // по R,G,B группам. Это сделано для картинок с палитрами. Так как их цвет занимает лишь 1 байт, вместо 4 байт.
									   // 1 в шейдере это R канал, 2 это RGBA, 3 = G , 5=B, 7=A
									   // R=0,G=1,B=2,A=3,RGBA=4 в r.Channel . В шейдере передалана функция связки масок, на точное соотвествие.
			}

			// var attribC = r.Channel == TextureChannel.RGBA ? 0x02 : ((byte)r.Channel) << 1 | 0x01;
			// attribC |= samplers.X << 6;
			ct2 = samplers.X;  // это потому что, выбор текстуры зависит от 0.0 чисел в шейдере в методе vec4 Sample()

			var ss = r as SpriteWithSecondaryData;

			// тут r трактуется уже как SpriteWithSecondaryData, если преобразование пройдет успешно! а если нет, то ss = null
			// используется для vDepthMask в шейдере и когда поставлен флаг ОтладкиГлубины SpriteRendere.SetDepthPreviewEnabled ставит у шейдера uniform EnableDepthPreview =true
			if (ss != null)
			{
				sl = ss.SecondaryLeft;
				st = ss.SecondaryTop;
				sr = ss.SecondaryRight;
				sb = ss.SecondaryBottom;

				// attribC |= ((byte)ss.SecondaryChannel) << 4 | 0x08;
				if (ss.SecondaryChannel == TextureChannel.RGBA)
				{
					ct3 = 4f;
				}
				else
				{
					ct3 = (byte)r.Channel;
				}

				// attribC |= samplers.Y << 9;
				ct4 = samplers.Y;
			}

			int drawmode = 0;

			// Как то передать режим в котором будет рисование.
			if (r.Channel == TextureChannel.RGBA)
			{
				// значит режим рисования RGBA из текстуры
				drawmode = 0;
			}
			else
			{
				drawmode = 1;

				// режим через палитру, в основном из канала R в текстуре идем к цвету в палитре

				// drawmode=2 ставится для рисования прямоугольников и т.п. в RgbaColorRenderer, так как у него свой VBO
			}
			if (r.SpriteType == 1)
			{
				paletteTextureIndex = r.SpriteArrayNum; // положим сюда цифру указывающую на спрайт внутри Texture2dArray в TextureFontMSDF параметра шейдера.
				drawmode = 3; // FontMSDF
			}
			if (r.SpriteType == 2)
			{
				drawmode = 6; // fill rect with rgba sprite

				int r1, t1;
				r1 = (int)((b.X - a.X) / r.Size.X);
				//b1 = r.Bottom + (b.X - a.X) / r.Size.X;
				t1 = (int)((c.Y - a.Y) / r.Size.Y);

				vertices[nv] = new Vertex(a, 0, 0, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				vertices[nv + 1] = new Vertex(b, r1, 0, sr, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				vertices[nv + 2] = new Vertex(c, r1, t1, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				vertices[nv + 3] = new Vertex(c, r1, t1, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				vertices[nv + 4] = new Vertex(d, 0, t1, sl, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				vertices[nv + 5] = new Vertex(a, 0, 0, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				return;
			}

			if (r.SpriteType == 3)
			{

				drawmode = 7; // fill rect with one channel sprite 

				float hlen, b1, vlen;
				int wk = (int)((b.X - a.X) / r.Size.X); //width koof
				int hk = (int)((c.Y - a.Y) / r.Size.Y); // height koof

				hlen = wk;
				vlen = hk;

				float top, right, bot, left;
				top = r.Top;
				right = r.Right;
				bot = r.Bottom;
				left = r.Left;

				float d1 = r.Right - r.Left;
				float d2 = r.Bottom - r.Top;

				//   top = r.Bottom;
				//bot = r.Top;
				//right = r.Left;
				//left = r.Right;
				if (r.Rotate > 0)
				{
					wk = (int)((b.X - a.X) / r.Size.X); //width koof
					hk = (int)((c.Y - a.Y) / r.Size.Y); // height koof
					if (vlen==0)
					{
						vlen = 1;
					}
														//left += d1;
														//right -= d2;
														//top += d2;
														//bot -= d2;
					vertices[nv] = new Vertex(a, 0, 0, sl, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 1] = new Vertex(b, hlen, 0, sr, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 2] = new Vertex(c, hlen, vlen, sr, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 3] = new Vertex(c, hlen, vlen, sr, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 4] = new Vertex(d, 0, vlen, sl, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 5] = new Vertex(a, 0, 0, sl, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
				}
				else
				
				{
					if (r.Stretched)
					{
						hlen = 1;
						vlen = 1;
					}
					vertices[nv] = new Vertex(a, 0, 0, sl, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 1] = new Vertex(b, hlen, 0, sr, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 2] = new Vertex(c, hlen, vlen, sr, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 3] = new Vertex(c, hlen, vlen, sr, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 4] = new Vertex(d, 0, vlen, sl, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
					vertices[nv + 5] = new Vertex(a, 0, 0, sl, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
				}
				//vertices[nv] = new Vertex(a, r.Left, r.Top, sl, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
				//vertices[nv + 1] = new Vertex(b, r.Right, r.Top, sr, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
				//vertices[nv + 2] = new Vertex(c, r.Right, r.Bottom, sr, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);

				//vertices[nv + 3] = new Vertex(c, r.Right, r.Bottom, sr, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
				//vertices[nv + 4] = new Vertex(d, r.Left, r.Bottom, sl, sb, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);
				//vertices[nv + 5] = new Vertex(a, r.Left, r.Top, sl, st, paletteTextureIndex, wk, drawmode, hk, ct1, ct2, ct3, ct4, left, top, right, bot);

				return;
			}

			if (r.SpriteType==4)
			{
				drawmode = 8;
				//vertices[nv] = new Vertex(a, a.X + 0, a.Y + 0, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				//vertices[nv + 1] = new Vertex(b, a.X + 1, a.Y + 0, sr, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				//vertices[nv + 2] = new Vertex(c, a.X + 1, a.Y + 1, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				//vertices[nv + 3] = new Vertex(c, a.X + 1, a.Y + 1, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				//vertices[nv + 4] = new Vertex(d, a.X + 0, a.Y + 1, sl, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				//vertices[nv + 5] = new Vertex(a, a.X + 0, a.Y + 0, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4, r.Left, r.Top, r.Right, r.Bottom);
				//return;
			}

			if (r.SpriteType==5) //dump texture to png
			{
				drawmode = 9;
			}
			// var fAttribC = (float)attribC;
			vertices[nv] = new Vertex(a, r.Left, r.Top, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 1] = new Vertex(b, r.Right, r.Top, sr, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 2] = new Vertex(c, r.Right, r.Bottom, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);

			vertices[nv + 3] = new Vertex(c, r.Right, r.Bottom, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 4] = new Vertex(d, r.Left, r.Bottom, sl, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 5] = new Vertex(a, r.Left, r.Top, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
		}

		public static void FastCreateQuadImGui(Vertex[] vertices, float3 a, float3 b, float3 c, float3 d, Sprite r, int2 samplers, float paletteTextureIndex, int nv)
		{
			float sl = 0;
			float st = 0;
			float sr = 0;
			float sb = 0;

			// See shp.vert for documentation on the channel attribute format
			float ct1 = 0, ct2 = 0, ct3 = 0, ct4 = 0;

			// тут r трактуется как класс Sprite
			if (r.Channel == TextureChannel.RGBA)
			{
				ct1 = 4f; // это потому что, выбор текстуры зависит от 0.0 чисел в шейдере в методе vec4 Sample()
			}
			else
			{
				ct1 = (byte)r.Channel;
			}

			// var attribC = r.Channel == TextureChannel.RGBA ? 0x02 : ((byte)r.Channel) << 1 | 0x01;
			// attribC |= samplers.X << 6;
			ct2 = samplers.X;

			var ss = r as SpriteWithSecondaryData;


			// используется для vDepthMask в шейдере и когда поставлен флаг ОтладкиГлубины SpriteRendere.SetDepthPreviewEnabled ставит у шейдера uniform EnableDepthPreview =true
			if (ss != null) // тут r трактуется уже как SpriteWithSecondaryData, если преобразование пройдет успешно=> а если нет, то ss = null
			{
				sl = ss.SecondaryLeft;
				st = ss.SecondaryTop;
				sr = ss.SecondaryRight;
				sb = ss.SecondaryBottom;

				// attribC |= ((byte)ss.SecondaryChannel) << 4 | 0x08;
				if (ss.SecondaryChannel == TextureChannel.RGBA)
				{
					ct3 = 4f;
				}
				else
				{
					ct3 = (byte)ss.SecondaryChannel;
				}

				// attribC |= samplers.Y << 9;
				ct4 = samplers.Y;
			}

			int drawmode = 0;

			// Как то передать режим в котором будет рисование.
			if (r.Channel == TextureChannel.RGBA)
			{
				// значит режим рисования RGBA из текстуры
				drawmode = 0;
			}
			else
			{
				drawmode = 1;

				// режим через палитру, в основном из канала R в текстуре идем к цвету в палитре

				// drawmode=2 ставится для рисования прямоугольников и т.п. в RgbaColorRenderer, так как у него свой VBO
			}
			if (r.SpriteType == 1)
			{
				paletteTextureIndex = r.SpriteArrayNum; // положим сюда цифру указывающую на спрайт внутри Texture2dArray в TextureFontMSDF параметра шейдера.
				drawmode = 3; // FontMSDF
			}

			drawmode = 5;

			vertices[nv] = new Vertex(a, r.Left, r.Top, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 1] = new Vertex(b, r.Right, r.Top, sr, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 2] = new Vertex(c, r.Right, r.Bottom, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 3] = new Vertex(c, r.Right, r.Bottom, sr, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 4] = new Vertex(d, r.Left, r.Bottom, sl, sb, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
			vertices[nv + 5] = new Vertex(a, r.Left, r.Top, sl, st, paletteTextureIndex, 0, drawmode, 0, ct1, ct2, ct3, ct4);
		}

		/// <summary>
		/// ДОполняет данные в data у Sheet новыми данными из разных картинок.
		/// Учитывая в какой канала записать R,G,B,A , так как эти картинки занимают только 1 байт на 1 пиксель.
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="src"></param>
		public static void FastCopyIntoChannel(Sprite dest, byte[] src)
		{
			var data = dest.Sheet.GetData();
			var srcStride = dest.Bounds.Width; //ширина спрайта в пикселях, после которой делается destSkip
			var destStride = dest.Sheet.Size.Width * 4; //ширина текстуры в байтах, с переводом пикселей в байты. Берется ширина, потому что предпологается, что ширина=высоте, так как степень двойки.
			var destOffset = destStride * dest.Bounds.Top + dest.Bounds.Left * 4 + ChannelMasks[(int)dest.Channel]; // 
			var destSkip = destStride - 4 * srcStride;
			var height = dest.Bounds.Height;

			var srcOffset = 0;
			for (var j = 0; j < height; j++)
			{
				for (var i = 0; i < srcStride; i++, srcOffset++) // после прогона в количестве равному ширине в пикселях
				{
					data[destOffset] = src[srcOffset]; //пишется в data по смещению destOffset
					destOffset += 4;
				}

				destOffset += destSkip;
			}
		}
		public static void FastCopyIntoRGBA(Sprite dest, byte[] src)
		{
			var data = dest.Sheet.GetData();
			var srcStride = dest.Bounds.Width;
			var destStride = dest.Sheet.Size.Width * 4;
			var destOffset = destStride * dest.Bounds.Top + dest.Bounds.Left * 4 + ChannelMasks[(int)dest.Channel];
			var destSkip = destStride - 4 * srcStride;
			var height = dest.Bounds.Height;

			var srcOffset = 0;
			for (var j = 0; j < height * 4; j++)
			{
				for (var i = 0; i < srcStride; i++, srcOffset++)
				{
					data[srcOffset] = src[srcOffset];
					destOffset += 4;
				}

				destOffset += destSkip;
			}
		}

		public static void FastCopyIntoSprite(Sprite dest, Png src)
		{
			var destData = dest.Sheet.GetData();
			var destStride = dest.Sheet.Size.Width;
			var width = dest.Bounds.Width;
			var height = dest.Bounds.Height;

			unsafe
			{
				// Cast the data to an int array so we can copy the src data directly
				fixed (byte* bd = &destData[0])
				{
					var data = (int*)bd;
					var x = dest.Bounds.Left;
					var y = dest.Bounds.Top;

					var k = 0;
					for (var j = 0; j < height; j++)
					{
						for (var i = 0; i < width; i++)
						{
							Color cc;
							if (src.Palette == null)
							{
								var r = src.Data[k++];
								var g = src.Data[k++];
								var b = src.Data[k++];
								var a = src.Data[k++];
								cc = Color.FromArgb(a, r, g, b);
							}
							else
								cc = src.Palette[src.Data[k++]];

							data[(y + j) * destStride + x + i] = PremultiplyAlpha(cc).ToArgb();
						}
					}
				}
			}
		}

		public static Color PremultiplyAlpha(Color c)
		{
			if (c.A == byte.MaxValue)
				return c;
			var a = c.A / 255f;
			return Color.FromArgb(c.A, (byte)(c.R * a + 0.5f), (byte)(c.G * a + 0.5f), (byte)(c.B * a + 0.5f));
		}

		public static Color PremultipliedColorLerp(float t, Color c1, Color c2)
		{
			// Colors must be lerped in a non-multiplied color space
			var a1 = 255f / c1.A;
			var a2 = 255f / c2.A;
			return PremultiplyAlpha(Color.FromArgb(
				(int)(t * c2.A + (1 - t) * c1.A),
				(int)((byte)(t * a2 * c2.R + 0.5f) + (1 - t) * (byte)(a1 * c1.R + 0.5f)),
				(int)((byte)(t * a2 * c2.G + 0.5f) + (1 - t) * (byte)(a1 * c1.G + 0.5f)),
				(int)((byte)(t * a2 * c2.B + 0.5f) + (1 - t) * (byte)(a1 * c1.B + 0.5f))));
		}

		public static float[] IdentityMatrix()
		{
			return Exts.MakeArray(16, j => (j % 5 == 0) ? 1.0f : 0);
		}

		public static float[] ScaleMatrix(float sx, float sy, float sz)
		{
			var mtx = IdentityMatrix();
			mtx[0] = sx;
			mtx[5] = sy;
			mtx[10] = sz;
			return mtx;
		}

		public static float[] TranslationMatrix(float x, float y, float z)
		{
			var mtx = IdentityMatrix();
			mtx[12] = x;
			mtx[13] = y;
			mtx[14] = z;
			return mtx;
		}

		public static float[] MatrixMultiply(float[] lhs, float[] rhs)
		{
			var mtx = new float[16];
			for (var i = 0; i < 4; i++)
				for (var j = 0; j < 4; j++)
				{
					mtx[4 * i + j] = 0;
					for (var k = 0; k < 4; k++)
						mtx[4 * i + j] += lhs[4 * k + j] * rhs[4 * i + k];
				}

			return mtx;
		}

		public static float[] MatrixVectorMultiply(float[] mtx, float[] vec)
		{
			var ret = new float[4];
			for (var j = 0; j < 4; j++)
			{
				ret[j] = 0;
				for (var k = 0; k < 4; k++)
					ret[j] += mtx[4 * k + j] * vec[k];
			}

			return ret;
		}

		public static float[] MatrixInverse(float[] m)
		{
			var mtx = new float[16];

			mtx[0] = m[5] * m[10] * m[15] -
				m[5] * m[11] * m[14] -
				m[9] * m[6] * m[15] +
				m[9] * m[7] * m[14] +
				m[13] * m[6] * m[11] -
				m[13] * m[7] * m[10];

			mtx[4] = -m[4] * m[10] * m[15] +
				m[4] * m[11] * m[14] +
				m[8] * m[6] * m[15] -
				m[8] * m[7] * m[14] -
				m[12] * m[6] * m[11] +
				m[12] * m[7] * m[10];

			mtx[8] = m[4] * m[9] * m[15] -
				m[4] * m[11] * m[13] -
				m[8] * m[5] * m[15] +
				m[8] * m[7] * m[13] +
				m[12] * m[5] * m[11] -
				m[12] * m[7] * m[9];

			mtx[12] = -m[4] * m[9] * m[14] +
				m[4] * m[10] * m[13] +
				m[8] * m[5] * m[14] -
				m[8] * m[6] * m[13] -
				m[12] * m[5] * m[10] +
				m[12] * m[6] * m[9];

			mtx[1] = -m[1] * m[10] * m[15] +
				m[1] * m[11] * m[14] +
				m[9] * m[2] * m[15] -
				m[9] * m[3] * m[14] -
				m[13] * m[2] * m[11] +
				m[13] * m[3] * m[10];

			mtx[5] = m[0] * m[10] * m[15] -
				m[0] * m[11] * m[14] -
				m[8] * m[2] * m[15] +
				m[8] * m[3] * m[14] +
				m[12] * m[2] * m[11] -
				m[12] * m[3] * m[10];

			mtx[9] = -m[0] * m[9] * m[15] +
				m[0] * m[11] * m[13] +
				m[8] * m[1] * m[15] -
				m[8] * m[3] * m[13] -
				m[12] * m[1] * m[11] +
				m[12] * m[3] * m[9];

			mtx[13] = m[0] * m[9] * m[14] -
				m[0] * m[10] * m[13] -
				m[8] * m[1] * m[14] +
				m[8] * m[2] * m[13] +
				m[12] * m[1] * m[10] -
				m[12] * m[2] * m[9];

			mtx[2] = m[1] * m[6] * m[15] -
				m[1] * m[7] * m[14] -
				m[5] * m[2] * m[15] +
				m[5] * m[3] * m[14] +
				m[13] * m[2] * m[7] -
				m[13] * m[3] * m[6];

			mtx[6] = -m[0] * m[6] * m[15] +
				m[0] * m[7] * m[14] +
				m[4] * m[2] * m[15] -
				m[4] * m[3] * m[14] -
				m[12] * m[2] * m[7] +
				m[12] * m[3] * m[6];

			mtx[10] = m[0] * m[5] * m[15] -
				m[0] * m[7] * m[13] -
				m[4] * m[1] * m[15] +
				m[4] * m[3] * m[13] +
				m[12] * m[1] * m[7] -
				m[12] * m[3] * m[5];

			mtx[14] = -m[0] * m[5] * m[14] +
				m[0] * m[6] * m[13] +
				m[4] * m[1] * m[14] -
				m[4] * m[2] * m[13] -
				m[12] * m[1] * m[6] +
				m[12] * m[2] * m[5];

			mtx[3] = -m[1] * m[6] * m[11] +
				m[1] * m[7] * m[10] +
				m[5] * m[2] * m[11] -
				m[5] * m[3] * m[10] -
				m[9] * m[2] * m[7] +
				m[9] * m[3] * m[6];

			mtx[7] = m[0] * m[6] * m[11] -
				m[0] * m[7] * m[10] -
				m[4] * m[2] * m[11] +
				m[4] * m[3] * m[10] +
				m[8] * m[2] * m[7] -
				m[8] * m[3] * m[6];

			mtx[11] = -m[0] * m[5] * m[11] +
				m[0] * m[7] * m[9] +
				m[4] * m[1] * m[11] -
				m[4] * m[3] * m[9] -
				m[8] * m[1] * m[7] +
				m[8] * m[3] * m[5];

			mtx[15] = m[0] * m[5] * m[10] -
				m[0] * m[6] * m[9] -
				m[4] * m[1] * m[10] +
				m[4] * m[2] * m[9] +
				m[8] * m[1] * m[6] -
				m[8] * m[2] * m[5];

			var det = m[0] * mtx[0] + m[1] * mtx[4] + m[2] * mtx[8] + m[3] * mtx[12];
			if (det == 0)
				return null;

			for (var i = 0; i < 16; i++)
				mtx[i] *= 1 / det;

			return mtx;
		}

		public static float[] MakeFloatMatrix(Int32Matrix4x4 imtx)
		{
			var multipler = 1f / imtx.M44;
			return new float[]
			{
				imtx.M11 * multipler,
				imtx.M12 * multipler,
				imtx.M13 * multipler,
				imtx.M14 * multipler,

				imtx.M21 * multipler,
				imtx.M22 * multipler,
				imtx.M23 * multipler,
				imtx.M24 * multipler,

				imtx.M31 * multipler,
				imtx.M32 * multipler,
				imtx.M33 * multipler,
				imtx.M34 * multipler,

				imtx.M41 * multipler,
				imtx.M42 * multipler,
				imtx.M43 * multipler,
				imtx.M44 * multipler,
			};
		}

		public static float[] MatrixAABBMultiply(float[] mtx, float[] bounds)
		{
			// Corner offsets
			var ix = new uint[] { 0, 0, 0, 0, 3, 3, 3, 3 };
			var iy = new uint[] { 1, 1, 4, 4, 1, 1, 4, 4 };
			var iz = new uint[] { 2, 5, 2, 5, 2, 5, 2, 5 };

			// Vectors to opposing corner
			var ret = new float[] { float.MaxValue, float.MaxValue, float.MaxValue,
				float.MinValue, float.MinValue, float.MinValue };

			// Transform vectors and find new bounding box
			for (var i = 0; i < 8; i++)
			{
				var vec = new float[] { bounds[ix[i]], bounds[iy[i]], bounds[iz[i]], 1 };
				var tvec = MatrixVectorMultiply(mtx, vec);

				ret[0] = Math.Min(ret[0], tvec[0] / tvec[3]);
				ret[1] = Math.Min(ret[1], tvec[1] / tvec[3]);
				ret[2] = Math.Min(ret[2], tvec[2] / tvec[3]);
				ret[3] = Math.Max(ret[3], tvec[0] / tvec[3]);
				ret[4] = Math.Max(ret[4], tvec[1] / tvec[3]);
				ret[5] = Math.Max(ret[5], tvec[2] / tvec[3]);
			}

			return ret;
		}
	}
}
