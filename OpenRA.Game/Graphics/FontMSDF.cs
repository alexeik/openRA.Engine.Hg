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
		public static int Size = 64;
		public FontMSDF()
		{
			Texture = Game.Renderer.Context.CreateTexture2DArray();
			Texture.SetEmpty(Size, Size, 127);// просто заготовка gjl 127 слойный массив, каждый слой 96 на 96 пикселей.
		}
		public void LoadFontTexturesAsPng()
		{
			FileSystem.IReadOnlyPackage pack;
			string temp;

			string p1, p2;
			p1 = Game.ModData.Manifest.FontsMSDFBaseFolders["Section1"].First;
			p2 = Game.ModData.Manifest.FontsMSDFBaseFolders["Section1"].Second;

			if (Game.ModData.DefaultFileSystem.TryGetPackageContaining(p2, out pack, out temp))
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
							Console.WriteLine("Error loading char: {0} x {1}", f, Convert.ToInt32(f.Split('.')[0]));
						}
					}
				}
			}
			else
			{
				Console.WriteLine("MSDF fonts not loaded. Create font folder and write it to mod.yaml Packages for mod=" + Game.ModData.Manifest.Id + " path=");
			}
		}
		public void LoadFontTextures()
		{
			FileSystem.IReadOnlyPackage pack;
			string temp;
			if (Game.ModData.DefaultFileSystem.TryGetPackageContaining(@"120.bin", out pack, out temp))
			{
				foreach (string f in pack.Contents)
				{
					using (var stream = pack.GetStream(f))
					{
						using (MemoryStream ms = new MemoryStream())
						{
							try
							{
								stream.CopyTo(ms);
								Texture.SetData(ms.ToArray(), Size, Size, Convert.ToInt32(f.Split('.')[0]));
							}
							catch (Exception e)
							{
								Console.WriteLine("Error loading char: {0} x {1}", f, Convert.ToInt32(f.Split('.')[0]));
							}
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
