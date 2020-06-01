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
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	public class AnimationString
	{
		public List<string> CurrentSequence { get; private set; }
		public string Name { get; private set; }
		public bool IsDecoration { get; set; }


		readonly Func<int> facingFunc;
		readonly Func<bool> paused;

		public int DefineTick = 40;
		int frame;
		bool backwards;
		bool tickAlways;
		int timeUntilNextFrame;
		Action tickFunc = () => { };

		public AnimationString(World world, string name)
			: this(world, name, () => 0) { }

		public AnimationString(World world, string name, Func<int> facingFunc)
			: this(world, name, facingFunc, null) { }

		public AnimationString(World world, string name, Func<bool> paused)
			: this(world, name, () => 0, paused) { }

		public AnimationString(World world, string name, Func<int> facingFunc, Func<bool> paused)
		{
		
			Name = name.ToLowerInvariant();
			this.facingFunc = facingFunc;
			this.paused = paused;
		}

		public void ChangeSequenceGroup(string seqgroupname)
		{
			Name = seqgroupname.ToLowerInvariant();
		}

		public int CurrentIndex { get { return backwards ? CurrentSequence.Count - frame - 1 : frame; } }

		public string Text
		{
			get
			{
				return CurrentSequence[CurrentIndex];
			}
		}



		public void Play(List<string> inputStrings)
		{
			CurrentSequence = inputStrings;
			PlayThen(inputStrings, null);
		}

		int CurrentSequenceTickOrDefault()
		{
			const int DefaultTick = 40; // 25 fps == 40 ms
			return CurrentSequence != null ? DefineTick : DefaultTick;
		}

		void PlaySequence(List<string> inputStrings)
		{
			CurrentSequence = inputStrings;
			timeUntilNextFrame = CurrentSequenceTickOrDefault();
		}

		public void PlayRepeating(List<string> inputStrings)
		{
			backwards = false;
			tickAlways = false;
			PlaySequence(inputStrings);

			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if (frame >= CurrentSequence.Count)
					frame = 0;
			};
		}



		/// <summary>
		/// Играет анимацию(показывает все кадры анимации в одну сторону) и потом делает какое-то действие в after делегате
		/// </summary>
		/// <param name="sequenceName">Имя sequence</param>
		/// <param name="after">Делегат, который выполнить после завершения анимации.</param>
		public void PlayThen(List<string> inputStrings, Action after)
		{
			backwards = false;
			tickAlways = false;
			PlaySequence(inputStrings);

			frame = 0;
			tickFunc = () =>
			{
				++frame;
				if (frame >= CurrentSequence.Count)
				{ 
					//если дошли до последнего кадра, то обнуляем tickFunc и вызывает after() делегат 
					frame = CurrentSequence.Count - 1;
					tickFunc = () => { };
					if (after != null) after();
				}
			};
		}

		public void PlayBackwardsThen(List<string> inputStrings, Action after)
		{
			PlayThen(inputStrings, after);
			backwards = true;
		}

		/// <summary>
		/// Меняет номер кадра анимации на тот, который получает при запуске анонимной функции tickfuncOverride, которая уйдет в tickFunc delegate of type Action.
		/// Sets tickAlways=true
		/// </summary>
		/// <param name="sequenceName">Название анимации</param>
		/// <param name="tickfuncOverride">Анонимная функция для расчета номера кадра</param>
		public void PlayFetchIndex(List<string> inputStrings, Func<int> tickfuncOverride)
		{
			backwards = false;
			tickAlways = true;
			PlaySequence(inputStrings);

			frame = tickfuncOverride(); // номер спрайта , который уйдет потом в Image свойство, через свойство CurrentFrame.
			tickFunc = () => frame = tickfuncOverride();
		}

		public void PlayFetchDirection(List<string> inputStrings, Func<int> direction)
		{
			tickAlways = false;
			PlaySequence(inputStrings);

			frame = 0;
			tickFunc = () =>
			{
				var d = direction();
				if (d > 0 && ++frame >= CurrentSequence.Count)
					frame = 0;

				if (d < 0 && --frame < 0)
					frame = CurrentSequence.Count - 1;
			};
		}

		/// <summary>
		/// Calls Tick(40) respecting tickAlways.
		/// if tickAlways==true => tickFunc().
		/// if tickAlways==false => respects timeUntilNextFrame cycle.
		/// Или берет Tick из sequence.yaml.
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


	}
}
