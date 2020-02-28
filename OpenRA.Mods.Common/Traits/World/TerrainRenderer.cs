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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TerrainRendererInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new TerrainRenderer(init.World); }
	}

	public sealed class TerrainRenderer : IRenderTerrain, IWorldLoaded, INotifyActorDisposing
	{
		readonly Map map;
		readonly Dictionary<string, TerrainSpriteLayer> spriteLayers = new Dictionary<string, TerrainSpriteLayer>();
		public Theater theater;
		bool disposed;

		public TerrainRenderer(World world)
		{
			map = world.Map;
		}
		public Dictionary<string, TerrainSpriteLayer> GetTerrainSpriteLayerRenderer()
		{
			return spriteLayers;
		}
		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			theater = wr.Theater;

			foreach (var template in map.Rules.TileSet.Templates)
			{
				var palette = template.Value.Palette ?? TileSet.TerrainPaletteInternalName;
				spriteLayers.GetOrAdd(palette, pal =>
					new TerrainSpriteLayer(world, wr, theater.Sheet, BlendMode.Alpha, wr.Palette(palette), world.Type != WorldType.Editor));
			}

			foreach (var cell in map.AllCells)
				UpdateCell(cell);

			map.Tiles.CellEntryChanged += UpdateCell;
			map.Height.CellEntryChanged += UpdateCell;
		}

		public void UpdateCell(CPos cell)
		{
			var tile = map.Tiles[cell];
			var palette = TileSet.TerrainPaletteInternalName;
			if (map.Rules.TileSet.Templates.ContainsKey(tile.Type))
				palette = map.Rules.TileSet.Templates[tile.Type].Palette ?? palette;

			var sprite = theater.TileSprite(tile);
			foreach (var kv in spriteLayers)
				kv.Value.Update(cell, palette == kv.Key ? sprite : null);
		}

		int ff = 0;
		int cc = 0;

		void IRenderTerrain.RenderTerrain(WorldRenderer wr, Viewport viewport)
		{
			cc = 0;
			Console.WriteLine("ff" + ff);
			ff++;

			// TODO: по идее, рисуется карта, а потом на ней рисуются разные слои от IRenderOverlay , но вышло не так. Каждый IRenderOverlay
			// рисует карту заново со своими добавками :) IRenderOverlay= D2TerrainLayer,BuildableTerrainLayer,SmudgeLayer,D2ResourceLayer.
			// надо бы, чтобы IRenderOverlay рисовали на TerrainSpriteLayer, которй внутри переменной spriteLayers.
			foreach (var kv in spriteLayers.Values)
				kv.Draw(wr.Viewport);

			foreach (var r in wr.World.WorldActor.TraitsImplementing<IRenderOverlay>())
			{
				Console.WriteLine("cc" + cc + " obj" + r.GetType().Name);

				r.Render(wr);
				cc++;
			}

		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			map.Tiles.CellEntryChanged -= UpdateCell;
			map.Height.CellEntryChanged -= UpdateCell;

			foreach (var kv in spriteLayers.Values)
				kv.Dispose();

			disposed = true;
		}
	}
}
