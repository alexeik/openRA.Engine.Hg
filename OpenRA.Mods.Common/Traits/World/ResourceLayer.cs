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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the world actor.", "Order of the layers defines the Z sorting.")]
	public class ResourceLayerInfo : ITraitInfo, Requires<ResourceTypeInfo>, Requires<BuildingInfluenceInfo>
	{
		public virtual object Create(ActorInitializer init) { return new ResourceLayer(init.Self); }
	}

	public class ResourceLayer : IRenderOverlay, IWorldLoaded, ITickRender, INotifyActorDisposing
	{
		static readonly CellContents EmptyCell = new CellContents();

		readonly World world;
		readonly BuildingInfluence buildingInfluence;
		readonly HashSet<CPos> ChangedResourceCellList = new HashSet<CPos>();
		readonly Dictionary<PaletteReference, TerrainSpriteLayer> spriteLayers = new Dictionary<PaletteReference, TerrainSpriteLayer>();

		protected readonly CellLayer<CellContents> Content;
		protected readonly CellLayer<CellContents> RenderContent;

		public bool IsResourceLayerEmpty { get { return resCells < 1; } }

		bool disposed;
		int resCells;

		// TerrainSpriteLayer render;
		Dictionary<string, TerrainSpriteLayer> terrainRenderer;
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		public ResourceLayer(Actor self)
		{
			world = self.World;
			buildingInfluence = self.Trait<BuildingInfluence>();

			Content = new CellLayer<CellContents>(world.Map);
			RenderContent = new CellLayer<CellContents>(world.Map);

			// этот метод обновляет TerrainSpriteLayer, через данное событие.
			//RenderContent.CellEntryChanged += SubmitCellToVertexBuffer; 
		}

		void SubmitCellToVertexBuffer(CPos cell2) // вызывается на каждое RenderContent[cell]=...
		{
			TerrainSpriteLayer tsl;
			foreach (var kv in spriteLayers)
			{
				// resource.Type is meaningless (and may be null) if resource.Sprite is null
				tsl = kv.Value;

				if (ChangedResourceCellList.Count > 0)
				{
					foreach (var c in ChangedResourceCellList)
					{
						if (RenderContent[c].Sprite != null && RenderContent[c].Type.Palette == kv.Key)
							tsl.Update(c, RenderContent[c].Sprite);// Update are submit new Quad to vertex buffer of kv(TerrainSpriteLayer)
						else
							tsl.Update(c, null);
					}
					foreach (var r in remove)
						ChangedResourceCellList.Remove(r);
					remove.Clear();
				}
				else
				{
					foreach (var cell in world.Map.AllCells)
					{
						var type = Content[cell].Type;
						if (type != null)
						{

							if (RenderContent[cell].Sprite != null && RenderContent[cell].Type.Palette == kv.Key)
								tsl.Update(cell, RenderContent[cell].Sprite);// Update are submit new Quad to vertex buffer of kv(TerrainSpriteLayer)
							else
								tsl.Update(cell, null);
						}
					}
				}
			}
		}

		int i2 = 0;
		List<CPos> remove = new List<CPos>();

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			

			// ChangedResourceCellList коллекция изменяется методами Harvest & Destroy & addresource, ниже идет анализ, убрать ее с карты рендера и логической
			// или изменить спрайт в зависимости от запаса ресурса на карте.
			// теперь обновим спрайты в RenderCount согласно их координатам в ChangedResourceCellList, через метод UpdateRenderedSprite в наследуемом классе
			foreach (var c in ChangedResourceCellList)
			{
				if (!self.World.FogObscures(c))
				{
					// происходит перекладывание из Content в RenderContent коллекцию. 
					// Все влияющие на ресурых объекты, изменяют Content например метод Harvest или Destroy
					 RenderContent[c] = Content[c]; // это штука, вызовет в коллекции RenderContent типа CellLayer событие изменение ячейки
					// TODO: в методе ниже идет анализ RenderContent поэтому, нужно сохранить присвоение из логики(COntent) в рендер(RenderContent)
					UpdateRenderedSprite(c); // forward call to virtual method that descendant has.
					// тут, UpdateRenderedSprite меняет иконку ресурса в зависимости от его логического запаса(харвест собирает или убивается оружием)
					remove.Add(c);
				}
			}

			
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			SubmitCellToVertexBuffer(new CPos());
			foreach (var kv in spriteLayers.Values)
			{
				kv.Draw(wr.Viewport);
			}
			//render.Draw(wr.Viewport);
			i2 = 0;
		}

		protected virtual void UpdateRenderedSprite(CPos cell)
		{
			var t = RenderContent[cell];
			if (t.Density > 0)
			{
				var sprites = t.Type.Variants[t.Variant];
				var frame = int2.Lerp(0, sprites.Length - 1, t.Density, t.Type.Info.MaxDensity);
				t.Sprite = sprites[frame];
			}
			else
				t.Sprite = null;

			RenderContent[cell] = t; // тут еще раз вызов идет в TerraiSpriteLayer 
									 // RenderContent.CellEntryChanged += SubmitCellToVertexBuffer;
		}

		int GetAdjacentCellsWith(ResourceType t, CPos cell)
		{
			var sum = 0;
			for (var u = -1; u < 2; u++)
			{
				for (var v = -1; v < 2; v++)
				{
					var c = cell + new CVec(u, v);
					if (Content.Contains(c) && Content[c].Type == t)
						++sum;
				}
			}

			return sum;
		}

		
		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var resources = w.WorldActor.TraitsImplementing<ResourceType>() //обращение к ResourceTYpe, который содержит в себе world.yaml.ResourceType таблицу
				.ToDictionary(r => r.Info.ResourceType, r => r);
			

			// Build the sprite layer dictionary for rendering resources
			// All resources that have the same palette must also share a sheet and blend mode
			foreach (var r in resources)
			{
				var layer = spriteLayers.GetOrAdd(r.Value.Palette, pal =>
				{
					var first = r.Value.Variants.First().Value.First();
					return new TerrainSpriteLayer(w, wr, first.Sheet2D, first.BlendMode, pal, wr.World.Type != WorldType.Editor, "ResourceLayer");
				});

				// Validate that sprites are compatible with this layer
				//var sheet = layer.Sheet;
				//if (r.Value.Variants.Any(kv => kv.Value.Any(s => s.Sheet != sheet)))
				//	throw new InvalidDataException("Resource sprites span multiple sheets. Try loading their sequences earlier.");

				var blendMode = layer.BlendMode;
				if (r.Value.Variants.Any(kv => kv.Value.Any(s => s.BlendMode != blendMode)))
					throw new InvalidDataException("Resource sprites specify different blend modes. "
						+ "Try using different palettes for resource types that use different blend modes.");
			}

			//var terrainRenderer = w.WorldActor.TraitOrDefault<IRenderTerrain>(); //to get TerrainRenderer.cs class 
			//this.terrainRenderer = terrainRenderer.GetTerrainSpriteLayerRenderer(); //get all Sprites that it has from tileset\*.yaml file
			//render = this.terrainRenderer[Palette];


			foreach (var cell in w.Map.AllCells)
			{
				ResourceType t;

			// w.Map.Resources тут содержаться таблица ResourceType . При импорте карт д2, туда были записаны 1 и 2
			// поэтому в resources к ключу =1 привязана таблица ResourceType. По сути проверка ниже с TryGetValue ни о чем. Так как там, всегда будет 1 благодаря
			// импорту.
				if (!resources.TryGetValue(w.Map.Resources[cell].Type, out t))
					continue;

				if (!AllowResourceAt(t, cell))
					continue;
				// тут составляется внутрення коллекция ресурсных тайлов Content из всех тайлов Map.AllCells
				// вся таблица ResourceType уходит в t параметр и хранится внутри каждой ячейки ресурса Content[cell].
				Content[cell] = CreateResourceCell(t, cell);
			}


			foreach (var cell in w.Map.AllCells) // это заставляет вызываться TerrainSpriteLayer столько раз, сколько ячеек с типом не null, для дюны выходит 3500 раз:) 
			{
				var type = Content[cell].Type;
				if (type != null)
				{
					// Set initial density based on the number of neighboring resources
					// Adjacent includes the current cell, so is always >= 1
					var adjacent = GetAdjacentCellsWith(type, cell);
					var density = int2.Lerp(0, type.Info.MaxDensity, adjacent, 9);
					var temp = Content[cell];
					temp.Density = Math.Max(density, 1);

					//temp.Sprite = GetResourceSprite(0);

					// Initialize the RenderContent with the initial map state
					// because the shroud may not be enabled.
					Content[cell] = temp;
					RenderContent[cell] = Content[cell];
					// тут вместо того, чтобы присвоить в RenderContent[cell] свойство SPrite, это уводится в 
					// вирт. метод UpdateRenderedSprite(), а уж вирт.метод на основе ResourceType решает, какой спрайт положить в RenderContent[cell]
					// ++ также вызовется это - RenderContent.CellEntryChanged += SubmitCellToVertexBuffer();

					UpdateRenderedSprite(cell);

					//render.Update(cell, RenderContent[cell].Sprite); //запускаем submit тут, так как после UpdateRenderedSprite(cell); спрайт будет обновлен по
																	 // алгоритму из D2ResourceLayer
				}
			}
		}

		//public Sprite GetResourceSprite(int templateid, int? offset, int variantrandom)
		//{
		//	ushort sdf;
		//	//int index = Game.CosmeticRandom.Next(63);
		//	//int ffd = templateid + Convert.ToInt32(offset);
		//	sdf = Convert.ToUInt16(templateid); //тут всегда одна цифра. играемся через variantrandom
		//	var t = new TerrainTile(sdf, 0);
		//	Sprite sprite = _wr.Theater.TileSprite(t, offset);
		//	return sprite;
		//}

		public bool AllowResourceAt(ResourceType rt, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (!rt.Info.AllowedTerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type))
				return false;

			if (!rt.Info.AllowUnderActors && world.ActorMap.AnyActorsAt(cell))
				return false;

			if (!rt.Info.AllowUnderBuildings && buildingInfluence.GetBuildingAt(cell) != null)
				return false;

			if (!rt.Info.AllowOnRamps)
			{
				var tile = world.Map.Tiles[cell];
				var tileInfo = world.Map.Rules.TileSet.GetTileInfo(tile);
				if (tileInfo != null && tileInfo.RampType > 0)
					return false;
			}

			return true;
		}

		public bool CanSpawnResourceAt(ResourceType newResourceType, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			var currentResourceType = GetResource(cell);
			return (currentResourceType == newResourceType && !IsFull(cell))
				|| (currentResourceType == null && AllowResourceAt(newResourceType, cell));
		}

		CellContents CreateResourceCell(ResourceType t, CPos cell)
		{
			world.Map.CustomTerrain[cell] = world.Map.Rules.TileSet.GetTerrainIndex(t.Info.TerrainType);
			++resCells;

			return new CellContents
			{
				Type = t,
				Variant = ChooseRandomVariant(t),
			};
		}
		protected virtual string ChooseRandomVariant(ResourceType t)
		{
			return t.Variants.Keys.Random(Game.CosmeticRandom);
		}

		public void AddResource(ResourceType t, CPos p, int n)
		{
			var cell = Content[p];
			if (cell.Type == null)
				cell = CreateResourceCell(t, p);

			if (cell.Type != t)
				return;

			cell.Density = Math.Min(cell.Type.Info.MaxDensity, cell.Density + n);
			Content[p] = cell;

			ChangedResourceCellList.Add(p);
		}

		public bool IsFull(CPos cell)
		{
			return Content[cell].Density == Content[cell].Type.Info.MaxDensity;
		}

		public ResourceType Harvest(CPos cell)
		{
			var c = Content[cell];
			if (c.Type == null)
				return null;

			--c.Density; // here --c.Denstiry decreases for 1 step of density.

			if (c.Density < 0)
			{
				Content[cell] = EmptyCell;
				world.Map.CustomTerrain[cell] =  byte.MaxValue;
				--resCells;
			}
			else
				Content[cell] = c;

			ChangedResourceCellList.Add(cell);

			return c.Type;
		}

		public void Destroy(CPos cell)
		{
			// Don't break other users of CustomTerrain if there are no resources
			if (Content[cell].Type == null)
				return;

			--resCells;

			// Clear cell
			Content[cell] = EmptyCell;
			world.Map.CustomTerrain[cell] = byte.MaxValue;

			ChangedResourceCellList.Add(cell);
		}

		public ResourceType GetResource(CPos cell) { return Content[cell].Type; }
		public ResourceType GetRenderedResource(CPos cell) { return RenderContent[cell].Type; }
		public int GetResourceDensity(CPos cell) { return Content[cell].Density; }
		public int GetMaxResourceDensity(CPos cell)
		{
			if (Content[cell].Type == null)
				return 0;

			return Content[cell].Type.Info.MaxDensity;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			foreach (var kv in spriteLayers.Values)
				kv.Dispose();

			RenderContent.CellEntryChanged -= SubmitCellToVertexBuffer;

			disposed = true;
		}

		public struct CellContents
		{
			public static readonly CellContents Empty = new CellContents();
			public ResourceType Type;
			public int Density;
			public string Variant;
			public Sprite Sprite { get; set; }
		}
	}
}
