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
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;
using OpenRA.Support;

namespace OpenRA.Primitives.Lua
{
	public struct WVecLua : IScriptBindable, ILuaAdditionBinding, ILuaSubtractionBinding, ILuaUnaryMinusBinding, ILuaEqualityBinding, ILuaTableBinding
	{
		public readonly int X, Y, Z;
		public WVecLua(int x, int y, int z) { X = x; Y = y; Z = z; }
		#region Scripting interface
		public static WVecLua operator -(WVecLua a) { return new WVecLua(-a.X, -a.Y, -a.Z); }
		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WVec a, b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call WVec.Add(WVec, WVec) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, right.WrappedClrType().Name));

			return new LuaCustomClrObject(a + b);
		}
		public long LengthSquared { get { return (long)X * X + (long)Y * Y + (long)Z * Z; } }

		public WAngle Yaw
		{
			get
			{
				if (LengthSquared == 0)
					return WAngle.Zero;

				// OpenRA defines north as -y
				return WAngle.ArcTan(-Y, X) - new WAngle(256);
			}
		}
		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WVec a, b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call WVec.Subtract(WVec, WVec) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, right.WrappedClrType().Name));

			return new LuaCustomClrObject(a - b);
		}

		public LuaValue Minus(LuaRuntime runtime)
		{
			return new LuaCustomClrObject(-this);
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WVec a, b;
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
					case "Facing": return Yaw.Facing;
					default: throw new LuaException("WVec does not define a member '{0}'".F(key));
				}
			}

			set
			{
				throw new LuaException("WVec is read-only. Use WVec.New to create a new value");
			}
		}

		#endregion
	}
}
