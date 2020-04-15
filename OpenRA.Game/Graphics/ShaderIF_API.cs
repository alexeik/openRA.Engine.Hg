using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class ShaderIF_API : ShaderIF
	{
		public ShaderIF_API() : base("ShaderIF")
		{

		}
		public Vertex2[] Verts = new Vertex2[100];
		public VertexIF GLVertexBuffer = new VertexIF(100, "ShaderIF");

		int nv = 0;
		int ni = 0;
		Sheet shtInputSlot1;

		public void AddCommand(int ShaderID, int CurrentFrame, int TotalFrames, int CurrentTime, int TotalTime, int2 iResolutionXY, 
			float3 TopLeftXYrect, float3 SpriteSize, Sprite SpriteUVCoords,PaletteReference PaletteIndex)
		{
			//рисуем в СисКоординат, где начало координат в ЛевомНижнемУглу
			//заполнение локального массива Verts 
			//нужно 4 записи сделать, на каждый угол прямоугольника.
			var a = new float3(TopLeftXYrect.X, TopLeftXYrect.Y + SpriteSize.Y, TopLeftXYrect.Z); //1
			var b = new float3(TopLeftXYrect.X + SpriteSize.X, TopLeftXYrect.Y + SpriteSize.Y, TopLeftXYrect.Z); //2
			var c = new float3(TopLeftXYrect.X, TopLeftXYrect.Y, TopLeftXYrect.Z + 0); //3
			var d = new float3(TopLeftXYrect.X + SpriteSize.X, TopLeftXYrect.Y , TopLeftXYrect.Z + 0); //4 

			float TextureStoreChannel=0;
			int TextureInputSlot = 1; // всегда 1 слот, так как пока поддержка только 1 текстурки будет.
			float palindex = 0;

			if (SpriteUVCoords != null)
			{
				shtInputSlot1 = SpriteUVCoords.Sheet;
				this.SetTexture("Texture0", shtInputSlot1.AssignOrGetOrSetDataGLTexture()); // заполняем текстуру0 для аргумента шейдера

				ni = 0;
				//Vertex mapping xyz,ShaderID,CurrentFrame,TotalFrames, iTime, TotalTime,iResolutionXY, TextureInputSlot , TextureStoreChannel,SpriteUVCoords

				//PaletteIndex.TextureIndex индекс в текстуре палитр


				if (SpriteUVCoords.Channel == TextureChannel.RGBA)
				{
					TextureStoreChannel = 4f; // это потому что, выбор текстуры зависит от 0.0 чисел в шейдере в методе vec4 Sample()
				}
				else
				{
					TextureStoreChannel = (byte)SpriteUVCoords.Channel;
				}
			}

			if (SpriteUVCoords == null)
			{
				SpriteUVCoords = new Sprite(new Sheet( SheetType.Indexed,new Size(0,0)), new Rectangle(new int2(0,0),new Size(0,0)) , 0);
			}
			if (PaletteIndex == null)
			{
				PaletteIndex = new PaletteReference("null", 0, null, new HardwarePalette());
				palindex = PaletteIndex.TextureIndex;

			}
			
		
			

			Verts[nv] = new Vertex2(a, ShaderID, CurrentFrame, TotalFrames, 0, 0, SpriteUVCoords.Size.X, SpriteUVCoords.Size.Y, TextureInputSlot, TextureStoreChannel, SpriteUVCoords.Left, SpriteUVCoords.Bottom, palindex, 0);
			Verts[nv + 1] = new Vertex2(b, ShaderID, CurrentFrame, TotalFrames, 0, 0, SpriteUVCoords.Size.X, SpriteUVCoords.Size.Y, TextureInputSlot, TextureStoreChannel, SpriteUVCoords.Right, SpriteUVCoords.Bottom, palindex, 0);
			Verts[nv + 2] = new Vertex2(c, ShaderID, CurrentFrame, TotalFrames, 0, 0, SpriteUVCoords.Size.X, SpriteUVCoords.Size.Y, TextureInputSlot, TextureStoreChannel, SpriteUVCoords.Left, SpriteUVCoords.Top, palindex, 0);
			Verts[nv + 3] = new Vertex2(d, ShaderID, CurrentFrame, TotalFrames, 0, 0, SpriteUVCoords.Size.X, SpriteUVCoords.Size.Y, TextureInputSlot, TextureStoreChannel, SpriteUVCoords.Right, SpriteUVCoords.Top, palindex, 0);
			nv += 4;

		}

		public void ExecCommandBuffer()
		{
			
			PrepareRender(); // opengl glBindTextures & glActiveTextures для шейдера
			GLVertexBuffer.ActivateVertextBuffer();
			unsafe
			{
				// The compiler / language spec won't let us calculate a pointer to
				// an offset inside a generic array T[], and so we are forced to
				// calculate the start-of-row pointer here to pass in to SetData.

				fixed (Vertex2* vPtr = &Verts[0])
				{
					GLVertexBuffer.SetData((IntPtr)(vPtr + 0), 0, nv);
				}
			}
			GLVertexBuffer.ActivateVAO();
			Game.Renderer.Context.SetBlendMode(BlendMode.Alpha);
			Game.Renderer.DrawBatcForOpenGLVertexBuffer(GLVertexBuffer, 0, nv, PrimitiveType.TriangleStrip);
			GLVertexBuffer.CloseVAO();
			nv = 0;
			//переливка из локального Verts в opengl VBO : VertexBuffer
			//вызов glDrawCall рисуем GL_TRIANGLE_STRIP
		}
		public void SetViewportParams(Size screen, float depthScale, float depthOffset, float zoom, int2 scroll)
		{
			this.SetVec("Scroll", scroll.X, scroll.Y, scroll.Y);
			this.SetVec("r1",
				zoom * 2f / screen.Width,
				-zoom * 2f / screen.Height,
				-depthScale * zoom / screen.Height);
			this.SetVec("r2", -1, 1, 1 - depthOffset);

			// Texture index is sampled as a float, so convert to pixels then scale
			this.SetVec("DepthTextureScale", 128 * depthScale * zoom / screen.Height);
		}
	}
}
