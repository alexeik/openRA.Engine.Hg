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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class SpriteCache
	{
		public readonly SheetBuilder SheetBuilder;
		public readonly SheetBuilder2D SheetBuilder2D;
		readonly SpriteLoaderBase[] loaders;
		readonly IReadOnlyFileSystem fileSystem;

		readonly Dictionary<string, List<Sprite[]>> sprites = new Dictionary<string, List<Sprite[]>>();
		readonly Dictionary<string, ISpriteFrame[]> ParsedFramesStorage = new Dictionary<string, ISpriteFrame[]>();
		readonly Dictionary<string, TypeDictionary> metadata = new Dictionary<string, TypeDictionary>();

		public SpriteCache(IReadOnlyFileSystem fileSystem, SpriteLoaderBase[] loaders, SheetBuilder sheetBuilder)
		{
			SheetBuilder = sheetBuilder;
			this.fileSystem = fileSystem;
			this.loaders = loaders;
		}
		public SpriteCache(IReadOnlyFileSystem fileSystem, SpriteLoaderBase[] loaders, SheetBuilder2D sheetBuilder)
		{
			SheetBuilder2D = sheetBuilder;
			this.fileSystem = fileSystem;
			this.loaders = loaders;
		}

		/// <summary>
		/// Returns the first set of sprites with the given filename.
		/// If getUsedFrames is defined then the indices returned by the function call
		/// are guaranteed to be loaded.  The value of other indices in the returned
		/// array are undefined and should never be accessed.
		/// </summary>
		public Sprite[] this[string filename, Func<int, IEnumerable<int>> getThisIndexesSpritesFromFile = null]
		{
			get
			{
				var allSprites = sprites.GetOrAdd(filename);
				var sprite = allSprites.FirstOrDefault();

				ISpriteFrame[] newFramesFromFile;
				if (!ParsedFramesStorage.TryGetValue(filename, out newFramesFromFile))
					newFramesFromFile = null;

				// This is the first time that the file has been requested
				// Load all of the frames into the unused buffer and initialize
				// the loaded cache (initially empty)
				if (sprite == null)
				{
					TypeDictionary fileMetadata = null;
					using (var stream = fileSystem.Open(filename))
					{
						newFramesFromFile = FrameLoader.GetFrames(stream, filename, loaders, out fileMetadata); //загрузит все спрайты из shp,wsa,... файлов в память
					}
					ParsedFramesStorage[filename] = newFramesFromFile;
					metadata[filename] = fileMetadata;

					sprite = new Sprite[newFramesFromFile.Length];
					allSprites.Add(sprite);
				}

				// HACK: The sequency code relies on side-effects from getUsedFrames
				var indices = getThisIndexesSpritesFromFile != null ? getThisIndexesSpritesFromFile(sprite.Length) :
					Enumerable.Range(0, sprite.Length);

				// Load any unused frames into the SheetBuilder
				if (newFramesFromFile != null)
				{
					foreach (var i in indices)
					{
						if (newFramesFromFile[i] != null)
						{
							//sprite[i] = SheetBuilder.Add(framesCandidates[i]);
							
							if (filename.Contains("png")) //for Loaders with 4bytes per pixel
							{
								using (var stream = fileSystem.Open(filename))
								{
									sprite[i] = SheetBuilder2D.Add(new Png(stream));
								}
							}
							else
							{
								sprite[i] = SheetBuilder2D.Add(newFramesFromFile[i]);
							}
							newFramesFromFile[i] = null;
						}
					}

					// All frames have been loaded
					if (newFramesFromFile.All(f => f == null))
						ParsedFramesStorage.Remove(filename);
				}

				return sprite;
			}
		}

		/// <summary>
		/// Returns a TypeDictionary containing any metadata defined by the frame
		/// or null if the frame does not define metadata.
		/// </summary>
		public TypeDictionary FrameMetadata(string filename)
		{
			TypeDictionary fileMetadata;
			if (!metadata.TryGetValue(filename, out fileMetadata))
			{
				FrameLoader.GetFrames(fileSystem, filename, loaders, out fileMetadata);
				metadata[filename] = fileMetadata;
			}

			return fileMetadata;
		}
	}
}
