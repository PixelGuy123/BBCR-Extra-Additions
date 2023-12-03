using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI.AssetManager;
using BBCRAdds.Main;
using System;
using MTM101BaldAPI;

namespace BBCRAdds.BepInEx
{
	[BepInPlugin(ModInfo.ModGUID, ModInfo.ModName, ModInfo.ModVersion)]
	public class BasePlugin : BaseUnityPlugin
	{
		ConfigEntry<bool> debug;
		void Awake()
		{

			Harmony harmony = new Harmony(ModInfo.ModGUID);


			var man = new GameObject("ModManager").AddComponent<ContentManager>();
			ContentManager.modPath = AssetManager.GetModPath(this);

			Logger.LogInfo(ContentManager.modPath);

			Logger.LogInfo($"{ModInfo.ModName} {ModInfo.ModVersion} has been initialized! Made by PixelGuy");

			debug = Config.Bind(
				"General",
				"Debug Mode",
				false,
				"Enables/Disables the debug mode on the game, when enabled, Baldi won\'t be able to kill you + Editor Mode Stuff"
				);

			man.DebugMode = debug.Value;

			DontDestroyOnLoad(man);

			harmony.PatchAllConditionals();

			if (man.DebugMode)
			{
				Logger.LogInfo("Yo here are some debugging strings for curiosity of developer lol");
				Logger.LogInfo($"Here\'s the binary 0001 in int: {Convert.ToInt32("0001", 2)}");
				Logger.LogInfo($"Here\'s the binary 0010 in int: {Convert.ToInt32("0010", 2)}");
				Logger.LogInfo($"Here\'s the binary 0111 in int: {Convert.ToInt32("0111", 2)}");
			}


		}
	}


	internal static class ModInfo
	{
		public const string ModGUID = "pixelguy.pixelmodding.baldiremastared.extraadditions";

		public const string ModName = "BBCR Extra Additions";

		public const string ModVersion = "0.0.0.1";
	}
}
