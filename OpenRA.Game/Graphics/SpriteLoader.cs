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

using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public interface ISpriteLoader
	{
		bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata);
	}

	public interface ISpriteFrame
	{
		/// <summary>
		/// Size of the frame's `Data`.
		/// </summary>
		Size Size { get; }

		/// <summary>
		/// Size of the entire frame including the frame's `Size`.
		/// Think of this like a picture frame.
		/// </summary>
		Size FrameSize { get; }

		float2 Offset { get; }
		byte[] Data { get; }
		bool DisableExportPadding { get; }
	}
}
