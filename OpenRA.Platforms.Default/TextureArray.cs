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
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	public class TextureArray : Texture, ITextureInternal
	{
		public int CapacityDepth = 256;
		public int CapacityReserved = 0;

		public override void PrepareTexture()
		{
			var filter = scaleFilter == TextureScaleFilter.Linear ? OpenGL.GL_LINEAR : OpenGL.GL_NEAREST;

			TextureType = OpenGL.GL_TEXTURE_2D_ARRAY;
			OpenGL.CheckGLError();
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, texture);
			OpenGL.CheckGLError();

			//var filter = OpenGL.GL_NEAREST;
			//var filter = OpenGL.GL_LINEAR;
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D_ARRAY, OpenGL.GL_TEXTURE_MAG_FILTER, filter);
			OpenGL.CheckGLError();
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D_ARRAY, OpenGL.GL_TEXTURE_MIN_FILTER, filter);
			OpenGL.CheckGLError();

			OpenGL.glTexParameterf(OpenGL.GL_TEXTURE_2D_ARRAY, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
			OpenGL.CheckGLError();
			OpenGL.glTexParameterf(OpenGL.GL_TEXTURE_2D_ARRAY, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
			OpenGL.CheckGLError();

		}

		/// <summary>
		/// По-штучная инициализация каждого слоя массива Texture2DArray.
		/// </summary>
		/// <param name="colors">массив байтов.</param>
		/// <param name="width">ширина текстурки.</param>
		/// <param name="height">высота текстурки.</param>
		/// <param name="depth">слои в массиве.</param>
		public override void SetData(byte[] colors, int width, int height, int depth)
		{
			VerifyThreadAffinity();
			Size = new Size(width, height);
			PrepareTexture();
			unsafe
			{
				fixed (byte* ptr = &colors[0])
				{
					var intPtr = new IntPtr((void*)ptr);
					OpenGL.glTexSubImage3D(OpenGL.GL_TEXTURE_2D_ARRAY, 0, 0, 0, depth, width, height, 1, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
					OpenGL.CheckGLError();
				}
			}
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, 0);
		}

		// An array of RGBA
		public void SetData(uint[,] colors, int depth)
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
					OpenGL.glTexImage3D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA32F, width, height, depth,
						0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
					OpenGL.CheckGLError();

				}
			}
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, 0);
		}

		public override byte[] GetData()
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

		/// <summary>
		/// Инициализация текстуры под будущее заполнение с помощью public void SetData(byte[] colors, int width, int height, int depth).
		/// </summary>
		/// <param name="width">размер текстурки ширина.</param>
		/// <param name="height">размер текстурки высота.</param>
		/// <param name="depth">количество слоев в массиве.</param>
		public override void SetEmpty(int width, int height, int depth)
		{
			VerifyThreadAffinity();

			PrepareTexture();
			OpenGL.glTexImage3D(OpenGL.GL_TEXTURE_2D_ARRAY, 0, OpenGL.GL_RGBA, width, height, depth,
						0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, IntPtr.Zero);
			OpenGL.CheckGLError();
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, 0);
		}
	}
}
