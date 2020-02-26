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
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;
using OpenRA.Support;

namespace OpenRA.Primitives.Lua
{
	/// <summary>
	/// 1d world distance - 1024 units = 1 cell.
	/// </summary>
	public struct WDistLua :  ILuaAdditionBinding, ILuaSubtractionBinding, ILuaEqualityBinding, ILuaTableBinding
	{
		public readonly int Length;

		#region Scripting interface
		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WDist a;
			WDist b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call WDist.Add(WDist, WDist) with invalid arguments.");

			return new LuaCustomClrObject(a + b);
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WDist a;
			WDist b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call WDist.Subtract(WDist, WDist) with invalid arguments.");

			return new LuaCustomClrObject(a - b);
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WDist a;
			WDist b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call WDist.Equals(WDist, WDist) with invalid arguments.");

			return a == b;
		}

		public LuaValue this[LuaRuntime runtime, LuaValue key]
		{
			get
			{
				switch (key.ToString())
				{
					case "Length": return Length;
					case "Range": Game.Debug("WDist.Range is deprecated. Use WDist.Length instead"); return Length;
					default: throw new LuaException("WDist does not define a member '{0}'".F(key));
				}
			}

			set
			{
				throw new LuaException("WDist is read-only. Use WDist.New to create a new value");
			}
		}
		#endregion
	}
}
