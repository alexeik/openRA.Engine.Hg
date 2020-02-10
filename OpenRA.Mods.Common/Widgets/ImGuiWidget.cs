using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ImGuiWidget : Widget
	{
		private IntPtr fontAtlasID = (IntPtr)1;
		Sprite sp;
		Stopwatch fpsTimer;
		Vertex[] ve;

		public ImGuiWidget()
		{
			ImGuiSetup();
			fpsTimer = Stopwatch.StartNew();
		}
		public void ImGuiSetup()
		{
			ImGuiIOPtr io = ImGui.GetIO();

			io.KeyMap[(int)ImGuiKey.Tab] = (int)ConsoleKey.Tab;
			io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)ConsoleKey.LeftArrow;
			io.KeyMap[(int)ImGuiKey.RightArrow] = (int)ConsoleKey.RightArrow;
			io.KeyMap[(int)ImGuiKey.UpArrow] = (int)ConsoleKey.UpArrow;
			io.KeyMap[(int)ImGuiKey.DownArrow] = (int)ConsoleKey.DownArrow;
			io.KeyMap[(int)ImGuiKey.PageUp] = (int)ConsoleKey.PageUp;
			io.KeyMap[(int)ImGuiKey.PageDown] = (int)ConsoleKey.PageDown;
			io.KeyMap[(int)ImGuiKey.Home] = (int)ConsoleKey.Home;
			io.KeyMap[(int)ImGuiKey.End] = (int)ConsoleKey.End;
			io.KeyMap[(int)ImGuiKey.Delete] = (int)ConsoleKey.Delete;
			io.KeyMap[(int)ImGuiKey.Backspace] = (int)ConsoleKey.Backspace;
			io.KeyMap[(int)ImGuiKey.Enter] = (int)ConsoleKey.Enter;
			io.KeyMap[(int)ImGuiKey.Escape] = (int)ConsoleKey.Escape;
			io.KeyMap[(int)ImGuiKey.A] = (int)ConsoleKey.A;
			io.KeyMap[(int)ImGuiKey.C] = (int)ConsoleKey.C;
			io.KeyMap[(int)ImGuiKey.V] = (int)ConsoleKey.V;
			io.KeyMap[(int)ImGuiKey.X] = (int)ConsoleKey.X;
			io.KeyMap[(int)ImGuiKey.Y] = (int)ConsoleKey.Y;
			io.KeyMap[(int)ImGuiKey.Z] = (int)ConsoleKey.Z;

			io.DisplaySize = new Vector2(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);

			ImFontPtr ret;
			ret = io.Fonts.AddFontDefault();

			IntPtr pixels;
			int width;
			int height;
			int bytesPerPixel;
			io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bytesPerPixel);

			// Store our identifier
			var sheetBuilder = new SheetBuilder(SheetType.Indexed, width, height);

			var byteArray = new byte[width * height * bytesPerPixel];
			Marshal.Copy(pixels, byteArray, 0, width * height * bytesPerPixel);

			sp = sheetBuilder.AddRGBA(byteArray, new Size(width, height));

			// sp.Sheet.AsPng().Save("imguifont.png");
			io.Fonts.SetTexID(fontAtlasID);
			Game.Renderer.ImguiSpriteRenderer.SetRenderStateForSprite(sp); // записываем sheet от нашего спрайта в шейдерную коллекцию sheets, чтобы спрайт ушел в первый аргумент шейдера
		}
		public void FeedToIO(MouseInput mi)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.MousePos = new Vector2(mi.Location.X, mi.Location.Y);
			io.MouseDown[0] = false;
			io.MouseDown[1] = false;
			io.MouseDown[2] = false;
			switch (mi.Button)
			{
				case MouseButton.Left:
					io.MouseDown[0] = true;
					break;
				case MouseButton.Right:
					io.MouseDown[2] = true;
					break;
				case MouseButton.Middle:
					io.MouseDown[1] = true;
					break;
				case MouseButton.None:
					io.MouseDown[0] = false;
					io.MouseDown[1] = false;
					io.MouseDown[2] = false;
					break;
			}
		}
		public bool Depressed;
		public override bool HandleMouseInput(MouseInput mi)
		{
			FeedToIO(mi);

			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;
			else if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
			{
				// Only fire the onMouseUp event if we successfully lost focus, and were pressed
				// if (Depressed )
				// OnMouseUp(mi);
				return YieldMouseFocus(mi);
			}

			if (mi.Event == MouseInputEvent.Down)
			{
				// OnMouseDown returns false if the button shouldn't be pressed

				// OnMouseDown(mi);
				Depressed = true;
			}
			else if (mi.Event == MouseInputEvent.Move && HasMouseFocus)
				Depressed = RenderBounds.Contains(mi.Location);

			return Depressed;
		}

		public void SubmitImGuiToRenderer()
		{
			ImDrawDataPtr test;
			test = ImGui.GetDrawData();
			ImDrawListPtr test3;
			if (test.CmdListsCount > 0)
			{
				test3 = test.CmdListsRange[0];

				ImVector<ushort> ptr = test3.IdxBuffer;
				ImPtrVector<ImDrawVertPtr> t1 = test3.VtxBuffer;
				ImDrawVertPtr pp;

				ImDrawVert[] vertbuf = new ImDrawVert[t1.Size];

				unsafe
				{
					for (int i = 0; i < t1.Size; i++)
					{
						pp = t1[i];
						ImDrawVert ff = new ImDrawVert();

						// Console.WriteLine("vertex x:{0} y:{1}, u:{2} v:{3} color:{4}", ff.pos.X, ff.pos.Y, ff.uv.X, ff.uv.Y, ff.col);
						ff.pos = pp.pos;
						ff.uv = pp.uv;
						ff.col = pp.col;
						vertbuf[i] = ff;
					}
				}
				ushort[] indexbuf = new ushort[ptr.Size];

				for (int i = 0; i < ptr.Size; i++)
				{
					indexbuf[i] = (ushort)ptr[i];
				}

				// indexbuf хранит индекс координаты в vertbuf, рисует полигоны, поэтому уходит по три индекса. индекс составлен так, что координаты
				// перечисляются против часовой стрелки.
				ve = new Vertex[indexbuf.Length];
				int y;
				Color c;

				for (int i = 0; i < indexbuf.Length; i++)
				{
					y = indexbuf[i]; // индекс для vertbuf хранится в ячейке indexbuf.
					c = Color.FromArgb(vertbuf[y].col);
					ve[i] = new Vertex(vertbuf[y].pos.X, vertbuf[y].pos.Y, 0, vertbuf[y].uv.X, vertbuf[y].uv.Y, 0f, 0f, 0f, 4f, c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
				}

				// теперь нужно создать спрайт, чтобы вызывать Renderer.DrawSprite для занесения данных в VAO
				// типа строчка в VBO появляется как желание нарисовать спрайт. В данном случае спрайт это должна быть буква.
				// Но нет. Тут просто линия цветные
				Game.Renderer.ImguiSpriteRenderer.DrawRGBAVertices(ve);
			}
		}

		/// <summary>
		/// U have to call this method inside Imgui.Begin. <-> Imgui.End block.
		/// </summary>
		public void UpdateWidgetSize()
		{
			Vector2 newpos = ImGui.GetWindowPos();
			Vector2 newsize = ImGui.GetWindowSize();
			this.Bounds.X = Convert.ToInt16(newpos.X);
			this.Bounds.Y = Convert.ToInt16(newpos.Y);
			this.Bounds.Width = Convert.ToInt16(newsize.X);
			this.Bounds.Height = Convert.ToInt16(newsize.Y);
		}

		/// <summary>
		/// U have to call this method in the end of your widget Draw method.
		/// </summary>
		public override void Draw()
		{
			ImGui.Render();
			SubmitImGuiToRenderer();
		}
	}
}
