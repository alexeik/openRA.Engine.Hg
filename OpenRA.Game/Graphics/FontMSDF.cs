using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public class FontMSDF
	{
		public ITexture Texture { get; private set; }
		public int Size = 64;
		public FontMSDF()
		{
			Texture = Game.Renderer.Context.CreateTexture2DArray();
			Texture.SetEmpty(Size, Size, 127);// просто заготовка gjl 127 слойный массив, каждый слой 96 на 96 пикселей.
		}
		public void LoadFontTextures()
		{
			
			FileSystem.IReadOnlyPackage pack;
			string temp;
			if (Game.ModData.DefaultFileSystem.TryGetPackageContaining(@"120.png", out pack, out temp))
			{
				foreach (string f in pack.Contents)
				{
					using (var stream = pack.GetStream(f))
					{
						Png pic;
						try
						{
							pic = new Png(stream);
							Texture.SetData(pic.Data, Size, Size, Convert.ToInt32(f.Split('.')[0]));
						}
						catch (Exception e)
						{
						}
					}
				}
			}
		}
		public void LoadFontTexturesByChar(byte[] data, int num)
		{
							Texture.SetData(data, Size, Size, num);
		}

	}
}
