﻿using HarmonyLib;
using UnityEngine;
using BBCRAdds.Main;
using TMPro;
using System;
using System.Collections.Generic;
using MTM101BaldAPI;
using BBCRAdds.Extensions;

namespace BBCRAdds.Patches
{
	public class ConditionalForDebugPatch : ConditionalPatch
	{
		public override bool ShouldPatch()
		{
			return ContentManager.i.DebugMode;
		}
	}



	// ---------- Debugging Section ----------
	[ConditionalForDebugPatch]
	[HarmonyPatch(typeof(Baldi), "OnTriggerEnter")]
	internal class BaldiDebuggingPatch
	{
		private static bool Prefix()
		{
			return false;
		}
	}

	[ConditionalForDebugPatch]
	[HarmonyPatch(typeof(PlayerMovement), "Update")]
	internal class PlayerGottaGoFast
	{
		private static void Postfix(PlayerMovement __instance)
		{
			__instance.walkSpeed = walkSpeed;
			__instance.runSpeed = runSpeed;
		}

		const float walkSpeed = 100f;
		const float runSpeed = 200f;
	}

	[ConditionalForDebugPatch]
	[HarmonyPatch(typeof(NullNPC), "OnTriggerEnter")]
	internal class NullCantKillYouHA
	{
		private static bool Prefix(Navigator ___navigator, NullNPC __instance, Collider other)
		{
			if (___navigator.passableObstacles.Contains(PassableObstacle.Window) && other.CompareTag("Window"))
			{
				other.GetComponent<Window>().Break(false);
				AccessTools.Method(typeof(NullNPC), "SpeechCheck", new Type[] { typeof(NullPhrase), typeof(float) }).Invoke(__instance, new object[] { NullPhrase.Hide, 0.04f }); // Invokes the speech thing aswell lol (idk why I'm even caring to this, it's debug mode)
			}
			return false;
		}
	}

	// ------ Map Stuff ------------

	[HarmonyPatch(typeof(GameInitializer), "Initialize")]
	internal class ChangeMapParameters
	{
		private static void Prefix(ref SceneObject ___sceneObject)
		{

			___sceneObject = Singleton<CoreGameManager>.Instance.sceneObject;
			MapDataHolder.ReadDataFromFolder(___sceneObject.levelAsset != null ? ___sceneObject.levelAsset.name + MapDataHolder.levelAssetTag : ___sceneObject.levelContainer.name + MapDataHolder.levelContainerTag); // Read the data and substitute everything on this level

			if (ClassicLoadingScreenPatch.i != null) // TO-DO: When mod initialization loads the data, remove this
			{
				ClassicLoadingScreenPatch.i.transform.Find("Text").GetChild(0).GetComponent<TextMeshProUGUI>().text = defaultloadthingy;
			}

			if (___sceneObject.levelAsset != null)
				EditorMode.tilesToSave = new List<TileData>(___sceneObject.levelAsset.tile);
			else if (___sceneObject.levelContainer != null)
				EditorMode.tilesToSave = new List<TileData>(___sceneObject.levelContainer.tile);
		}

		const string defaultloadthingy = "L O A D";

	}


	[HarmonyPatch(typeof(BaseGameManager), "Initialize")]
	internal class InitDebuggingEditor
	{
		private static void Postfix(EnvironmentController ___ec)
		{
			EnvironmentData.ec = ___ec;

			if (!ContentManager.i.DebugMode) return; // Editor hud down here


			var hud = Singleton<CoreGameManager>.Instance.GetHud(0);
			if (hud.transform.Find("EditorHud") == null)
			{
				var newText = UnityEngine.Object.Instantiate(hud.transform.Find("Notebook Text").gameObject); // Notebook Text
				newText.name = "EditorHud";
				newText.transform.SetParent(hud.transform);
				newText.transform.localScale = Vector3.one;
				newText.GetComponent<TextMeshProUGUI>().autoSizeTextContainer = true;
			}

			
		}
	}

	[HarmonyPatch(typeof(HudManager), "Update")]
	internal class EditorMode
	{
		private static void Postfix(HudManager __instance)
		{
			if (!ContentManager.i.DebugMode) return; // All below being editor

			if (text == null)
			{
				text = __instance.transform.Find("EditorHud").GetComponent<TextMeshProUGUI>();
			}

			if (text == null) return;

			if (text.transform.localPosition != pos)
			{
				text.transform.localPosition = pos;
			}

			var playerPos = IntVector2.GetGridPosition(Singleton<CoreGameManager>.Instance.GetPlayer(0).transform.position); // just register the player pos for later usage

			string sData = new string(data);
			text.text = sData + $"\nsaved: {beenSaved}\nCurrent Room Type:{(room != null ? room.name : "none")}\n Pos: {playerPos.x},{playerPos.z}";

			if (Input.GetKeyDown(KeyCode.Alpha1)) // A 4-bit flag (for tiles)
				data[0] = data[0] == '0' ? '1' : '0';
			if (Input.GetKeyDown(KeyCode.Alpha2))
				data[1] = data[1] == '0' ? '1' : '0';
			if (Input.GetKeyDown(KeyCode.Alpha3))
				data[2] = data[2] == '0' ? '1' : '0';
			if (Input.GetKeyDown(KeyCode.Alpha4))
				data[3] = data[3] == '0' ? '1' : '0';

			

			if (Input.GetKeyDown(KeyCode.UpArrow)) // Switch the room inside the room list
			{
				roomIdx++;
				roomIdx %= EnvironmentData.ec.rooms.Count;
			}
			if (Input.GetKeyDown(KeyCode.DownArrow)) // Switch the room inside the room list
			{
				roomIdx--;
				roomIdx %= EnvironmentData.ec.rooms.Count;
				if (roomIdx < 0) roomIdx = EnvironmentData.ec.rooms.Count - 1;
			}
			if (Input.GetKeyDown(KeyCode.C)) // Copy the current tile's room into reference
			{
				var r = EnvironmentData.ec.TileFromPos(playerPos);
				if (r != null && r.room != null)
					roomIdx = EnvironmentData.ec.rooms.IndexOf(r.room);
			}

			room = EnvironmentData.ec.rooms[roomIdx];

			if (Input.GetKeyDown(KeyCode.R)) // Creates a roomData based on the current one (to easily add it
			{
				var sceneObject = Singleton<CoreGameManager>.Instance.sceneObject;
				if (sceneObject.levelAsset != null)
				{
					var foundRoom = sceneObject.levelAsset.rooms.Find(x => x.name.Contains(room.name));
					if (foundRoom != default)
					{
						Debug.Log("Created Room!");
						roomDatas.Add(foundRoom.CopyRoomData(true));
					}
				}
			}

			if (Input.GetKeyDown(KeyCode.L)) // Creates lighting at current position
			{
				Debug.Log("Created Lighting!");
				lightDatas.Add(new LightSourceData()
				{
					prefab = null,
					position = playerPos,
					color = new Color(1f, 1f, 1f, 0f),
					strength = 1
				});
			}


			if (Input.GetKeyDown(KeyCode.Z)) // Create tile
			{
				var type = Convert.ToInt32(sData, 2);
				EnvironmentData.ec.CreateTile(type, room.transform, playerPos, room, true);
				var index = tilesToSave.FindIndex(x => x.pos == playerPos);
				if (index == -1) // If it doesn't exists, just add it for save list
				{
					tilesToSave.Add(new TileData()
					{
						pos = playerPos,
						type = type,
						roomId = EnvironmentData.ec.rooms.IndexOf(room)
					});
				}
				else // Else, if the tile exists and has been added, just replace it
				{
					tilesToSave[index] = new TileData()
					{
						pos = playerPos,
						type = type,
						roomId = EnvironmentData.ec.rooms.IndexOf(room)
					};
				}
				beenSaved = false;
			}
			if (Input.GetKeyDown(KeyCode.X)) // Destroy tile
			{
				if (EnvironmentData.ec.tiles[playerPos.x, playerPos.z] != null)
				{
					EnvironmentData.ec.DestroyTile(playerPos, EnvironmentData.ec.tiles[playerPos.x, playerPos.z]);
					tilesToSave.RemoveAll(x => x.pos == playerPos);
				}
				beenSaved = false;
			}
			if (Input.GetKeyDown(KeyCode.J)) // Saves data
			{
				beenSaved = true;
				var sceneObject = Singleton<CoreGameManager>.Instance.sceneObject;
				if (sceneObject.levelAsset != null)
					MapDataHolder.UpdateDataIntoAsset(ref sceneObject.levelAsset, sceneObject.levelAsset.levelSize, tilesToSave.ToArray(), true);
				else if (sceneObject.levelContainer != null)
					MapDataHolder.UpdateDataIntoAsset(ref sceneObject.levelContainer, sceneObject.levelContainer.levelSize, tilesToSave.ToArray(), true);
			}
		}

		static TextMeshProUGUI text = null;

		public static RoomController room = null;

		static bool beenSaved = false;

		static int roomIdx = 0;

		public static List<TileData> tilesToSave = new List<TileData>();

		readonly static char[] data = new char[4] {'0', '0', '0', '0'};

		readonly static Vector3 pos = Vector3.up * 175f;



		// Here is the storage of references, it'll basically create the necessary stuff such as room datas for example and store them in lists, so I can access through UE

		readonly static List<RoomData> roomDatas = new List<RoomData>();

		readonly static List<LightSourceData> lightDatas = new List<LightSourceData>();
	}

	[HarmonyPatch(typeof(ClassicLoadScreen), "OnEnable")] // TO-DO: Remove this aswell when data loads through initialization
	public class ClassicLoadingScreenPatch
	{
		private static void Prefix(ClassicLoadScreen __instance)
		{
			i = __instance;
			var text =__instance.transform.Find("Text").GetChild(0).GetComponent<TextMeshProUGUI>();
			text.GetComponent<RectTransform>().offsetMax = new Vector2(758f, 64f); // Increases the box to put the rest of the text
			text.GetComponent<RectTransform>().offsetMin = new Vector2(-758f, -64f);
			text.text += "\n Loading Map Data\nMight take a while";
		}

		public static ClassicLoadScreen i = null;
	}

	//  -------------- Mod Initialization (Still including map stuff aswell) ------------
	[HarmonyPatch(typeof(MainMenu), "Start")]
	internal class MainMenuPatch
	{
		private static void Prefix()
		{
			if (initialized) return;
			initialized = true;

			MapDataHolder.ConvertAllAssetsInGameToFiles(); // This normally shouldn't work, but if some genius decide to mess up with the files, it'll create the ones based on the default map
		}

		static bool initialized = false;
	}

	[HarmonyPatch(typeof(BaseGameManager), "ApplyMap")]
	internal class ThereWasNoErrorStfu
	{
		private static Exception Finalizer() // Shut the apply map, to do your job >:(
		{				// Why is this even called, the map isn't enabled on this game, omg
			return null;
		}
	}

	// ------------- Gameplay Patches ---------------
	[HarmonyPatch(typeof(ClassicSwingDoor), "OnTriggerEnter")]
	internal class YouNeedTwoNotebooksToBeAbleToOpenTheseDoors // Just let it spam, it's funny lol
	{
		private static bool Prefix(Collider other, SwingDoor ___door, SoundObject ___audYouNeed)
		{
			if (other.tag == "Player" && Singleton<BaseGameManager>.Instance.FoundNotebooks < 2 && !Singleton<CoreGameManager>.Instance.freeRun)
			{
				___door.audMan.PlaySingle(___audYouNeed);
			}
			return false;
		}
	}
}
