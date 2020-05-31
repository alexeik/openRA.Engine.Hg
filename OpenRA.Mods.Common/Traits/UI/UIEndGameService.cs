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
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Service for EndGame event.")]
	public class UIEndGameServiceInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new UIEndGame(init.Self, this, init.World); }
	}

	public class UIEndGame : IGameOver
	{
		UIEndGameServiceInfo info;
		Widget widget;
		Widget ingameRoot;
		Widget worldRoot;
		Widget playerRoot;

		public UIEndGame(Actor self, UIEndGameServiceInfo info, World world)
		{
			this.info = info;
		}

		void IGameOver.GameOver(World world)
		{

			widget = Ui.Root.Get("INGAME_ROOT");
			ingameRoot = widget.Get("INGAME_ROOT");
			worldRoot = ingameRoot.Get("WORLD_ROOT");
			playerRoot = worldRoot.Get("PLAYER_ROOT");

			var menuRoot = ingameRoot.Get("MENU_ROOT");

			world.GameOver += () =>
			{
				Ui.CloseWindow();
				menuRoot.RemoveChildren();

				if (world.LocalPlayer != null)
				{
					var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
					var missionData = world.WorldActor.Info.TraitInfoOrDefault<MissionDataInfo>();
					if (missionData != null && !(scriptContext != null && scriptContext.FatalErrorOccurred))
					{
						var video = world.LocalPlayer.WinState == WinState.Won ? missionData.WinVideo : missionData.LossVideo;
						if (!string.IsNullOrEmpty(video))
							Media.PlayFMVFullscreen(world, video, () => { });
					}
				}

				var optionsButton = playerRoot.GetOrNull<MenuButtonWidget>("OPTIONS_BUTTON");
				if (optionsButton != null)
					Sync.RunUnsynced(Game.Settings.Debug.SyncCheckUnsyncedCode, world, optionsButton.OnClick);
			};

			var objectives = world.LocalPlayer.PlayerActor.Info.TraitInfoOrDefault<MissionObjectivesInfo>();
			Game.RunAfterDelay(objectives != null ? objectives.GameOverDelay : 0, () =>
			{
				if (!Game.IsCurrentWorld(world))
					return;

				playerRoot.RemoveChildren();
				if (world.Type == WorldType.Capmaign)
				{
					Game.LoadWidget(world, "MISSIONBROWSER_PANEL", playerRoot, new WidgetArgs()
												{
													{ "onStart", () => { } },
													{ "onExit", () =>
													{
													 Ui.ResetAll();
													Ui.CloseWindow();
													Game.LoadWidget(world, "MAINMENU", Ui.Root, new WidgetArgs());
													} 
												}
												});
				}
				else
				{
					Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());
				}
			});
		}
	}
}
