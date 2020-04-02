using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Platforms.Default;

namespace OpenRA.Graphics
{
	public class VertexIF : VertexBuffer<Vertex2>
	{
		public VertexIF(int size, string ownername) : base (size, ownername)
		{

		}
		public override void ApplyFormatOnVertexBuffer()
		{
			// format for vertex buffer

			VerifyThreadAffinity();
			
			OpenGL.glVertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, VertexSize, IntPtr.Zero);  //GL_FLOAT(4byte) * 3 = 12 vVertexPosition
			OpenGL.glEnableVertexAttribArray(0);
			
			OpenGL.glVertexAttribPointer(1, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(12)); //ShaderID * 1 = 4
			OpenGL.glEnableVertexAttribArray(1);
			
			OpenGL.glVertexAttribPointer(2, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(16)); //CurrentFrame
			OpenGL.glEnableVertexAttribArray(2);
			
			OpenGL.glVertexAttribPointer(3, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(20));  //TotalFrames
			OpenGL.glEnableVertexAttribArray(3);

			OpenGL.glVertexAttribPointer(4, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(24));  //iTime
			OpenGL.glEnableVertexAttribArray(4);

			OpenGL.glVertexAttribPointer(5, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(28));  //TotalTime
			OpenGL.glEnableVertexAttribArray(5);

			OpenGL.glVertexAttribPointer(6, 2, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(32));  //iResolutionXY
			OpenGL.glEnableVertexAttribArray(6);

			OpenGL.glVertexAttribPointer(7, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(40));  //TextureInputSlot
			OpenGL.glEnableVertexAttribArray(7);

			OpenGL.glVertexAttribPointer(8, 2, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(44));  //SpriteUVCoords
			OpenGL.glEnableVertexAttribArray(8);

			OpenGL.glVertexAttribPointer(9, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(52));  //PaletteIndex
			OpenGL.glEnableVertexAttribArray(9);

			OpenGL.glVertexAttribPointer(10, 1, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(56));  //Temp
			OpenGL.glEnableVertexAttribArray(10);

		}
	}
}
