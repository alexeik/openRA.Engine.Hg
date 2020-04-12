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
using System.IO;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public sealed class TerrainSpriteLayer : IDisposable
	{
		public readonly Sheet[] sheets=new Sheet[7];
		public readonly BlendMode BlendMode;

		readonly Sprite emptySprite;

		readonly VertexBuffer<Vertex> vertexBuffer;
		readonly Vertex[] vertices;
		readonly HashSet<int> dirtyRows = new HashSet<int>();
		readonly int TerrainFullRowLenInVertexRowsNums;
		readonly bool restrictToBounds;

		readonly WorldRenderer worldRenderer;
		readonly Map map;

		readonly PaletteReference palette;
		public string ownername;
		public TerrainSpriteLayer(World world, WorldRenderer wr, Sheet sheet, BlendMode blendMode, PaletteReference palette, bool restrictToBounds, string ownername)
		{
			// Так как все вертексы хранятся плоским списком, то приходится отправлять в рендер всю ширину карты. Так как нельзя вырезать регион из плоского списка по Ширине
			// Поэтому отрезается только по высоте. А ширина учитывается полностью.
			worldRenderer = wr;
			this.restrictToBounds = restrictToBounds;
			
			BlendMode = blendMode;
			this.palette = palette;
			this.ownername = ownername;
			map = world.Map;
			TerrainFullRowLenInVertexRowsNums = 6 * map.MapSize.X;

			vertices = new Vertex[TerrainFullRowLenInVertexRowsNums * map.MapSize.Y];
			vertexBuffer = Game.Renderer.Context.CreateVertexBuffer(vertices.Length, "TerrainSpriteLayer");
			emptySprite = new Sprite(sheet, Rectangle.Empty, TextureChannel.Alpha);
			vertexBuffer.ownername += "->" + ownername;
			wr.PaletteInvalidated += UpdatePaletteIndices;
		}

		public int ns;

		public int2 SetRenderStateForSprite(Sprite s)
		{

			// Check if the sheet (or secondary data sheet) have already been mapped
			var sheet = s.Sheet;
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



			return new int2(sheetIndex, secondarySheetIndex);
		}

		void UpdatePaletteIndices()
		{
			// Everything in the layer uses the same palette,
			// so we can fix the indices in one pass
			for (var i = 0; i < vertices.Length; i++)
			{
				var v = vertices[i];
				vertices[i] = new Vertex(v.X, v.Y, v.Z, v.S, v.T, v.U, v.V, palette.TextureIndex, 0, v.Drawmode, 0, 0, 0, 0, 0);
			}

			for (var row = 0; row < map.MapSize.Y; row++)
				dirtyRows.Add(row);
		}

		public void Update(CPos cell, Sprite sprite)
		{
			var xyz = sprite == null ? float3.Zero :
				worldRenderer.Screen3DPosition(map.CenterOfCell(cell)) + sprite.Offset - 0.5f * sprite.Size;

			Update(cell.ToMPos(map.Grid.Type), sprite, xyz);
		}

		public void Update(MPos uv, Sprite sprite, float3 pos)
		{
			if (sprite != null)
			{
				//if (sprite.Sheet != Sheet)
				//	throw new InvalidDataException("Attempted to add sprite from a different sheet");

				if (sprite.BlendMode != BlendMode)
					throw new InvalidDataException("Attempted to add sprite with a different blend mode");
			}
			else
				sprite = emptySprite;


			// The vertex buffer does not have geometry for cells outside the map
			if (!map.Tiles.Contains(uv))
				return;
			int2 textslot;
			

			var offset = TerrainFullRowLenInVertexRowsNums * uv.V + 6 * uv.U;
			if (sprite.SpriteType == 4)
			{
				Sprite tmpsp = new Sprite(sprite.Sheet, new Rectangle((int)pos.X, (int)pos.Y, 16, 16), TextureChannel.RGBA);
				tmpsp.SpriteType = 4;
				textslot = SetRenderStateForSprite(tmpsp);
				Util.FastCreateQuad(vertices, pos, tmpsp, textslot, palette.TextureIndex, offset, tmpsp.Size);
			}
			else
			{
				textslot = SetRenderStateForSprite(sprite);
				Util.FastCreateQuad(vertices, pos, sprite, textslot, palette.TextureIndex, offset, sprite.Size);
			}
			dirtyRows.Add(uv.V);
		}

		public void Draw(Viewport viewport)
		{
			var cells = restrictToBounds ? viewport.VisibleCellsInsideBounds : viewport.AllVisibleCells;

			// Only draw the rows that are visible.
			var firstRow = cells.CandidateMapCoords.TopLeft.V.Clamp(0, map.MapSize.Y);
			var lastRow = (cells.CandidateMapCoords.BottomRight.V + 1).Clamp(firstRow, map.MapSize.Y);

			Game.Renderer.Flush();

			vertexBuffer.ActivateVertextBuffer();
			// Flush any visible changes to the GPU
			for (var row = firstRow; row <= lastRow; row++) //update changed quads if any
			{
				if (!dirtyRows.Remove(row))
					continue;

				var rowOffset = TerrainFullRowLenInVertexRowsNums * row;

				unsafe
				{
					// The compiler / language spec won't let us calculate a pointer to
					// an offset inside a generic array T[], and so we are forced to
					// calculate the start-of-row pointer here to pass in to SetData.

					fixed (Vertex* vPtr = &vertices[0])
					{
						vertexBuffer.SetData((IntPtr)(vPtr + rowOffset), rowOffset, TerrainFullRowLenInVertexRowsNums);
					}
				}
			}
	
			// Так как все вертексы хранятся плоским списком, то приходится отправлять в рендер всю ширину карты. Так как нельзя вырезать регион из плоского списка по Ширине
			// Поэтому отрезается только по высоте. А ширина учитывается полностью.
			Game.Renderer.WorldSpriteRenderer.DrawVertexBuffer(
				vertexBuffer, TerrainFullRowLenInVertexRowsNums * firstRow, TerrainFullRowLenInVertexRowsNums * (lastRow - firstRow),
				PrimitiveType.TriangleList, sheets, BlendMode);
			
			Game.Renderer.Flush();
		}

		public void Dispose()
		{
			worldRenderer.PaletteInvalidated -= UpdatePaletteIndices;
			vertexBuffer.Dispose();
		}
	}
}
