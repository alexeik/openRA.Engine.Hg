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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	class TheaterTemplate
	{
		public readonly Sprite[] Sprites;
		public readonly int Stride;
		public readonly int Variants;

		public TheaterTemplate(Sprite[] sprites, int stride, int variants)
		{
			Sprites = sprites;
			Stride = stride;
			Variants = variants;
		}
	}

	/// <summary>
	/// Класс по-сути SequenceProvider для спрайтов карты.
	/// </summary>
	public sealed class Theater : IDisposable
	{
		readonly Dictionary<ushort, TheaterTemplate> templates = new Dictionary<ushort, TheaterTemplate>();
		readonly SheetBuilder2D sheetBuilder2d;
		public readonly SheetBuilder2D sbMegaTexture;
		readonly Sprite missingTile;
		Sprite MegaTextureSprite;
		readonly MersenneTwister random;
		public TileSet tileset;

		public Theater(TileSet tileset)
		{
			this.tileset = tileset;
			var allocated = false;

			//Func<Sheet2D> allocate = () =>
			//{
			//	if (allocated)
			//		throw new SheetOverflowException("Terrain sheet overflow. Try increasing the tileset SheetSize parameter.");
			//	allocated = true;
			//	SheetBuilder2D shb= Game.worldRenderer.World.Map.Rules.Sequences.SpriteCache.SheetBuilder2D;
			//	return new Sheet2D(SheetType.Indexed, shb., shb.TextureArrayIndex);
			//};
			
			//SheetBuilder2D shb = Game.OrderManager.World.Map.Rules.Sequences.SpriteCache.SheetBuilder2D;

			//sheetBuilder2d = Game.OrderManager.World.Map.Rules.Sequences.SpriteCache.SheetBuilder2D;
			sheetBuilder2d = Game.SheetBuilder2D;



			if (!string.IsNullOrEmpty(tileset.MegaTexture))
			{
				sbMegaTexture = sheetBuilder2d;
				LoadsbMegaTexture(tileset.MegaTexture);
			}
			random = new MersenneTwister();

			var frameCache = new FrameCache(Game.ModData.DefaultFileSystem, Game.ModData.SpriteLoaders);
			foreach (var t in tileset.Templates)
			{
				var variants = new List<Sprite[]>();

				foreach (var i in t.Value.Images)
				{
					var allFrames = frameCache[i];
					var frameCount = tileset.EnableDepth ? allFrames.Length / 2 : allFrames.Length;
					var indices = t.Value.Frames != null ? t.Value.Frames : Enumerable.Range(0, frameCount);
					variants.Add(indices.Select(j =>
					{
						var f = allFrames[j];
						var tile = t.Value.Contains(j) ? t.Value[j] : null;

						// The internal z axis is inverted from expectation (negative is closer)
						var zOffset = tile != null ? -tile.ZOffset : 0;
						var zRamp = tile != null ? tile.ZRamp : 1f;
						var offset = new float3(f.Offset, zOffset);
						var s = sheetBuilder2d.Allocate(f.Size, zRamp, offset);
						Util.FastCopyIntoChannel(s, f.Data);

						if (tileset.EnableDepth)
						{
							var ss = sheetBuilder2d.Allocate(f.Size, zRamp, offset);
							Util.FastCopyIntoChannel(ss, allFrames[j + frameCount].Data);

							// s and ss are guaranteed to use the same sheet
							// because of the custom terrain sheet allocation
							s = new SpriteWithSecondaryData(s, s.Sheet, ss.Bounds, ss.Channel);
						}

						return s;
					}).ToArray());
				}

				var allSprites = variants.SelectMany(s => s);

				// Ignore the offsets baked into R8 sprites
				if (tileset.IgnoreTileSpriteOffsets)
					allSprites = allSprites.Select(s => new Sprite(s.Sheet2D, s.Bounds, s.ZRamp, new float3(float2.Zero, s.Offset.Z), s.Channel, s.BlendMode));

				if (t.Value.Variants == "Calc")
				{
					templates.Add(t.Value.Id, new TheaterTemplate(allSprites.ToArray(), 1, variants.First().Count()));
				}
				else
				{
					templates.Add(t.Value.Id, new TheaterTemplate(allSprites.ToArray(), variants.First().Count(), t.Value.Images.Length));
				}
			}

			// 1x1px transparent tile
			missingTile = sheetBuilder2d.Add(new byte[1], new Size(1, 1));

			//Sheet2D.ReleaseBuffer();
		}

		public void LoadsbMegaTexture(string filename)
		{
			FileSystem.IReadOnlyPackage pack;
			string temp;

			if (Game.ModData.DefaultFileSystem.TryGetPackageContaining(filename, out pack, out temp))
			{
				using (var stream = Game.ModData.DefaultFileSystem.Open("noise.png"))
				{
					Png pic;
					try
					{
						pic = new Png(stream);
						MegaTextureSprite = sbMegaTexture.Add(pic);
					}
					catch (Exception e)
					{
						//Console.WriteLine("Error loading char: {0} x {1}", f, Convert.ToInt32(f.Split('.')[0]));
					}
				}
			}
		}

		public Sprite TileSprite(TerrainTile r, int? variant = null)
		{
			bool flag1=false;
			if (!string.IsNullOrEmpty(tileset.MegaTexture))
			{
				flag1 = true;
			}

			if (r.Type==0 &&  flag1)
			{
				Sprite sp = new Sprite(sbMegaTexture.Current, new Rectangle(0, 0, 16, 16), TextureChannel.RGBA);
				sp.SpriteType = 4;
				return sp;
			}
			else
			{
				TheaterTemplate template;
				if (!templates.TryGetValue(r.Type, out template))
					return missingTile;

				if (r.Index >= template.Stride)
					return missingTile;
				// if variant == null then random.Next calls
				var start = template.Variants > 1 ? (variant.HasValue ? variant.Value : random.Next(template.Variants)) : 0;
				return template.Sprites[start * template.Stride + r.Index];
			}

		}

		public Rectangle TemplateBounds(TerrainTemplateInfo template, Size tileSize, MapGridType mapGrid)
		{
			Rectangle? templateRect = null;

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)(i++));
					var tileInfo = tileset.GetTileInfo(tile);

					// Empty tile
					if (tileInfo == null)
						continue;

					var sprite = TileSprite(tile);
					var u = mapGrid == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = mapGrid == MapGridType.Rectangular ? y : (x + y) / 2f;

					var tl = new float2(u * tileSize.Width, (v - 0.5f * tileInfo.Height) * tileSize.Height) - 0.5f * sprite.Size;
					var rect = new Rectangle((int)(tl.X + sprite.Offset.X), (int)(tl.Y + sprite.Offset.Y), (int)sprite.Size.X, (int)sprite.Size.Y);
					templateRect = templateRect.HasValue ? Rectangle.Union(templateRect.Value, rect) : rect;
				}
			}

			return templateRect.HasValue ? templateRect.Value : Rectangle.Empty;
		}

		public Sheet2D Sheet { get { return sheetBuilder2d.Current; } }

		public void Dispose()
		{
			sheetBuilder2d.Dispose();
		}
	}
}
