using BBCRAdds.Extensions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BBCRAdds.Main
{
	public static class MapDataHolder
	{
		public static void UpdateDataIntoAsset(ref LevelAsset asset, IntVector2 newSize, TileData[] tileSet, bool updateFileAfter)
		{
			asset.levelSize = newSize;
			asset.tile = tileSet;
			if (updateFileAfter)
			{
				SaveDataToFile(LevelData.ConvertFromAsset(asset), asset.name);
			}
		}

		public static void UpdateDataIntoAsset(ref LevelDataContainer asset, IntVector2 newSize, TileData[] tileSet, bool updateFileAfter)
		{
			asset.levelSize = newSize;
			asset.tile = tileSet;
			if (updateFileAfter)
			{
				SaveDataToFile(LevelData.ConvertFromContainer(asset), asset.name, isContainer: true);
			}
		}

		public static void ConvertAllAssetsInGameToFiles()
		{
			foreach (var asset in Resources.FindObjectsOfTypeAll<LevelAsset>())
			{
				SaveDataToFile(LevelData.ConvertFromAsset(asset), asset.name, false);
			}

			foreach (var data in Resources.FindObjectsOfTypeAll<LevelDataContainer>())
			{
				SaveDataToFile(LevelData.ConvertFromContainer(data), data.name, false, true);
			}
		}

		static T FilterOutWhichOne<T>(T[] foundings, string targetName)
		{
			if (foundings.Length == 0) return default; // if array empty, just return default

			if (typeof(T) == typeof(Transform)) // If it is selecting a transform, to not collide with other types
			{
				switch (targetName)
				{
					case "Bus":
						return foundings[1];

					default:
						return foundings[0];
				}
			}

			else
			{
				return foundings[0];
			}
		}

		static void ReadDataForAsset(string file, ref LevelData asset) // Yes, I made this an IEnumerator to not lag out the game when loading the file, since people can confunde it with a crash
		{

			//Some clear up before reading data so it doesn't mess up
			asset.tile = new TileData[0];

			Debug.Log("Reading: " + Path.GetFileName(file));

			// throw new ArgumentException("HA GOTCHA, this is to pause in load screen for stuff... there..."); Don't enable this.

			using (StreamReader rd = new StreamReader(file))
			{
				while (!rd.EndOfStream)
				{
					string data = rd.ReadLine();

					
					var sizes = new string[0];

					switch (data)
					{
						case defaultPrefix + mapSizeTag: // yeah, it doesn't accept interpolation in switches for this C# version

							data = rd.ReadLine(); // Reads next line which supposedely must have the map sizes
							sizes = data.Split(',');

							asset.levelSize = sizes.ToIntVector2();

							break;

						case defaultPrefix + tilesTag:
							data = rd.ReadLine(); // Begins with a readline to skip the first part

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // While it doesn't end, read the tiles and put inside the levelAsset lol
							{
								TileData tData = new TileData();

								var tile = data.Split(';');
								sizes = tile[0].Split(','); // Sizes

								tData.pos = sizes.ToIntVector2(); // Not exactly a size, but still works out

								tData.type = int.Parse(tile[1]);
								tData.roomId = int.Parse(tile[2]);

								asset.tile = asset.tile.AddToArray(tData);

								data = rd.ReadLine(); // Reads only afterwards so the check for sufix works

							}

							break;

						case defaultPrefix + roomsTag:

							int lidx = 0;
							data = rd.ReadLine(); // Starts skipping the prefix

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream)
							{

								RoomData room;

								bool isClone = lidx >= asset.rooms.Count;

								Debug.Log("Current Room Being Held: " + data);

								if (isClone) // Checks if the room is a clone or not
								{
									var tarRoom = asset.rooms.Find(x => x.name.Contains(data));
									// Setup the room, since assigning directly would just refer by reference
									room = tarRoom.CopyRoomData();
								}
								else
								{
									room = asset.rooms[lidx];
								}

								data = rd.ReadLine(); // Activity Data

								if (data == "null")
								{
									room.activity = null; // If no activity, it's just a null
								}
								else
								{ // prefab name, vector3, direction
									var acData = data.Split(';');

									room.activity = new ActivityData()
									{
										prefab = FilterOutWhichOne(Resources.FindObjectsOfTypeAll<Activity>().Where(x => x.name == acData[0]).ToArray(), acData[0]),
										position = acData[1].Split(':').ToVector3(),
										direction = acData[2].GetDirFromString()
									};
								}

								int idx = 0;
								string[] pos;

								data = rd.ReadLine(); // Object looping begins here

								while (!data.StartsWith(defaultSecondSufix) && !rd.EndOfStream) // Object Loading
								{
									var objData = data.Split(';');

									pos = objData[1].Split(':');
									string[] rot = objData[2].Split(':');

									if (idx >= room.basicObjects.Count)
									{
										room.basicObjects.Add(new BasicObjectData()
										{
											prefab = FilterOutWhichOne(Resources.FindObjectsOfTypeAll<Transform>().Where(x => x.name == objData[0]).ToArray(), objData[0]),
											position = pos.ToVector3(),
											rotation = rot.ToRotation()
										});
										idx++;
										goto obj_endLine;
									}


									var obj = room.basicObjects[idx];
									obj.prefab = FilterOutWhichOne(Resources.FindObjectsOfTypeAll<Transform>().Where(x => x.name == objData[0]).ToArray(), objData[0]);
									try
									{
										pos = objData[1].Split(':');
										obj.position = pos.ToVector3(); // Yeah it is quite unsafe, but there's no way it wouldn't find these values
										pos = objData[2].Split(':');
										obj.rotation = pos.ToRotation();
									}
									catch
									{
										obj.position = default;
										obj.rotation = default;
									}
									idx++;

								obj_endLine:
									data = rd.ReadLine();
								}

								data = rd.ReadLine();

								idx = 0;


								while (!data.StartsWith(defaultSecondSufix) && !rd.EndOfStream) // Item Loading
								{
									var objData = data.Split(';');

									var itmObj = FilterOutWhichOne(Resources.FindObjectsOfTypeAll<ItemObject>().Where(x => x.name == objData[0]).ToArray(), objData[0]);

									if (itmObj == default) // If true, it is a custom item and should be hidden in DontDestroyAtLoad
									{
										itmObj = FilterOutWhichOne(UnityEngine.Object.FindObjectsOfType<ItemObject>(true).Where(x => x.name == objData[0]).ToArray(), objData[0]);
										if (itmObj == default) // Another safe measure just in case
											goto obj_endLine2;
									}

									pos = objData[1].Split(':');

									if (idx >= room.items.Count)
									{
										room.items.Add(new ItemData()
										{
											item = itmObj,
											position = pos.ToVector2() // Parse again
										});

										idx++;
										goto obj_endLine2;
									}

									var item = room.items[idx];

									item.item = itmObj;
									pos = objData[1].Split(':');
									item.position = pos.ToVector2(); // Parse again

									idx++;

								obj_endLine2:

									data = rd.ReadLine();

								}

								if (isClone)
									asset.rooms.Add(room); // If it is clone, add to add later after this ForEach (to not mess up duh)

								lidx++;
								data = rd.ReadLine(); // Basically just name

							} // End of the main loop (before I get lost)

							break;
						case defaultPrefix + doorsTag:
							data = rd.ReadLine();
							int i = 0;


							while (!data.StartsWith(defaultSecondSufix) && !rd.EndOfStream)
							{
								var doorData = data.Split(';');
								sizes = doorData[1].Split(','); // Position, not size btw

								if (i >= asset.doors.Count)
								{
									asset.doors.Add(new DoorData(int.Parse(doorData[3]),
										FilterOutWhichOne(Resources.FindObjectsOfTypeAll<Door>().Where(x => x.name == doorData[0]).ToArray(), doorData[0]),
										sizes.ToIntVector2(), doorData[2].GetDirFromString()));
									goto door_endLine;
								}

								var door = asset.doors[i];
								door.doorPre = FilterOutWhichOne(Resources.FindObjectsOfTypeAll<Door>().Where(x => x.name == doorData[0]).ToArray(), doorData[0]);
								door.position = sizes.ToIntVector2();
								door.dir = doorData[2].GetDirFromString();
								door.roomId = int.Parse(doorData[3]);

							door_endLine:
								data = rd.ReadLine();
								i++;

							}

							break;
						default: break;

					}

				}
			}

		}

		public static void ReadDataFromFolder(string targetAsset)
		{

			CreateDefaultFolderIfNotExistent();
			foreach (var file in Directory.GetFiles(FolderPath, $"*{defaultFileType}"))
			{
				string fileName = Path.GetFileNameWithoutExtension(file);

				if (fileName != targetAsset) // Looks for specific asset
					continue;

				if (fileName.EndsWith(levelAssetTag)) // If it is a level asset
				{
					LevelAsset asset;
					string name = Path.GetFileNameWithoutExtension(file).Replace(levelAssetTag, "");
					try
					{
						asset = Resources.FindObjectsOfTypeAll<LevelAsset>().First(x => x.name == name);
					}
					catch (Exception e)
					{
						Debug.LogError(e.Message);
						Debug.Log("The file name doesn\'t match with any level asset available in the game");
						return;
					}
					var dat = LevelData.ConvertFromAsset(asset); // Same pratice here, explained in level container
					ReadDataForAsset(file, ref dat);
					dat.ConvertToAsset(asset);
				}
				else if (fileName.EndsWith(levelContainerTag)) // if it is a level container
				{
					LevelDataContainer asset;
					string name = Path.GetFileNameWithoutExtension(file).Replace(levelContainerTag, "");
					try
					{
						asset = Resources.FindObjectsOfTypeAll<LevelDataContainer>().First(x => x.name == name);
					}
					catch (Exception e)
					{
						Debug.LogError(e.Message);
						Debug.Log("The file name doesn\'t match with any level asset available in the game");
						return;
					}
					var dat = LevelData.ConvertFromContainer(asset); // Converts the container into an asset using the LevelData
					ReadDataForAsset(file, ref dat);
					dat.ConvertToContainer(asset, null); // Convert the asset back using the Level Data into the container
				}


			}
		}


		// =============== Level Asset Saving ================
		/// <summary>
		/// Saves the data into a file inside the default folder
		/// </summary>
		public static void SaveDataToFile(LevelData data, string name, bool replaceIfExistent = true, bool isContainer = false)
		{
			CreateDefaultFolderIfNotExistent();

			string path = Path.Combine(FolderPath, $"{name}{(!isContainer ? levelAssetTag : levelContainerTag)}{defaultFileType}");
			if (File.Exists(path))
			{
				if (!replaceIfExistent) // Simple way to not reset every time the game is opened
					return;

				File.Delete(path); // Deletes the file to be re-created again
			}
			Debug.Log(Path.GetFileName(path));
			using (StreamWriter writer = new StreamWriter(path))
			{
				writer.WriteLine("You can play with the data here, but this will mostly just break the game, so do not :)");

				writer.WriteLine($"{defaultPrefix}{mapSizeTag}");
				writer.WriteLine($"{data.levelSize.x},{data.levelSize.z}");

				writer.WriteLine(defaultSufix); // Spacing between them

				writer.WriteLine($"{defaultPrefix}{tilesTag}");
				Debug.Log("saving tiles");
				foreach (var tile in data.tile)
				{
					writer.WriteLine($"{tile.pos.x},{tile.pos.z};{tile.type};{tile.roomId}"); // By default, the order should be: POS, TYPE, ROOMID
				}

				writer.WriteLine(defaultSufix); // Spacing betweem them (again lol)

				writer.WriteLine($"{defaultPrefix}{roomsTag}");
				Debug.Log("Saving rooms");
				foreach (var room in data.rooms) // Room Save. It can't actually remove rooms, instead this saving will only apply new rooms (not custom, just clones literally) and modify specifically items and objects in existing ones
												 // this foreach loop will go in order of each room, so important note: DON'T TOUCH THIS
				{
					writer.WriteLine($"{room.name}"); // first boolean tells if the room is a clone or not (so the loader duplicates it during generation)
						writer.WriteLine(room.activity == null || room.activity.prefab == null ? "null" :  // Apparently activity can't be null, but the prefab can
							$"{room.activity.prefab.name};{room.activity.position.x}:{room.activity.position.y}:{room.activity.position.z};{room.activity.direction}");

					foreach (var obj in room.basicObjects)
					{
						if (obj.prefab != null)
							writer.WriteLine($"{obj.prefab.name};{obj.position.x}:{obj.position.y}:{obj.position.z};{obj.rotation.x}:{obj.rotation.y}:{obj.rotation.z}:{obj.rotation.w}"); // Using ":" for decimal support
					}
					writer.WriteLine(defaultSecondSufix); // markup of a second sufix to not conflict with the first
					foreach (var item in room.items)
					{
						if (item.item != null)
							writer.WriteLine($"{item.item.name};{item.position.x}:{item.position.y}"); // Uses Instance ID to maintain consistency (using names is unstable and caused some glitches)
					}
					writer.WriteLine(defaultSecondSufix);
				}

				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{doorsTag}");
				Debug.Log("Saving Doors");
				foreach (var door in data.doors) // Saving Doors
				{
					writer.WriteLine($"{door.doorPre.name};{door.position.x},{door.position.z};{door.dir};{door.roomId}");
				}

				writer.WriteLine(defaultSufix); // Spacing

			}
		}

		// =============== Level Container Saving ===============

		/// <summary>
		/// Saves the data into a file inside the default folder
		/// </summary>
		public static void SaveDataToFile(LevelDataContainer data, bool replaceIfExistent = true) => SaveDataToFile(LevelData.ConvertFromContainer(data), data.name,replaceIfExistent, true); // Saves the asset data but assigned as a data container to not mess up
		

		private static void CreateDefaultFolderIfNotExistent()
		{
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);
		}

		public static string FolderPath => Path.Combine(ContentManager.modPath, "mapData");
		const string defaultFileType = ".mapDat", defaultPrefix = "//>>", defaultSufix = "//<<", defaultSecondSufix = "//<><";
		const string mapSizeTag = "MapSizes", tilesTag = "tiles", roomsTag = "roomsData", doorsTag = "doorsData";
		const string roomCloneTag = "clone_";

		public const string levelAssetTag = "_LevelAsset", levelContainerTag = "_LevelDataContainer";
	}
}
