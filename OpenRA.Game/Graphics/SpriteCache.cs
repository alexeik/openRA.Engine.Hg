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
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class SpriteCache
	{
		public readonly SheetBuilder SheetBuilder;
		readonly SpriteLoaderBase[] loaders;
		readonly IReadOnlyFileSystem fileSystem;

		readonly Dictionary<string, List<Sprite[]>> sprites = new Dictionary<string, List<Sprite[]>>();
		readonly Dictionary<string, ISpriteFrame[]> framesCandidatesStorage = new Dictionary<string, ISpriteFrame[]>();
		readonly Dictionary<string, TypeDictionary> metadata = new Dictionary<string, TypeDictionary>();

		public SpriteCache(IReadOnlyFileSystem fileSystem, SpriteLoaderBase[] loaders, SheetBuilder sheetBuilder)
		{
			SheetBuilder = sheetBuilder;
			this.fileSystem = fileSystem;
			this.loaders = loaders;
		}

		/// <summary>
		/// Returns the first set of sprites with the given filename.
		/// If getUsedFrames is defined then the indices returned by the function call
		/// are guaranteed to be loaded.  The value of other indices in the returned
		/// array are undefined and should never be accessed.
		/// </summary>
		public Sprite[] this[string filename, Func<int, IEnumerable<int>> getUsedFramesDelegate = null]
		{
			get
			{
				var allSprites = sprites.GetOrAdd(filename);
				var sprite = allSprites.FirstOrDefault();

				ISpriteFrame[] framesCandidates;
				if (!framesCandidatesStorage.TryGetValue(filename, out framesCandidates))
					framesCandidates = null;

				// This is the first time that the file has been requested
				// Load all of the frames into the unused buffer and initialize
				// the loaded cache (initially empty)
				if (sprite == null)
				{
					TypeDictionary fileMetadata = null;
					framesCandidates = FrameLoader.GetFrames(fileSystem, filename, loaders, out fileMetadata);
					framesCandidatesStorage[filename] = framesCandidates;
					metadata[filename] = fileMetadata;

					sprite = new Sprite[framesCandidates.Length];
					allSprites.Add(sprite);
				}

				// HACK: The sequency code relies on side-effects from getUsedFrames
				var indices = getUsedFramesDelegate != null ? getUsedFramesDelegate(sprite.Length) :
					Enumerable.Range(0, sprite.Length);

				// Load any unused frames into the SheetBuilder
				if (framesCandidates != null)
				{
					foreach (var i in indices)
					{
						if (framesCandidates[i] != null)
						{
							sprite[i] = SheetBuilder.Add(framesCandidates[i]);
							framesCandidates[i] = null;
						}
					}

					// All frames have been loaded
					if (framesCandidates.All(f => f == null))
						framesCandidatesStorage.Remove(filename);
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
