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
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;

namespace OpenRA.Primitives.Lua
{
	public struct WPosLua : IScriptBindable, ILuaAdditionBinding, ILuaSubtractionBinding, ILuaEqualityBinding, ILuaTableBinding
	{
		public readonly int X, Y, Z;
		#region Scripting interface

		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WPos a;
			WVec b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call WPos.Add(WPos, WVec) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, right.WrappedClrType().Name));

			return new LuaCustomClrObject(a + b);
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WPos a;
			var rightType = right.WrappedClrType();
			if (!left.TryGetClrValue(out a))
				throw new LuaException("Attempted to call WPos.Subtract(WPos, (WPos|WVec)) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, rightType.Name));

			if (rightType == typeof(WPos))
			{
				WPos b;
				right.TryGetClrValue(out b);
				return new LuaCustomClrObject(a - b);
			}
			else if (rightType == typeof(WVec))
			{
				WVec b;
				right.TryGetClrValue(out b);
				return new LuaCustomClrObject(a - b);
			}

			throw new LuaException("Attempted to call WPos.Subtract(WPos, (WPos|WVec)) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, rightType.Name));
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WPos a, b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				return false;

			return a == b;
		}

		public LuaValue this[LuaRuntime runtime, LuaValue key]
		{
			get
			{
				switch (key.ToString())
				{
					case "X": return X;
					case "Y": return Y;
					case "Z": return Z;
					default: throw new LuaException("WPos does not define a member '{0}'".F(key));
				}
			}

			set
			{
				throw new LuaException("WPos is read-only. Use WPos.New to create a new value");
			}
		}

		#endregion
	}

	
}
