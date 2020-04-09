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

using OpenRA.Graphics;
using OpenRA.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;

namespace OpenRA.Platforms.Default
{
	public class ShaderInfo
	{
		public int type;
		public string name;
		public uint glid;

	}
	public class ShaderIF : ShaderBase
	{
		public const int VertexPosAttributeIndex = 0;
		public const int TexCoordAttributeIndex = 1;
		public const int TexMetadataAttributeIndex = 2;
		public const int VertexColorInfo = 3;

		/// <summary>
		/// содержит все sampler2D аргументы у шейдера в формате имя(строка), позиция аргумента(число)
		/// </summary>
		readonly Dictionary<string, int> samplers = new Dictionary<string, int>();
		readonly Dictionary<int, ITexture> textures = new Dictionary<int, ITexture>();
		uint program;
		public string sharerfilename;


		public Dictionary<string, FileSystemWatcher> watcherbox = new Dictionary<string, FileSystemWatcher>();
		public Dictionary<string, ShaderInfo> compiledbox = new Dictionary<string, ShaderInfo>();
		public Dictionary<string, string> candidates = new Dictionary<string, string>();

		public void AddWatcher(string shadername, string shaderext)
		{
			if (watcherbox.ContainsKey(shadername + "." + shaderext))
			{
				return;
			}
			FileSystemWatcher temp = new FileSystemWatcher(Path.Combine(Platform.GameDir, "glsl"), shadername + "." + shaderext);

			temp.Changed += FSW_Event_ShaderFileChanged;
			temp.EnableRaisingEvents = true;
			watcherbox.Add(shadername + "." + shaderext, temp);

		}
	
		public bool canenter = true;
		public bool UseCandidates()
		{
			if (canenter == false)
			{
				return false;
			}

			if (candidates.Count == 0)
			{
				return false;
			}

			foreach (string key in candidates.Keys)
			{
				canenter = false;
				try
				{
					ReCompileShader(key);
				}
				catch (Exception s)
				{
					canenter = true;
					return false;//exit without candidates.clear
				}
			
			}

			canenter = true;
			candidates.Clear();
			return true;
		}
		private void FSW_Event_ShaderFileChanged(object sender, FileSystemEventArgs e)
		{
			if (candidates.ContainsKey(e.Name))
			{
				return;
			}

			candidates.Add(e.Name, "");

		}
		public string ShaderTypeToFileExt(int type)
		{
			var ext = "";

			if (type == OpenGL.GL_VERTEX_SHADER)
			{
				ext = "vert";
			}
			if (type == OpenGL.GL_FRAGMENT_SHADER)
			{
				ext = "frag";
			}
			if (type == OpenGL.GL_GEOMETRY_SHADER)
			{
				ext = "geom";
			}
			return ext;
		}
		protected uint CompileShaderObject(int type, string name)
		{

			var ext = ShaderTypeToFileExt(type);
			var filename = Path.Combine(Platform.GameDir, "glsl", name + "." + ext);
			var code = File.ReadAllText(filename);

			var shader = OpenGL.glCreateShader(type);

			if (shader == 0)
			{

			}
			unsafe
			{
				var length = code.Length;
				OpenGL.glShaderSource(shader, 1, new string[] { code }, new IntPtr(&length));
			}

			OpenGL.glCompileShader(shader);


			int success;
			OpenGL.glGetShaderiv(shader, OpenGL.GL_COMPILE_STATUS, out success);
			if (success == OpenGL.GL_FALSE)
			{
				int len;
				OpenGL.glGetShaderiv(shader, OpenGL.GL_INFO_LOG_LENGTH, out len);
				var log = new StringBuilder(len);
				int length;
				OpenGL.glGetShaderInfoLog(shader, len, out length, log);

				Log.Write("graphics", "GL Info Log:\n{0}", log.ToString());
				throw new InvalidProgramException("Compile error in shader object '{0}'".F(filename));
			}

			return shader;
		}

		public void ReCompileShader(string shaderkey)
		{
			ShaderInfo si;
			si = compiledbox[shaderkey];
			CreateProgram(sharerfilename);
		}
	
		public void DetachAndDeleteShader(ShaderInfo si)
		{
			OpenGL.glDetachShader(program, si.glid);
			OpenGL.glDeleteShader(si.glid);

			//можно найти шейдер, отключить от программы, переподключить новый
		}
		public ShaderIF(string name)
		{
			sharerfilename = name;
			compiledbox.Add(name + "." + ShaderTypeToFileExt(OpenGL.GL_VERTEX_SHADER), new ShaderInfo() { name = name, type = OpenGL.GL_VERTEX_SHADER });
			compiledbox.Add(name + "." + ShaderTypeToFileExt(OpenGL.GL_FRAGMENT_SHADER), new ShaderInfo() { name = name, type = OpenGL.GL_FRAGMENT_SHADER });
			//compiledbox.Add(name + ShaderTypeToFileExt(OpenGL.GL_GEOMETRY_SHADER), new ShaderInfo() { name = name, type = OpenGL.GL_GEOMETRY_SHADER });

			CreateProgram(name);
		}

		public void CreateProgram(string name)
		{




			// Assemble program
			if (program == 0)
			{
				program = OpenGL.glCreateProgram();
			}
			else
			{
				uint prevpr = program;
				program = OpenGL.glCreateProgram();
				OpenGL.glDeleteProgram(prevpr);
			}
			
			foreach (ShaderInfo si in compiledbox.Values)
			{
				si.glid = CompileShaderObject(si.type, si.name);
				if (si.glid > 0)
				{
					AddWatcher(si.name, ShaderTypeToFileExt(si.type));
					OpenGL.glAttachShader(program, si.glid);
					OpenGL.glDeleteShader(si.glid);
				}
			}
			
			
			
			//можно экономить на этих командах glBindAttribLocation, убрать отсюда и перенес и в сам файл vert от шейдера
			// разметить эти позиции через layout(location=0) in vec4 aVertexPosition - означает привязка к 0 позиции формата вертбуфера 
			// к аргументу aVertexPosition в шейдере.

			OpenGL.glLinkProgram(program);
			int success;
			OpenGL.glGetProgramiv(program, OpenGL.GL_LINK_STATUS, out success);
			if (success == OpenGL.GL_FALSE)
			{
				int len;
				OpenGL.glGetProgramiv(program, OpenGL.GL_INFO_LOG_LENGTH, out len);

				var log = new StringBuilder(len);
				int length;
				OpenGL.glGetProgramInfoLog(program, len, out length, log);
				Log.Write("graphics", "GL Info Log:\n{0}", log.ToString());
				throw new InvalidProgramException("Link error in shader program '{0}'".F(name));
			}

			OpenGL.glUseProgram(program);

			int numUniforms;
			OpenGL.glGetProgramiv(program, OpenGL.GL_ACTIVE_UNIFORMS, out numUniforms);


			// забираем все переменные из shader и потом используем для связи с ними текстур
			var nextTexUnit = 0;
			samplers.Clear();

			for (var i = 0; i < numUniforms; i++)
			{
				int length, size;
				int type;
				var sb = new StringBuilder(128);
				OpenGL.glGetActiveUniform(program, i, 128, out length, out size, out type, sb);
				var sampler = sb.ToString();

				if (type == OpenGL.GL_SAMPLER_2D)
				{
					samplers.Add(sampler, nextTexUnit);

					var loc = OpenGL.glGetUniformLocation(program, sampler);
					OpenGL.glUniform1i(loc, nextTexUnit);

					nextTexUnit++;
				}
				if (type == OpenGL.GL_SAMPLER_2D_ARRAY)
				{
					samplers.Add(sampler, nextTexUnit);

					var loc = OpenGL.glGetUniformLocation(program, sampler);
					OpenGL.glUniform1i(loc, nextTexUnit);

					nextTexUnit++;
				}
			}

		}

		public override void PrepareRender()
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();

			// bind the textures
			foreach (var kv in textures)
			{
				if ((kv.Value as Texture).disposed)
				{
					continue; // on world restart some Textures can be Disposed. But assigned to Shader.
					// TODO: On Dispose Texture from Sheets need to add Clear textures on Shaders
				}
				OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + kv.Key);
				OpenGL.CheckGLError();

				if ((kv.Value as Texture).TextureType == OpenGL.GL_TEXTURE_2D_ARRAY)
				{
					OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D_ARRAY, ((ITextureInternal)kv.Value).ID);
					OpenGL.CheckGLError();
				}
				else
				{
					OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, ((ITextureInternal)kv.Value).ID);
					OpenGL.CheckGLError();
				}
			}

			OpenGL.CheckGLError();
			
		}

		/// <summary>
		/// Устанавливает связь между названием текстуры и именем сэмплера в шейдере.
		/// Должны быть одинаковые. Для упрощения так сделано.
		/// </summary>
		/// <param name="name">имя сэмплера в шейдере.</param>
		/// <param name="t">ссылка на текстуру.</param>
		public override void SetTexture(string name, ITexture t)
		{
			VerifyThreadAffinity();
			if (t == null)
				return;

			int texUnit;
			if (samplers.TryGetValue(name, out texUnit))
			{
				textures[texUnit] = t;
			}
		}

		public void ClearTextures()
		{
			textures.Clear();
		}

		public override void SetBool(string name, bool value)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform1i(param, value ? 1 : 0);
			OpenGL.CheckGLError();
		}

		public override void SetVec(string name, float x)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform1f(param, x);
			OpenGL.CheckGLError();
		}

		public override void SetVec(string name, float x, float y)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform2f(param, x, y);
			OpenGL.CheckGLError();
		}

		public override void SetVec(string name, float x, float y, float z)
		{
			VerifyThreadAffinity();
			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			OpenGL.glUniform3f(param, x, y, z);
			OpenGL.CheckGLError();
		}

		public override void SetVec(string name, float[] vec, int length)
		{
			VerifyThreadAffinity();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();
			unsafe
			{
				fixed (float* pVec = vec)
				{
					var ptr = new IntPtr(pVec);
					switch (length)
					{
						case 1: OpenGL.glUniform1fv(param, 1, ptr); break;
						case 2: OpenGL.glUniform2fv(param, 1, ptr); break;
						case 3: OpenGL.glUniform3fv(param, 1, ptr); break;
						case 4: OpenGL.glUniform4fv(param, 1, ptr); break;
						default: throw new InvalidDataException("Invalid vector length");
					}
				}
			}

			OpenGL.CheckGLError();
		}

		public override void SetMatrix(string name, float[] mtx)
		{
			VerifyThreadAffinity();
			if (mtx.Length != 16)
				throw new InvalidDataException("Invalid 4x4 matrix");

			OpenGL.glUseProgram(program);
			OpenGL.CheckGLError();
			var param = OpenGL.glGetUniformLocation(program, name);
			OpenGL.CheckGLError();

			unsafe
			{
				fixed (float* pMtx = mtx)
					OpenGL.glUniformMatrix4fv(param, 1, false, new IntPtr(pMtx));
			}

			OpenGL.CheckGLError();
		}
	}
}
