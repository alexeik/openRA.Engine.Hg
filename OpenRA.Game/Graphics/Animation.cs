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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	public class Animation
	{
		public ISpriteSequence CurrentSequence { get; private set; }
		public string Name { get; private set; }
		public bool IsDecoration { get; set; }

		readonly SequenceProvider sequenceProvider;
		readonly Func<int> facingFunc;
		readonly Func<bool> paused;

		int frame;
		bool backwards;
		bool tickAlways;
		int timeUntilNextFrame;
		Action tickFunc = () => { };

		public Animation(World world, string name)
			: this(world, name, () => 0) { }

		public Animation(World world, string name, Func<int> facingFunc)
			: this(world, name, facingFunc, null) { }

		public Animation(World world, string name, Func<bool> paused)
			: this(world, name, () => 0, paused) { }

		public Animation(World world, string name, Func<int> facingFunc, Func<bool> paused)
		{
			sequenceProvider = world.Map.Rules.Sequences;
			Name = name.ToLowerInvariant();
			this.facingFunc = facingFunc;
			this.paused = paused;
		}

		public void ChangeSequenceGroup(string seqgroupname)
		{
			Name = seqgroupname.ToLowerInvariant();
		}

		public int CurrentFrame { get { return backwards ? CurrentSequence.Length - frame - 1 : frame; } }
		public Sprite Image { get { return CurrentSequence.GetSprite(CurrentFrame, facingFunc()); } }

		public IRenderable[] Render(Actor actor, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale)
		{
			var imageRenderable = new SpriteRenderable(actor, Image, pos, offset, CurrentSequence.ZOffset + zOffset, palette, scale, IsDecoration);

			if (CurrentSequence.ShadowStart >= 0)
			{
				var shadow = CurrentSequence.GetShadow(CurrentFrame, facingFunc());
				var shadowRenderable = new SpriteRenderable(actor, shadow, pos, offset, CurrentSequence.ShadowZOffset + zOffset, palette, scale, true);
				return new IRenderable[] { shadowRenderable, imageRenderable };
			}

			return new IRenderable[] { imageRenderable };
		}

		public Rectangle ScreenBounds(WorldRenderer wr, WPos pos, WVec offset, float scale)
		{
			var xy = wr.ScreenPxPosition(pos) + wr.ScreenPxOffset(offset);
			var cb = CurrentSequence.Bounds;
			return Rectangle.FromLTRB(
				xy.X + (int)(cb.Left * scale),
				xy.Y + (int)(cb.Top * scale),
				xy.X + (int)(cb.Right * scale),
				xy.Y + (int)(cb.Bottom * scale));
		}

		public IRenderable[] Render(Actor actor, WPos pos, PaletteReference palette)
		{
			return Render(actor, pos, WVec.Zero, 0, palette, 1f);
		}

		public void Play(string sequenceName)
		{
			PlayThen(sequenceName, null);
		}

		int CurrentSequenceTickOrDefault()
		{
			const int DefaultTick = 40; // 25 fps == 40 ms
			return CurrentSequence != null ? CurrentSequence.Tick : DefaultTick;
		}

		void PlaySequence(string sequenceName)
		{
			CurrentSequence = GetSequence(sequenceName);
			timeUntilNextFrame = CurrentSequenceTickOrDefault();
		}

		public void PlayRepeating(string sequenceName)
		{
			backwards = false;
			tickAlways = false;
			PlaySequence(sequenceName);

			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if (frame >= CurrentSequence.Length)
					frame = 0;
			};
		}

		public bool ReplaceAnim(string sequenceName)
		{
			if (!HasSequence(sequenceName))
				return false;

			CurrentSequence = GetSequence(sequenceName);
			timeUntilNextFrame = Math.Min(CurrentSequenceTickOrDefault(), timeUntilNextFrame);
			frame %= CurrentSequence.Length;
			return true;
		}

		/// <summary>
		/// ������ ��������(���������� ��� ����� �������� � ���� �������) � ����� ������ �����-�� �������� � after ��������
		/// </summary>
		/// <param name="sequenceName">��� sequence</param>
		/// <param name="after">�������, ������� ��������� ����� ���������� ��������.</param>
		public void PlayThen(string sequenceName, Action after)
		{
			backwards = false;
			tickAlways = false;
			PlaySequence(sequenceName);

			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if (frame >= CurrentSequence.Length)
				{ 
					//���� ����� �� ���������� �����, �� �������� tickFunc � �������� after() ������� 
					frame = CurrentSequence.Length - 1;
					tickFunc = () => { };
					if (after != null) after();
				}
			};
		}

		public void PlayBackwardsThen(string sequenceName, Action after)
		{
			PlayThen(sequenceName, after);
			backwards = true;
		}

		/// <summary>
		/// ������ ����� ����� �������� �� ���, ������� �������� ��� ������� ��������� ������� tickfuncOverride, ������� ����� � tickFunc delegate of type Action.
		/// Sets tickAlways=true
		/// </summary>
		/// <param name="sequenceName">�������� ��������</param>
		/// <param name="tickfuncOverride">��������� ������� ��� ������� ������ �����</param>
		public void PlayFetchIndex(string sequenceName, Func<int> tickfuncOverride)
		{
			backwards = false;
			tickAlways = true;
			PlaySequence(sequenceName);

			frame = tickfuncOverride(); // ����� ������� , ������� ����� ����� � Image ��������, ����� �������� CurrentFrame.
			tickFunc = () => frame = tickfuncOverride();
		}

		public void PlayFetchDirection(string sequenceName, Func<int> direction)
		{
			tickAlways = false;
			PlaySequence(sequenceName);

			frame = 0;
			tickFunc = () =>
			{
				var d = direction();
				if (d > 0 && ++frame >= CurrentSequence.Length)
					frame = 0;

				if (d < 0 && --frame < 0)
					frame = CurrentSequence.Length - 1;
			};
		}

		/// <summary>
		/// Calls Tick(40) respecting tickAlways.
		/// if tickAlways==true => tickFunc().
		/// if tickAlways==false => respects timeUntilNextFrame cycle.
		/// ��� ����� Tick �� sequence.yaml.
		/// </summary>
		public void Tick()
		{
			if (paused == null || !paused())
				Tick(40); // tick one frame
		}

		/// <summary>
		/// Calls tickfuncOverride always if tickAlways=true, and some times if tickAlways=false.
		/// </summary>
		/// <param name="t">times to call tickFunc delegate </param>
		public void Tick(int t)
		{
			if (tickAlways)
				tickFunc();
			else
			{
				timeUntilNextFrame -= t;
				while (timeUntilNextFrame <= 0)
				{
					tickFunc();
					timeUntilNextFrame += CurrentSequenceTickOrDefault();
				}
			}
		}

		public void ChangeImage(string newImage, string newAnimIfMissing)
		{
			newImage = newImage.ToLowerInvariant();

			if (Name != newImage)
			{
				Name = newImage;
				if (!ReplaceAnim(CurrentSequence.Name))
					ReplaceAnim(newAnimIfMissing);
			}
		}

		public bool HasSequence(string seq)
		{
			return sequenceProvider.HasSequence(Name, seq);
		}

		public ISpriteSequence GetSequence(string sequenceName)
		{
			return sequenceProvider.GetSequence(Name, sequenceName);
		}

		public string GetRandomExistingSequence(string[] sequences, MersenneTwister random)
		{
			return sequences.Where(s => HasSequence(s)).RandomOrDefault(random);
		}
	}
}
