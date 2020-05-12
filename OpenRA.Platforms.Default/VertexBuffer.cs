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
using System.Runtime.InteropServices;

namespace OpenRA.Platforms.Default
{
	public class VertexBuffer<T> : VertexBufferUserBase<T>
			where T : struct
	{
		public static readonly int VertexSize = Marshal.SizeOf(typeof(T));
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
			GraphicsContext.VAOReserveStack += 1; //помечаем, что резерв уменьшился.
			LocalVertexArrayIndex = GraphicsContext.VAOReserveStack;
			

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


#endregion
		public override void ActivateVAO()
		{
			if (LocalVertexArrayIndex == 0)
			{
				Console.WriteLine("ERROR IN VAO Activate index==0!");
			}
			OpenGL.glBindVertexArray(GraphicsContext.VAOList[LocalVertexArrayIndex]);
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
			OpenGL.glBindVertexArray(GraphicsContext.VAOList[LocalVertexArrayIndex]);
			OpenGL.CheckGLError();
			OpenGL.glGenBuffers(1, out buffer);
			OpenGL.CheckGLError();
			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();
			ApplyFormatOnVertexBuffer();


		}
		public virtual void ApplyFormatOnVertexBuffer()
		{
			List<VertexLayout> vl = new List<VertexLayout>();
			vl.Add(new VertexLayout() { SlotCount = 3, SlotDataType = OpenGL.GL_FLOAT }); //aVertexPosition
			vl.Add(new VertexLayout() { SlotCount = 2, SlotDataType = OpenGL.GL_FLOAT }); //aVertexTexCoord
			vl.Add(new VertexLayout() { SlotCount = 2, SlotDataType = OpenGL.GL_FLOAT }); //aVertexTexCoordSecond
			vl.Add(new VertexLayout() { SlotCount = 1, SlotDataType = OpenGL.GL_FLOAT }); //aVertexPaletteIndex
			vl.Add(new VertexLayout() { SlotCount = 1, SlotDataType = OpenGL.GL_FLOAT }); //aVertexTexMetadata
			vl.Add(new VertexLayout() { SlotCount = 1, SlotDataType = OpenGL.GL_FLOAT }); //aVertexDrawmode
			vl.Add(new VertexLayout() { SlotCount = 1, SlotDataType = OpenGL.GL_FLOAT }); //aVertexTexMetadata2
			vl.Add(new VertexLayout() { SlotCount = 4, SlotDataType = OpenGL.GL_FLOAT }); //aVertexColorInfo
			vl.Add(new VertexLayout() { SlotCount = 4, SlotDataType = OpenGL.GL_FLOAT }); //aVertexUVFillRect

			int slotbytesize = 4;
			int offset = 0;
			for ( int i=0;i<vl.Count;i++)
			{

				OpenGL.glVertexAttribPointer(i, vl[i].SlotCount, vl[i].SlotDataType, false, VertexSize, new IntPtr(offset));
				OpenGL.glEnableVertexAttribArray(i);
				offset += vl[i].SlotCount * slotbytesize;
			}

			//OpenGL.glVertexAttribPointer(Shader.VertexPosAttributeIndex, 3, OpenGL.GL_FLOAT, false, VertexSize, IntPtr.Zero);
			//OpenGL.glEnableVertexAttribArray(Shader.VertexPosAttributeIndex);
			//OpenGL.glVertexAttribPointer(Shader.TexCoordAttributeIndex, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(12));
			//OpenGL.glEnableVertexAttribArray(Shader.TexCoordAttributeIndex);
			//OpenGL.glVertexAttribPointer(Shader.TexMetadataAttributeIndex, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(28));  // последний аргумнет, это смещение измеряющиеся в байтах , которые указывают на начало столбика данных
			//OpenGL.glEnableVertexAttribArray(Shader.TexMetadataAttributeIndex);
			//OpenGL.glVertexAttribPointer(Shader.VertexColorInfo, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(44));  // последний аргумнет, это смещение измеряющиеся в байтах , которые указывают на начало столбика данных
			//OpenGL.glEnableVertexAttribArray(Shader.VertexColorInfo);
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
			GraphicsContext.VAOReserveStack -= 1; //помечаем, что резерв уменьшился.
		}

	}
	public class VertexLayout
	{
		public int SlotCount;
		public int SlotDataType;
	}
}
