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

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Projectiles
{
	[Desc("Not a sprite, but an engine effect.")]
	public class LaserZapInfo : IProjectileInfo
	{
		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(86);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape Shape = BeamRenderableShape.Cylindrical;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The maximum duration (in ticks) of the beam's existence.")]
		public readonly int Duration = 10;

		[Desc("Total time-frame in ticks that the beam deals damage every DamageInterval.")]
		public readonly int DamageDuration = 1;

		[Desc("The number of ticks between the beam causing warhead impacts in its area of effect.")]
		public readonly int DamageInterval = 1;

		public readonly bool UsePlayerColor = false;

		[Desc("Color of the beam.")]
		public readonly Color Color = Color.Red;

		[Desc("Beam follows the target.")]
		public readonly bool TrackTarget = true;

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Beam can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("Draw a second beam (for 'glow' effect).")]
		public readonly bool SecondaryBeam = false;

		[Desc("The width of the zap.")]
		public readonly WDist SecondaryBeamWidth = new WDist(86);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape SecondaryBeamShape = BeamRenderableShape.Cylindrical;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int SecondaryBeamZOffset = 0;

		public readonly bool SecondaryBeamUsePlayerColor = false;

		[Desc("Color of the secondary beam.")]
		public readonly Color SecondaryBeamColor = Color.Red;

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[SequenceReference("HitAnim")]
		[Desc("Sequence of impact animation to use.")]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		[Desc("Image containing launch effect sequence.")]
		public readonly string LaunchEffectImage = null;

		[SequenceReference("LaunchEffectImage")]
		[Desc("Launch effect sequence to play.")]
		public readonly string LaunchEffectSequence = null;

		[PaletteReference]
		[Desc("Palette to use for launch effect.")]
		public readonly string LaunchEffectPalette = "effect";

		public IProjectile Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.Owner.Color : Color;
			return new LaserZap(this, args, c);
		}
	}

	public class LaserZap : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly LaserZapInfo info;
		readonly Animation hitanim;
		readonly Color color;
		readonly Color secondaryColor;
		readonly bool hasLaunchEffect;
		int ticks;
		int interval;
		bool showHitAnim;

		[Sync]
		WPos target;

		[Sync]
		WPos source;

		public LaserZap(LaserZapInfo info, ProjectileArgs args, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;
			secondaryColor = info.SecondaryBeamUsePlayerColor ? args.SourceActor.Owner.Color : info.SecondaryBeamColor;
			target = args.PassiveTarget;
			source = args.Source;

			if (info.Inaccuracy.Length > 0)
			{
				var inaccuracy = OpenRA.Mods.Common.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
				var maxOffset = inaccuracy * (target - source).Length / args.Weapon.Range.Length;
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxOffset / 1024;
			}

			if (!string.IsNullOrEmpty(info.HitAnim))
			{
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);
				showHitAnim = true;
			}

			hasLaunchEffect = !string.IsNullOrEmpty(info.LaunchEffectImage) && !string.IsNullOrEmpty(info.LaunchEffectSequence);
		}

		public void Tick(World world)
		{
			source = args.CurrentSource();

			if (hasLaunchEffect && ticks == 0)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(args.CurrentSource, args.CurrentMuzzleFacing, world,
					info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));

			// Beam tracks target
			if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.PositionClosestTo(source);

			// Check for blocking actors
			WPos blockedPos;
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target,
				info.Width, out blockedPos))
			{
				target = blockedPos;
			}

			if (ticks < info.DamageDuration && --interval <= 0)
			{
				args.Weapon.Impact(Target.FromPos(target), args.SourceActor, args.DamageModifiers);
				interval = info.DamageInterval;
			}

			if (showHitAnim)
			{
				if (ticks == 0)
					hitanim.PlayThen(info.HitAnimSequence, () => showHitAnim = false);

				hitanim.Tick();
			}

			if (++ticks >= info.Duration && !showHitAnim)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(source))
				yield break;

			if (ticks < info.Duration)
			{
				var rc = Color.FromArgb((info.Duration - ticks) * color.A / info.Duration, color);
				yield return new BeamRenderable(source, info.ZOffset, target - source, info.Shape, info.Width, rc);

				if (info.SecondaryBeam)
				{
					var src = Color.FromArgb((info.Duration - ticks) * secondaryColor.A / info.Duration, secondaryColor);
					yield return new BeamRenderable(source, info.SecondaryBeamZOffset, target - source,
						info.SecondaryBeamShape, info.SecondaryBeamWidth, src);
				}
			}

			if (showHitAnim)
				foreach (var r in hitanim.Render(args.SourceActor, target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
