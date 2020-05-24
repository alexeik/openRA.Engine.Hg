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

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public static class ChromeProvider
	{
		struct Collection
		{
			public string Src;
			public Dictionary<string, MappedImage> Regions;
			internal int OffsetLeft;
			internal int OffsetTop;
		}

		struct SheetCache
		{
			public Sheet2D sheet;
			public int OffsetX;
			public int OffsetY;
		}
		static Dictionary<string, Collection> collections;
		static Dictionary<string, Sheet> cachedSheets;
		static Dictionary<string, SheetCache> cachedSheets2d;
		public static Dictionary<string, Dictionary<string, Sprite>> cachedSprites;
		static IReadOnlyFileSystem fileSystem;
		public static World World;
		static SequenceProvider seqprov;
		static Sprite currentPngSprite;
		static bool Initialized = false;


		public static void Initialize(ModData modData)
		{
			if (Initialized)
			{ return; }

			Deinitialize();

			fileSystem = modData.DefaultFileSystem;
			collections = new Dictionary<string, Collection>();
			cachedSheets = new Dictionary<string, Sheet>();
			
			cachedSheets2d = new Dictionary<string, SheetCache>();
			cachedSprites = new Dictionary<string, Dictionary<string, Sprite>>();

			var chrome = MiniYaml.Merge(modData.Manifest.Chrome
				.Select(s => MiniYaml.FromStream(fileSystem.Open(s), s)));

			foreach (var c in chrome)
				LoadCollection(c.Key, c.Value);
			Initialized = true;
		}

		public static void Deinitialize()
		{
			if (cachedSheets != null)
				foreach (var sheet in cachedSheets.Values)
					sheet.Dispose();
			cachedSheets2d = null;
			collections = null;
			cachedSheets = null;
			cachedSprites = null;
		}

		public static void Save(string file)
		{
			var root = new List<MiniYamlNode>();
			foreach (var kv in collections)
				root.Add(new MiniYamlNode(kv.Key, SaveCollection(kv.Value)));

			root.WriteToFile(file);
		}

		static MiniYaml SaveCollection(Collection collection)
		{
			var root = new List<MiniYamlNode>();
			foreach (var kv in collection.Regions)
				root.Add(new MiniYamlNode(kv.Key, kv.Value.Save(collection.Src)));

			return new MiniYaml(collection.Src, root);
		}

		static void LoadCollection(string name, MiniYaml yaml)
		{
			if (Game.ModData.LoadScreen != null)
				Game.ModData.LoadScreen.Display();
			var collection = new Collection()
			{
				Src = yaml.Value,
				Regions = yaml.Nodes.ToDictionary(n => n.Key, n => new MappedImage(yaml.Value, n.Value))
			};

			// Regions это ссылки на области внутри текстуры png файла. Name это псевдоним имени png файла. Все из chrome.yaml будет тут считано.

			collections.Add(name, collection);
		}

		public static Sprite GetImageOld(string collectionName, string imageName)
		{
			if(imageName== "harktitle")
			{

			}
			if (string.IsNullOrEmpty(collectionName))
				return null;

			// Cached sprite
			Dictionary<string, Sprite> cachedCollection;
			Sprite sprite;
			if (cachedSprites.TryGetValue(collectionName, out cachedCollection) && cachedCollection.TryGetValue(imageName, out sprite))
				return sprite;

			Collection collection;
			if (!collections.TryGetValue(collectionName, out collection))
			{
				Log.Write("debug", "Could not find collection '{0}'", collectionName);
				return null;
			}

			MappedImage mi;
			if (!collection.Regions.TryGetValue(imageName, out mi))
				return null;

			SequenceProvider seqprov;
			seqprov = World.Map.Rules.Sequences;
			// по идее, можно написать в chrome.yaml разные ресурсы игры cps и т.п. , они будут загружаться , только при обращении в этот метод.
			// при обращении за cps , переменная wolrd уже будет заполнена.
			bool switch2Seq = false;

			if (seqprov != null)
			{
				if (seqprov.HasSequence(mi.Src))
				{
					switch2Seq = true;
				}
			}
			Sprite image = null, image2 = null;
			Sheet sheet;

			if (switch2Seq)
			{
				if (cachedSheets.ContainsKey(mi.Src)) //mi.Src это имя png файла.
					sheet = cachedSheets[mi.Src];
				else
				{

					sheet = seqprov.GetSequence(mi.Src, "idle").GetSprite(0).Sheet;
					cachedSheets.Add(mi.Src, sheet);
				}
				image2 = seqprov.GetSequence(mi.Src, "idle").GetSprite(0);
				int2 offset = new int2(image2.Bounds.Location.X, image2.Bounds.Location.Y); //основная часть текстуры
				image = new Sprite(sheet, new Rectangle(mi.rect.X + offset.X, mi.rect.Y + offset.Y, mi.rect.Width, mi.rect.Height), TextureChannel.Red); //смещение в основной части текстуры

				if (mi.Rotate > 0)
				{
					// передаем данные о повороте, если больше 0
					image.Rotate = mi.Rotate;

				}
				if (mi.Stretched)
				{
					image.Stretched = true;
				}
				image.SpriteType = 3; // для Utils.FastCreateQuad для алгоритма Fill rect with
				
				if (cachedCollection == null)
				{
					cachedCollection = new Dictionary<string, Sprite>();
					cachedSprites.Add(collectionName, cachedCollection);
				}
				cachedCollection.Add(imageName, image);
				return image;


			}

			if (!switch2Seq)
			{
				// Cached sheet
				if (cachedSheets.ContainsKey(mi.Src)) //mi.Src это имя png файла. То есть Sheet создается под каждый файл png.
					sheet = cachedSheets[mi.Src];
				else
				{
					using (var stream = fileSystem.Open(mi.Src))
						sheet = new Sheet(SheetType.BGRA, stream);

					cachedSheets.Add(mi.Src, sheet);
				}

				// Cache the sprite
				if (cachedCollection == null)
				{
					cachedCollection = new Dictionary<string, Sprite>();
					cachedSprites.Add(collectionName, cachedCollection);
				}

				image = mi.GetImage(sheet);
				cachedCollection.Add(imageName, image);
			}
			return image;
		}
		public static Sprite GetImage(string collectionName, string imageName)
		{
			if (imageName == "harktitle")
			{

			}
			if (string.IsNullOrEmpty(collectionName))
				return null;

			// Cached sprite
			Dictionary<string, Sprite> cachedCollection;
			Sprite sprite;
			if (collectionName=="dropdown")
			{

			}
			if (cachedSprites.TryGetValue(collectionName, out cachedCollection) && cachedCollection.TryGetValue(imageName, out sprite))
				return sprite; //нашли в кешах, возвращаем Sprite

			Collection collection;
			if (!collections.TryGetValue(collectionName, out collection))
			{
				Log.Write("debug", "Could not find collection '{0}'", collectionName);
				return null;
			}

			MappedImage mi;
			if (!collection.Regions.TryGetValue(imageName, out mi))
				return null;
			if (World == null)
			{

			}
			else
			{
				seqprov = World.Map.Rules.Sequences;
			}
			// по идее, можно написать в chrome.yaml разные ресурсы игры cps и т.п. , они будут загружаться , только при обращении в этот метод.
			// при обращении за cps , переменная wolrd уже будет заполнена.
			bool switch2Seq = false;

			if (seqprov != null)
			{
				if (seqprov.HasSequence(mi.Src))
				{
					switch2Seq = true;
				}
			}
			Sprite image = null, image2 = null;
			SheetCache sheet2d;

			if (switch2Seq)
			{
				if (cachedSheets2d.ContainsKey(mi.Src)) //mi.Src это имя png файла.
					sheet2d = cachedSheets2d[mi.Src];
				else
				{
					sheet2d = new SheetCache();
					sheet2d.sheet = seqprov.GetSequence(mi.Src, "idle").GetSprite(0).Sheet2D;
					cachedSheets2d.Add(mi.Src, sheet2d);
				}
				image2 = seqprov.GetSequence(mi.Src, "idle").GetSprite(0);
				int2 offset = new int2(image2.Bounds.Location.X, image2.Bounds.Location.Y); //основная часть текстуры
				image = new Sprite(sheet2d.sheet, new Rectangle(mi.rect.X + offset.X, mi.rect.Y + offset.Y, mi.rect.Width, mi.rect.Height), TextureChannel.Red); //смещение в основной части текстуры

				if (mi.Rotate > 0)
				{
					// передаем данные о повороте, если больше 0
					image.Rotate = mi.Rotate;

				}
				if (mi.Stretched)
				{
					image.Stretched = true;
				}
				image.SpriteType = 3; // для Utils.FastCreateQuad для алгоритма Fill rect with

				if (cachedCollection == null)
				{
					cachedCollection = new Dictionary<string, Sprite>();
					cachedSprites.Add(collectionName, cachedCollection);
				}
				cachedCollection.Add(imageName, image);
				return image;


			}

			if (!switch2Seq)
			{
				// Cached sheet
				if (cachedCollection == null)
				{
					cachedCollection = new Dictionary<string, Sprite>();
					cachedSprites.Add(collectionName, cachedCollection);
				}

				
				if (cachedSheets2d.ContainsKey(mi.Src)) //mi.Src это имя png файла. Каждый png будет в своем индексе у 2д текстуры
				{
					sheet2d = cachedSheets2d[mi.Src];
			
				}
				else
				{
					using (var stream = fileSystem.Open(mi.Src))
					{

						//currentPngSprite = seqprov.SpriteCache.SheetBuilder2D.Add(new Png(stream));
						currentPngSprite = Game.SheetBuilder2D.Add(new Png(stream));
						//sheet2d = new Sheet2D(SheetType.BGRA, stream);
					}

					sheet2d = new SheetCache();
					sheet2d.sheet = Game.SheetBuilder2D.Current;
					sheet2d.OffsetX = currentPngSprite.Bounds.Location.X; //так как теперь png внутри большой текстуры, то теперь нужно запоминать ее смещение и прибавлять к коориданатам в chrome.yaml
					sheet2d.OffsetY = currentPngSprite.Bounds.Location.Y;
					cachedSheets2d.Add(mi.Src, sheet2d);
				}
				//collection.OffsetLeft = sheet2d.OffsetX;
				//collection.OffsetTop = sheet2d.OffsetY;
				// Cache the sprite

				mi.OffsetTop = sheet2d.OffsetY;
				mi.OffsetLeft = sheet2d.OffsetX;
				image = mi.GetImage(sheet2d.sheet);
				cachedCollection.Add(imageName, image);
			}
			return image;
		}
		public static void AddSprite(string collectionName, string imageName, Sprite sprite)
		{
			if (string.IsNullOrEmpty(collectionName))
				return ;

			// Cached sprite
			Dictionary<string, Sprite> cachedCollection;

			Sprite spriteout;
			if (cachedSprites.TryGetValue(collectionName, out cachedCollection) && cachedCollection.TryGetValue(imageName, out spriteout))
				return ; //нашли в кешах, возвращаем Sprite


			if (cachedCollection == null)
			{
				cachedCollection = new Dictionary<string, Sprite>();
				cachedSprites.Add(collectionName, cachedCollection);
			}
			cachedCollection.Add(imageName, sprite);
		
			//cachedCollection.Add(imageName, sprite);

		}
	}
}


