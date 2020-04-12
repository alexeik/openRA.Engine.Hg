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
using System.IO;
using System.Threading;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	public class Texture : ITextureInternal
	{
		volatile int managedThreadId;

		protected void SetThreadAffinity()
		{
			managedThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		protected void VerifyThreadAffinity()
		{
			if (managedThreadId != Thread.CurrentThread.ManagedThreadId)
				throw new InvalidOperationException("Cross-thread operation not valid: This method must only be called from the thread that owns this object.");
		}

		public uint texture;
		public TextureScaleFilter scaleFilter;
		public int TextureType;
		public uint ID { get { return texture; } }
		public Size Size { get; set; }

		public bool disposed;

		public TextureScaleFilter ScaleFilter
		{
			get
			{
				return scaleFilter;
			}

			set
			{
				VerifyThreadAffinity();
				if (scaleFilter == value)
					return;

				scaleFilter = value;
				PrepareTexture();
			}
		}

		public Texture()
		{
			OpenGL.glGenTextures(1, out texture);
			OpenGL.CheckGLError();
			SetThreadAffinity();
		}

		public virtual void PrepareTexture()
		{
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture);

			var filter = scaleFilter == TextureScaleFilter.Linear ? OpenGL.GL_LINEAR : OpenGL.GL_NEAREST;
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, filter);
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, filter);

			OpenGL.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
			OpenGL.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);

			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_BASE_LEVEL, 0);
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAX_LEVEL, 0);
		}

		public virtual void SetData(byte[] colors, int width, int height)
		{
			VerifyThreadAffinity();
			//if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
			//	throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			unsafe
			{
				fixed (byte* ptr = &colors[0])
				{
					var intPtr = new IntPtr((void*)ptr);
					PrepareTexture();
					OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, width, height,
						0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
					OpenGL.CheckGLError();
				}
			}
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, 0);
		}

		// An array of RGBA
		public virtual void SetData(uint[,] colors)
		{
			VerifyThreadAffinity();
			var width = colors.GetUpperBound(1) + 1;
			var height = colors.GetUpperBound(0) + 1;

			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			unsafe
			{
				fixed (uint* ptr = &colors[0, 0])
				{
					var intPtr = new IntPtr((void*)ptr);
					PrepareTexture();
					OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, width, height,
						0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
					OpenGL.CheckGLError();
				}
			}
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, 0);
		}

		public virtual byte[] GetData()
		{
			VerifyThreadAffinity();
			var data = new byte[4 * Size.Width * Size.Height];

			OpenGL.CheckGLError();
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture);
			unsafe
			{
				fixed (byte* ptr = &data[0])
				{
					var intPtr = new IntPtr((void*)ptr);
					OpenGL.glGetTexImage(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_BGRA,
						OpenGL.GL_UNSIGNED_BYTE, intPtr);
				}
			}

			OpenGL.CheckGLError();
			return data;
		}

		public virtual void SetEmpty(int width, int height)
		{
			VerifyThreadAffinity();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			PrepareTexture();
			OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, width, height,
				0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, IntPtr.Zero);
			OpenGL.CheckGLError();
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, 0);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;
			OpenGL.glDeleteTextures(1, ref texture);
		}

		public virtual void SetData(byte[] colors, int width, int height, int depth)
		{
			throw new NotImplementedException();
		}

		public virtual void SetEmpty(int width, int height, int depth)
		{
			throw new NotImplementedException();
		}
	}
}
