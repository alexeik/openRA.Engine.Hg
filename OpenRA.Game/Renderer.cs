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
using System.Linq;
using ImGuiNET;
using OpenRA.Graphics;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class Renderer : IDisposable
	{
		public SpriteRenderer WorldSpriteRenderer { get; private set; }
		public RgbaSpriteRenderer WorldRgbaSpriteRenderer { get; private set; }
		public RgbaColorRenderer WorldRgbaColorRenderer { get; private set; }
		public ModelRenderer WorldModelRenderer { get; private set; }
		public RgbaColorRenderer RgbaColorRenderer { get; private set; }
		public SpriteRenderer SpriteRenderer { get; private set; }
		public RgbaSpriteRenderer RgbaSpriteRenderer { get; private set; }
		public SpriteRenderer FontSpriteRenderer { get; private set; }
		public SpriteRenderer ImguiSpriteRenderer { get; private set; }
		public ShaderIF_API sproc;

		public PixelDumpRenderer PixelDumpRenderer { get; private set; }

		public IReadOnlyDictionary<string, SpriteFontMSDF> Fonts;

		public PlatformWindow Window { get; private set; }
		public GraphicsContext Context { get; private set; }

		internal int SheetSize { get; private set; }
		internal int TempBufferSize { get; private set; }

		readonly VertexBuffer<Vertex> RendererVertexBuffer;
		readonly Stack<Rectangle> scissorState = new Stack<Rectangle>();

		SheetBuilder fontSheetBuilder;
		readonly DefaultPlatform platform;

		float depthScale;
		float depthOffset;

		Size lastResolution = new Size(-1, -1);
		int2 lastScroll = new int2(-1, -1);
		float lastZoom = -1f;
		ITexture currentPaletteTexture;
		IBatchRenderer currentBatchRenderer;

		public Renderer(DefaultPlatform platform, GraphicSettings graphicSettings)
		{
			this.platform = platform;
			var resolution = GetResolution(graphicSettings);

			Window = platform.CreateWindow(new Size(resolution.Width, resolution.Height), graphicSettings.Mode, graphicSettings.BatchSize, Game.Settings.Graphics.DisableWindowsDPIScaling, Game.Settings.Game.LockMouseWindow, Game.Settings.Graphics.DisableWindowsRenderThread);
			Context = Window.Context;

			TempBufferSize = graphicSettings.BatchSize;
			SheetSize = graphicSettings.SheetSize;

			WorldSpriteRenderer = new SpriteRenderer("WorldSpriteRenderer", this, Context.CreateShader("combined")); // каждый имеет vertex массив, VAO это tempbuffer.
			WorldRgbaSpriteRenderer = new RgbaSpriteRenderer(WorldSpriteRenderer);
			WorldRgbaColorRenderer = new RgbaColorRenderer(WorldSpriteRenderer);
			WorldModelRenderer = new ModelRenderer(this, Context.CreateShader("model")); // каждый имеет vertex массив, VAO это tempbuffer 
			SpriteRenderer = new SpriteRenderer("SpriteRenderer", this, Context.CreateShader("combined")); // каждый имеет vertex массив, VAO это tempbuffer.
			RgbaSpriteRenderer = new RgbaSpriteRenderer(SpriteRenderer); // эти пишут в родительский VBO
			RgbaColorRenderer = new RgbaColorRenderer(SpriteRenderer); // эти пишут в родительский VBO
			FontSpriteRenderer = new SpriteRenderer("FontSpriteRenderer", this, Context.CreateShader("text")); // каждый имеет свой vertex массив, VAO это tempbuffer.
			ImguiSpriteRenderer = new SpriteRenderer("ImguiSpriteRenderer", this, Context.CreateShader("combined"));// дл€ ImGui
			PixelDumpRenderer = new PixelDumpRenderer("ImguiSpriteRenderer", this, Context.CreateShader("combined"));// дл€ ImGui
			
			sproc = new ShaderIF_API(); 
			IntPtr context = ImGui.CreateContext();
			ImGui.SetCurrentContext(context);

			RendererVertexBuffer = Context.CreateVertexBuffer(TempBufferSize,"Renderer");
		}

		static Size GetResolution(GraphicSettings graphicsSettings)
		{
			var size = (graphicsSettings.Mode == WindowMode.Windowed)
				? graphicsSettings.WindowedSize
				: graphicsSettings.FullscreenSize;
			return new Size(size.X, size.Y);
		}

		public void InitializeFonts(ModData modData)
		{
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();

			using (new PerfTimer("SpriteFonts"))
			{
				if (fontSheetBuilder != null)
				{
					fontSheetBuilder.Dispose();
				}
				fontSheetBuilder = new SheetBuilder(SheetType.BGRA, 512);

				Fonts = modData.Manifest.Fonts.ToDictionary(x => x.Key,
					x => new SpriteFontMSDF(x.Value.First, modData.DefaultFileSystem.Open(x.Value.First).ReadAllBytes(),
										x.Value.Second, Window.WindowScale, fontSheetBuilder)).AsReadOnly();
			}

			Window.OnWindowScaleChanged += (before, after) =>
			{
				Game.RunAfterTick(() =>
				{
					foreach (var f in Fonts)
						f.Value.SetScale(after);
				});
			};
			
		}

		public void InitializeDepthBuffer(MapGrid mapGrid)
		{
			// The depth buffer needs to be initialized with enough range to cover:
			//  - the height of the screen
			//  - the z-offset of tiles from MaxTerrainHeight below the bottom of the screen (pushed into view)
			//  - additional z-offset from actors on top of MaxTerrainHeight terrain
			//  - a small margin so that tiles rendered partially above the top edge of the screen aren't pushed behind the clip plane
			// We need an offset of mapGrid.MaximumTerrainHeight * mapGrid.TileSize.Height / 2 to cover the terrain height
			// and choose to use mapGrid.MaximumTerrainHeight * mapGrid.TileSize.Height / 4 for each of the actor and top-edge cases
			this.depthScale = mapGrid == null || !mapGrid.EnableDepthBuffer ? 0 :
				(float)Resolution.Height / (Resolution.Height + mapGrid.TileSize.Height * mapGrid.MaximumTerrainHeight);
			this.depthOffset = this.depthScale / 2;
		}

		public void BeginFrame(int2 scroll, float zoom)
		{
			Context.Clear();
			SetViewportParams(scroll, zoom);
		}

		public void SetViewportParams(int2 scroll, float zoom)
		{
			// PERF: Calling SetViewportParams on each renderer is slow. Only call it when things change.
			var resolutionChanged = lastResolution != Resolution;
			if (resolutionChanged)
			{
				lastResolution = Resolution;
				SpriteRenderer.SetViewportParams(lastResolution, 0f, 0f, 1f, int2.Zero);
				ImguiSpriteRenderer.SetViewportParams(lastResolution, 0f, 0f, 1f, int2.Zero);
				FontSpriteRenderer.SetViewportParams(lastResolution, 0f, 0f, 1f, int2.Zero);
				sproc.SetViewportParams(lastResolution, 0f, 0f, 1f, int2.Zero);
				
			}

			// If zoom evaluates as different due to floating point weirdness that's OK, setting the parameters again is harmless.
			if (resolutionChanged || lastScroll != scroll || lastZoom != zoom)
			{
				lastScroll = scroll;
				lastZoom = zoom;
				WorldSpriteRenderer.SetViewportParams(lastResolution, depthScale, depthOffset, zoom, scroll);
				WorldModelRenderer.SetViewportParams(lastResolution, zoom, scroll);
			}
		}

		public void SetPalette(HardwarePalette palette)
		{
			if (palette.Texture == currentPaletteTexture)
				return;

			Flush();
			currentPaletteTexture = palette.Texture;

			SpriteRenderer.SetPalette(currentPaletteTexture);
			FontSpriteRenderer.SetPalette(currentPaletteTexture);
			WorldSpriteRenderer.SetPalette(currentPaletteTexture);
			WorldModelRenderer.SetPalette(currentPaletteTexture);
			sproc.SetTexture("Palette", currentPaletteTexture);
		
		}
		public void ResetSproc()
		{
			sproc.SetTexture("Palette", currentPaletteTexture);
			sproc.SetViewportParams(lastResolution, 0f, 0f, 1f, int2.Zero);
			SpriteRenderer.shader.SetTexture("Palette", currentPaletteTexture);
			SpriteRenderer.SetViewportParams(lastResolution, 0f, 0f, 1f, int2.Zero);
			WorldSpriteRenderer.shader.SetTexture("Palette", currentPaletteTexture);
			WorldSpriteRenderer.SetViewportParams(lastResolution, depthScale, depthOffset, lastZoom, lastScroll);
			
		}

		public void EndFrame(IInputHandler inputHandler)
		{
			Flush();
			Window.PumpInput(inputHandler);
			Context.Present();
		}

		/// <summary>
		/// ћетод используетс€, дл€ перевалки данных из других spriterenderer в этот
		/// </summary>
		/// <param name="vertices">¬ертексы</param>
		/// <param name="numVertices">длина</param>
		/// <param name="type">тип</param>
		public void DrawBatchForVertexesSpriteRendererClasses(Vertex[] vertices, int numVertices, PrimitiveType type)
		{
			RendererVertexBuffer.ActivateVertextBuffer();
			RendererVertexBuffer.SetData(vertices, numVertices);
			RendererOpenVAO();
			Context.DrawPrimitives(type, 0, numVertices);
			RendererCloseVAO();
#if DEBUG_VERTEX
			Console.WriteLine("DrawBatchForVertexesSprite SpriteRenderer");
#endif
			PerfHistory.Increment("batches", 1);
		}

		public void RendererOpenVAO()
		{
			RendererVertexBuffer.ActivateVAO();
		}

		public void RendererCloseVAO()
		{
			RendererVertexBuffer.CloseVAO();
		}

		/// <summary>
		/// Ётот метод имеет пр€мой вызов, без tempBuffer.SetData(без внутреннего верт.буфера класса Renderer,
		/// поэтому тут используетс€ еще один vertices.Bind(),чтобы прив€зать верт.буфер от внешнего класса.
		/// Ќапример TerrainSpriteLayer.
		/// </summary>
		/// <typeparam name="T">в основном это Vertex</typeparam>
		/// <param name="VertBuffer">¬нешний буфер вертексов </param>
		/// <param name="firstVertex">от</param>
		/// <param name="numVertices">длина</param>
		/// <param name="type">тип треугольники и т.д.</param>
		public void DrawBatcForOpenGLVertexBuffer<T>(VertexBuffer<T> VertBuffer, int firstVertex, int numVertices, PrimitiveType type) where T : struct
		{
#if DEBUG_VERTEX
			Console.WriteLine("DrawBatcForOpenGL " + VertBuffer.ownername);
#endif
			Context.DrawPrimitives(type, firstVertex, numVertices);
			PerfHistory.Increment("batches", 1);
		}

		public void Flush()
		{
			if (PauseRender)
			{
				return;
			}
			CurrentBatchRenderer = null;
		}

		public Size Resolution { get { return Window.WindowSize; } }
		public float WindowScale { get { return Window.WindowScale; } }

		public interface IBatchRenderer { void Flush(); }
		public bool PauseRender = false;

		public IBatchRenderer CurrentBatchRenderer
		{
			get
			{
				return currentBatchRenderer;
			}

			set
			{
				if (currentBatchRenderer == value)
					return;
				if (currentBatchRenderer != null)
					currentBatchRenderer.Flush();
				currentBatchRenderer = value;
			}
		}

		public void EnableScissor(Rectangle rect)
		{
			// Must remain inside the current scissor rect
			if (scissorState.Any())
				rect = Rectangle.Intersect(rect, scissorState.Peek());

			Flush();
			Context.EnableScissor(rect.Left, rect.Top, rect.Width, rect.Height);
			scissorState.Push(rect);
		}

		public void DisableScissor()
		{
			scissorState.Pop();
			Flush();

			// Restore previous scissor rect
			if (scissorState.Any())
			{
				var rect = scissorState.Peek();
				Context.EnableScissor(rect.Left, rect.Top, rect.Width, rect.Height);
			}
			else
				Context.DisableScissor();
		}

		public void EnableDepthBuffer()
		{
			Flush();
			Context.EnableDepthBuffer();
		}

		public void DisableDepthBuffer()
		{
			Flush();
			Context.DisableDepthBuffer();
		}

		public void ClearDepthBuffer()
		{
			Flush();
			Context.ClearDepthBuffer();
		}

		public void GrabWindowMouseFocus()
		{
			Window.GrabWindowMouseFocus();
		}

		public void ReleaseWindowMouseFocus()
		{
			Window.ReleaseWindowMouseFocus();
		}

		public void Dispose()
		{
			WorldModelRenderer.Dispose();
			RendererVertexBuffer.Dispose();
			if (fontSheetBuilder != null)
				fontSheetBuilder.Dispose();
			if (Fonts != null)
				foreach (var font in Fonts.Values)
					font.Dispose();
			Window.Dispose();
		}

		public string GetClipboardText()
		{
			return Window.GetClipboardText();
		}

		public bool SetClipboardText(string text)
		{
			return Window.SetClipboardText(text);
		}

		public string GLVersion
		{
			get { return Context.GLVersion; }
		}

		public IFont CreateFont(byte[] data)
		{
			return platform.CreateFont(data);
		}
	}
}
