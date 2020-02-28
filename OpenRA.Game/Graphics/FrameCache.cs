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

using OpenRA.FileSystem;
using OpenRA.Primitives;
using System.IO;

namespace OpenRA.Graphics
{
	public class FrameCache
	{
		readonly Cache<string, ISpriteFrame[]> frames;

		public FrameCache(IReadOnlyFileSystem fileSystem, ISpriteLoader[] loaders)
		{
			TypeDictionary metadata;
			frames = new Cache<string, ISpriteFrame[]>(filename => FrameLoader.GetFrames(fileSystem, filename, loaders, out metadata));
		}// ISpriteFrame[] are give a key and value for this Cache

		public ISpriteFrame[] this[string filename] { get { return frames[filename]; } }
	}

	public static class FrameLoader
	{
		public static ISpriteFrame[] GetFrames(IReadOnlyFileSystem fileSystem, string filename, ISpriteLoader[] loaders, out TypeDictionary metadata)
		{
			using (var stream = fileSystem.Open(filename))
			{
				var spriteFrames = GetFrames(stream, loaders, out metadata);
				if (spriteFrames == null)
					throw new InvalidDataException(filename + " is not a valid sprite file!");

				return spriteFrames;
			}
		}

		public static ISpriteFrame[] GetFrames(Stream stream, ISpriteLoader[] loaders, out TypeDictionary metadata)
		{
			ISpriteFrame[] frames;
			metadata = null;

			foreach (var loader in loaders)
				if (loader.TryParseSprite(stream, out frames, out metadata))
					return frames;

			return null;
		}
	}
}
