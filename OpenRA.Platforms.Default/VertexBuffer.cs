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
using System.Runtime.InteropServices;

namespace OpenRA.Platforms.Default
{
	public sealed class VertexBuffer<T> : VertexBufferUserBase<T>
			where T : struct
	{
		static readonly int VertexSize = Marshal.SizeOf(typeof(T));
		uint buffer;
		bool disposed;
		int LocalVertexArrayIndex;
		public string ownername;
		public VertexBuffer(int size, string ownername)
		{
			#if DEBUG_VERTEX
			Console.WriteLine("VB created owner: " + ownername);
#endif
			this.ownername = ownername;
			VAOReserveStack += 1; //помечаем, что резерв уменьшился.
			LocalVertexArrayIndex = VAOReserveStack;
			

			BindOnceOpen();

			// Generates a buffer with uninitialized memory.
			OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER,
					new IntPtr(VertexSize * size),
					IntPtr.Zero,
					OpenGL.GL_DYNAMIC_DRAW);
			OpenGL.CheckGLError();

			// We need to zero all the memory. Let's generate a smallish array and copy that over the whole buffer.
			var zeroedArrayElementSize = Math.Min(size, 2048);
			var ptr = GCHandle.Alloc(new T[zeroedArrayElementSize], GCHandleType.Pinned);
			try
			{
				for (var offset = 0; offset < size; offset += zeroedArrayElementSize)
				{
					var length = Math.Min(zeroedArrayElementSize, size - offset);
					OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
						new IntPtr(VertexSize * offset),
						new IntPtr(VertexSize * length),
						ptr.AddrOfPinnedObject());
					OpenGL.CheckGLError();
				}
			}
			finally
			{
				ptr.Free();
			}
			BindOnceClose();
		}

		public override void SetData(T[] data, int length)
		{
			SetData(data, 0, length);
		}

		public override void SetData(T[] data, int start, int length)
		{
			//Console.WriteLine("buffer SetData() in buffer number : " + buffer);

			var ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
					new IntPtr(VertexSize * start),
					new IntPtr(VertexSize * length),
					ptr.AddrOfPinnedObject());
			}
			finally
			{
				ptr.Free();
			}

			OpenGL.CheckGLError();

	
		}

		public override void SetData(IntPtr data, int start, int length)
		{
			//Console.WriteLine("buffer SetData2()" + buffer);
			//ActivateVAOBeforeGLDraw();
			//ActivateVertextBuffer();
			//BindOnce();
			OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
				new IntPtr(VertexSize * start),
				new IntPtr(VertexSize * length),
				data);
			OpenGL.CheckGLError();
		}

#region Vertex Array Managmenet

		public static int[] VAOList;
		public static int VAOReserveStack;

		public static void ReserveVAOList()
		{
			VAOList = new int[30];
			OpenGL.glGenVertexArrays(30, VAOList);
			OpenGL.CheckGLError();
		}
#endregion
		public override void ActivateVAO()
		{
			if (LocalVertexArrayIndex == 0)
			{
				Console.WriteLine("ERROR IN VAO Activate index==0!");
			}
			OpenGL.glBindVertexArray(VAOList[LocalVertexArrayIndex]);
#if DEBUG_VERTEX
			Console.WriteLine("glBindVertexArray: " + VAOList[LocalVertexArrayIndex] + "owner : " +ownername);
#endif
			OpenGL.CheckGLError();
		}

		public void CloseVAO()
		{
#if DEBUG_VERTEX
			Console.WriteLine("glBindVertexArray: closed" + "owner : " + ownername );
#endif
			OpenGL.glBindVertexArray(0);
			OpenGL.CheckGLError();
		}

		public void ActivateVertextBuffer()
		{
			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();

		}

		public override void BindOnceOpen()
		{
			VerifyThreadAffinity();
#if DEBUG_VERTEX
			Console.WriteLine("BindOnceOpen: " + VAOList[LocalVertexArrayIndex].ToString());
#endif
			OpenGL.glBindVertexArray(VAOList[LocalVertexArrayIndex]);
			OpenGL.CheckGLError();
			OpenGL.glGenBuffers(1, out buffer);
			OpenGL.CheckGLError();
			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.VertexPosAttributeIndex, 3, OpenGL.GL_FLOAT, false, VertexSize, IntPtr.Zero);
			OpenGL.CheckGLError();
			OpenGL.glEnableVertexAttribArray(Shader.VertexPosAttributeIndex);
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.TexCoordAttributeIndex, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(12));
			OpenGL.CheckGLError();
			OpenGL.glEnableVertexAttribArray(Shader.TexCoordAttributeIndex);
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.TexMetadataAttributeIndex, 2, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(28));  // последний аргумнет, это смещение измеряющиеся в байтах , которые указывают на начало столбика данных
			OpenGL.CheckGLError();
			OpenGL.glEnableVertexAttribArray(Shader.TexMetadataAttributeIndex);
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.VertexColorInfo, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(36));  // последний аргумнет, это смещение измеряющиеся в байтах , которые указывают на начало столбика данных
			OpenGL.CheckGLError();
			OpenGL.glEnableVertexAttribArray(Shader.VertexColorInfo);
			OpenGL.CheckGLError();


		}
		public void BindOnceClose()
		{
#if DEBUG_VERTEX
			Console.WriteLine("BindOnceClose: " + VAOList[LocalVertexArrayIndex].ToString());
#endif
			OpenGL.glBindVertexArray(0);
			OpenGL.CheckGLError();
		}
		public override void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;
			OpenGL.glDeleteBuffers(1, ref buffer);
			OpenGL.CheckGLError();
		}

	}
}
