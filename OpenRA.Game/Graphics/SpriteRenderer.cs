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
using OpenRA.Platforms.Default;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class SpriteRenderer : Renderer.IBatchRenderer
	{
		readonly Renderer renderer;
		public readonly Shader shader;

		readonly Vertex[] vertices;
		public readonly Sheet[] sheets = new Sheet[7];
		public readonly Sheet2D[] sheets2d = new Sheet2D[7];

		BlendMode currentBlend = BlendMode.None;
		int nv = 0;
		int ns = 0;

		int ns2d = 0;

		readonly string rendererID;
		public SpriteRenderer(string rendereID, Renderer renderer, Shader shader)
		{
			this.rendererID = rendereID;
			this.renderer = renderer;
			this.shader = shader;
			vertices = new Vertex[renderer.TempBufferSize];
		}
		public void IncrementNumSheets()
		{
			ns += 1;
		}

		public void ClearTexturesShader()
		{
			shader.ClearTextures();
			//foreach ( Sheet s in sheets)
			//{
			//	if (s != null)
			//	{
			//		s.Dispose();
			//	}
			//}
			//foreach (Sheet2D s in sheets2d)
			//{
			//	if (s != null)
			//	{
			//		s.Dispose();
			//	}
			//}
		}

		public void Flush()
		{
			if (rendererID=="WorldSpriteRenedrer")
			{

			}
			if (nv > 0)
			{
				for (var i = 0; i < ns; i++)
				{
					shader.SetTexture("Texture{0}".F(i), sheets[i].AssignOrGetOrSetDataGLTexture());
					sheets[i] = null;
				}
				for (var i = 0; i < ns2d; i++)
				{
					shader.SetTexture("Texture2D{0}".F(i), sheets2d[i].AssignOrGetOrSetDataGLTexture());
					sheets2d[i] = null;
				}
				renderer.Context.SetBlendMode(currentBlend);
				shader.PrepareRender();
				renderer.DrawBatchForVertexesSpriteRendererClasses(vertices, nv, PrimitiveType.TriangleList);
				renderer.Context.SetBlendMode(BlendMode.None);
				nv = 0;
				ns = 0;
				ns2d = 0;
			}

		}

		/// <summary>
		/// ���� �����, �������� ��������� opengl DrawBatchWithBind ���� ������� Renderer(��������� Renderer.IBatchRenderer) ���������� �� ��������.
		/// </summary>
		/// <param name="s">������, ������� ������������ � VBO.</param>
		/// <returns>���������� ������ Sheet ���� ����� ������  � ����� ������ Sheet,��� ��� 1 �������(������ R �����) ����� ���� � R,G,B,A ������ ��������.</returns>
		public SamplerPointer SetRenderStateForSprite(Sprite s)
		{
			if (rendererID == "WordlSprireRenderer")
			{

			}
			renderer.CurrentBatchRenderer = this;

			if (s.BlendMode != currentBlend || nv + 6 > renderer.TempBufferSize)
				Flush();

			currentBlend = s.BlendMode;

			// Check if the sheet (or secondary data sheet) have already been mapped
			Sheet sheet;
			Sheet2D sheet2d;

			if (s.Sheet == null)
			{
				sheet2d = s.Sheet2D;
				var sheetIndex = 0;
				for (; sheetIndex < ns2d; sheetIndex++)
					if (sheets2d[sheetIndex] == sheet2d)
						break;

				var secondarySheetIndex = 0;
				var ss = s as SpriteWithSecondaryData;
				if (ss != null)
				{
					var secondarySheet2D = ss.SecondarySheet2D;
					for (; secondarySheetIndex < ns2d; secondarySheetIndex++)
						if (sheets2d[secondarySheetIndex] == secondarySheet2D)
							break;
				}

				// Make sure that we have enough free samplers to map both if needed, otherwise flush
				var needSamplers = (sheetIndex == ns2d ? 1 : 0) + (secondarySheetIndex == ns2d ? 1 : 0);
				if (ns2d + needSamplers >= sheets2d.Length)
				{
					Flush();
					sheetIndex = 0;
					if (ss != null)
						secondarySheetIndex = 1;
				}

				if (sheetIndex >= ns2d)
				{
					sheets2d[sheetIndex] = sheet2d;
					ns2d += 1;
				}

				if (secondarySheetIndex >= ns2d && ss != null)
				{
					sheets[secondarySheetIndex] = ss.SecondarySheet;
					ns2d += 1;
				}



				return new SamplerPointer() { Stype1 = SamplerType.Sampler2d, num1 = sheetIndex, StypeSec = SamplerType.Sampler2d, numSec = secondarySheetIndex};
				//new int2(sheetIndex, secondarySheetIndex);
			}
			else
			{
				sheet = s.Sheet;
				var sheetIndex = 0;
				for (; sheetIndex < ns; sheetIndex++)
					if (sheets[sheetIndex] == sheet)
						break;

				var secondarySheetIndex = 0;
				var ss = s as SpriteWithSecondaryData;
				if (ss != null)
				{
					var secondarySheet = ss.SecondarySheet;
					for (; secondarySheetIndex < ns; secondarySheetIndex++)
						if (sheets[secondarySheetIndex] == secondarySheet)
							break;
				}

				// Make sure that we have enough free samplers to map both if needed, otherwise flush
				var needSamplers = (sheetIndex == ns ? 1 : 0) + (secondarySheetIndex == ns ? 1 : 0);
				if (ns + needSamplers >= sheets.Length)
				{
					Flush();
					sheetIndex = 0;
					if (ss != null)
						secondarySheetIndex = 1;
				}

				if (sheetIndex >= ns)
				{
					sheets[sheetIndex] = sheet;
					ns += 1;
				}

				if (secondarySheetIndex >= ns && ss != null)
				{
					sheets[secondarySheetIndex] = ss.SecondarySheet;
					ns += 1;
				}



				return new SamplerPointer() { Stype1 = SamplerType.Sampler, num1 = sheetIndex, StypeSec = SamplerType.Sampler, numSec = secondarySheetIndex };
			}
			
		}


		public void DrawSprite(Sprite s, float3 location, float paletteTextureIndex, float3 size)
		{
			var samplers = SetRenderStateForSprite(s); // ������ ����� �������� �� ������� ���� ������ � ���������� samplers, ����� ����� �������� ��� � VBO
			Util.FastCreateQuad(vertices, location + s.FractionalOffset * size, s, samplers, paletteTextureIndex, nv, size);
			nv += 6;
		}
		public void DrawTextSprite(Sprite s, float3 location, float paletteTextureIndex, float3 size)
		{
			var samplers = SetRenderStateForSprite(s); // ������ ����� �������� �� ������� ���� ������ � ���������� samplers, ����� ����� �������� ��� � VBO
			Util.FastCreateQuad(vertices, location , s, samplers, paletteTextureIndex, nv, size);
			nv += 6;
		}
		public void DrawSprite(Sprite s, float3 location, PaletteReference pal)
		{
			DrawSprite(s, location, pal.TextureIndex, s.Size);
		}

		public void DrawSprite(Sprite s, float3 location, PaletteReference pal, float3 size)
		{
			DrawSprite(s, location, pal.TextureIndex, size);
		}

		public void DrawSprite(Sprite s, float3 a, float3 b, float3 c, float3 d)
		{
			var samplers = SetRenderStateForSprite(s);
			Util.FastCreateQuad(vertices, a, b, c, d, s, samplers, 0, nv);
			nv += 6;
		}

		/// <summary>
		/// ������������ ��� ��������� ����������� ������ �������� ������, �������� TerrainSpriteLayer
		/// </summary>
		/// <param name="buffer">.</param>
		/// <param name="start">.</param>
		/// <param name="length">.</param>
		/// <param name="type">.</param>
		/// <param name="sheet">.</param>
		/// <param name="blendMode">.</param>
		public void DrawVertexBuffer(VertexBuffer<Vertex> buffer, int start, int length, PrimitiveType type, Sheet sheet, BlendMode blendMode)
		{
			shader.SetTexture("Texture0", sheet.AssignOrGetOrSetDataGLTexture());
			renderer.Context.SetBlendMode(blendMode);
			shader.PrepareRender();
			buffer.ActivateVAO();
			renderer.DrawBatcForOpenGLVertexBuffer(buffer, start, length, type);
			buffer.CloseVAO();
			renderer.Context.SetBlendMode(BlendMode.None);
		}
		public void DrawVertexBuffer(VertexBuffer<Vertex> buffer, int start, int length, PrimitiveType type, Sheet2D[] sheets, BlendMode blendMode)
		{
			if (rendererID == "WorldSpriteRenderer")
			{

			}
			for (var i = 0; i < sheets.Length; i++)
			{
				if (sheets[i] == null)
				{
					continue;
				}
				shader.SetTexture("Texture2D{0}".F(i), sheets[i].AssignOrGetOrSetDataGLTexture());
			}

			renderer.Context.SetBlendMode(blendMode);
			shader.PrepareRender();
			buffer.ActivateVAO();
			renderer.DrawBatcForOpenGLVertexBuffer(buffer, start, length, type);
			buffer.CloseVAO();
			renderer.Context.SetBlendMode(BlendMode.None);
		}

		// For RGBAColorRenderer
		public void DrawRGBAVertices(Vertex[] v)
		{
			if (Game.Renderer.PauseRender)
			{
				return;
			}
			renderer.CurrentBatchRenderer = this;

			if (currentBlend != BlendMode.Alpha || nv + v.Length > renderer.TempBufferSize)
				Flush();

			currentBlend = BlendMode.Alpha;
			Array.Copy(v, 0, vertices, nv, v.Length);
			nv += v.Length;
		}

		public void SetPalette(ITexture palette)
		{
			shader.SetTexture("Palette", palette);
		}
		public void SetFontMSDF(ITexture palette)
		{
			shader.SetTexture("TextureFontMSDF", palette);
		}
		public void SetViewportParams(Size screen, float depthScale, float depthOffset, float zoom, int2 scroll)
		{
			shader.SetVec("Scroll", scroll.X, scroll.Y, scroll.Y);
			shader.SetVec("r1",
				zoom * 2f / screen.Width,
				-zoom * 2f / screen.Height,
				-depthScale * zoom / screen.Height);
			shader.SetVec("r2", -1, 1, 1 - depthOffset);

			// Texture index is sampled as a float, so convert to pixels then scale
			shader.SetVec("DepthTextureScale", 128 * depthScale * zoom / screen.Height);
		}
		public void SetTextColor(Color c)
		{
			shader.SetVec("TextColor", c.R/255f, c.G/255f, c.B/255f);
		}
		public void SetDepthPreviewEnabled(bool enabled)
		{
			shader.SetBool("EnableDepthPreview", enabled);
		}
		public void SetMouseLocation(float2 mloc)
		{
			shader.SetVec("MouseLocation", mloc.X, mloc.Y);
		}

		public void SetAlphaConstantRegion(float r, float g, float b, float a)
		{
			shader.SetVec("AlphaConstantRegion", r/255, g/255, b/255);
		}
		public void SetAlphaInit(float r, float g, float b, float a)
		{
			shader.SetVec("AlphaInit", r/255, g/255, b/255);
		}
		public void SetAlphaFlag(bool c)
		{
			shader.SetBool("AlphaFlag", c);
		}
		public void SetFrameBufferMaskMode(bool c)
		{
			shader.SetBool("FrameBufferMaskMode", c);
		}
		public void SetLayer1KeyColor(float r, float g, float b, float a)
		{
			shader.SetVec("Layer1KeyColor", r / 255, g / 255, b / 255);
		}

	}
}
