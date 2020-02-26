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

using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	public class DefaultPlatform 
	{
		public PlatformWindow CreateWindow(Size size, WindowMode windowMode, int batchSize,bool DisableWindowsDPIScaling, 
			bool LockMouseWindow, bool DisableWindowsRenderThread)
		{
			return new PlatformWindow(size, windowMode, batchSize, DisableWindowsDPIScaling, LockMouseWindow, DisableWindowsRenderThread);
		}

		public ISoundEngine CreateSound(string device)
		{
			return new OpenAlSoundEngine(device);
		}

		public IFont CreateFont(byte[] data)
		{
			return new FreeTypeFont(data);
		}
	}
}
