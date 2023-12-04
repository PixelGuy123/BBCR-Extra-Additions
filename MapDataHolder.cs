using BBCRAdds.Extensions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using MTM101BaldAPI;

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

		public static void ConvertAllAssetsInGameToFiles() // just pick every asset and convert into a file
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

		static T FilterOutWhichOne<T>(string targetName, bool findByObject = false, bool defaultToFindObjectIfRequired = false) where T : UnityEngine.Object // Yes, an IEnumerable, I don't think I have to convert every Where() into an array
		{
			IEnumerable<T> foundings = findByObject ? UnityEngine.Object.FindObjectsOfType<T>(true).Where(x => x.name == targetName) : Resources.FindObjectsOfTypeAll<T>().Where(x => x.name == targetName);


			if (foundings.Count() == 0)
			{
				if (!findByObject & defaultToFindObjectIfRequired)
				{
					return FilterOutWhichOne<T>(targetName, true); // Basically, if not found in resources, switch to Object search
				}
				return default; // if array empty, just return default
			}

			if (typeof(T) == typeof(Transform)) // If it is selecting a transform, to not collide with other types
			{
				switch (targetName)
				{
					case "Bus":
						return foundings.ElementAt(1);

					default:
						return foundings.First();
				}
			}

			else
			{
				return foundings.First();
			}
		}

		//=======================================================
		//=======================================================
		//=======================================================
		//=============Level Loading Process Below===============
		//=======================================================
		//=======================================================
		//=======================================================

		static void ReadDataForAsset(string file, ref LevelData asset) // Actual method that reads the file
		{
			

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
							asset.tile = new TileData[0]; // Clears out

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
										prefab = FilterOutWhichOne<Activity>(acData[0]),
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
											prefab = FilterOutWhichOne<Transform>(objData[0]),
											position = pos.ToVector3(),
											rotation = rot.ToRotation()
										});
										idx++;
										goto obj_endLine;
									}


									var obj = room.basicObjects[idx];
									obj.prefab = FilterOutWhichOne<Transform>(objData[0]);
									try
									{
										pos = objData[1].Split(':');
										obj.position = pos.ToVector3();
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

									var itmObj = FilterOutWhichOne<ItemObject>(objData[0], defaultToFindObjectIfRequired:true);

									if (itmObj == default) // Safe measure just in case
										goto obj_endLine2;
									

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
									asset.rooms.Add(room);

								lidx++;
								data = rd.ReadLine(); // Basically just name

							} // End of the main loop (before I get lost)

							break;
						case defaultPrefix + doorsTag:
							data = rd.ReadLine();

							asset.doors.Clear(); // Clears out doors


							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream)
							{
								var doorData = data.Split(';');

								asset.doors.Add(new DoorData(int.Parse(doorData[3]),
											FilterOutWhichOne<Door>(doorData[0]),
											doorData[1].Split(',').ToIntVector2(), doorData[2].GetDirFromString()));

								data = rd.ReadLine();

							}

							break;

						case defaultPrefix + exitTag:
							asset.exits.Clear(); // Clears out to add new exits

							data = rd.ReadLine(); // Just reads out first line (which is data)


							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // Prefab Name, IntVector2, Direction, Boolean (is spawn)
							{
								
								var exitData = data.Split(';');
								Debug.Log("Reading Exit: " + exitData[0]);

								asset.exits.Add(new ExitData()
								{
									prefab = FilterOutWhichOne<Elevator>(exitData[0]), // Prefab
									position = exitData[1].Split(',').ToIntVector2(), // pos
									direction = exitData[2].GetDirFromString(), // dir
									spawn = bool.Parse(exitData[3]) // is spawn
								});

								data = rd.ReadLine(); // Next line...
							}


							break;

						case defaultPrefix + lightTag:
							asset.lights.Clear(); // Clears out to add new lighting

							data = rd.ReadLine();

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // Prefab Name, IntVector2, Color, Int (Strength)
							{

								var light = data.Split(';');

								asset.lights.Add(new LightSourceData()
								{
									prefab = light[0] != "null" ? FilterOutWhichOne<Transform>(light[0]) : null, // forgot lights can also have null prefabs
									position = light[1].Split(',').ToIntVector2(),
									color = light[2].Split(':').ToColor(),
									strength = int.Parse(light[3])
								});
								

								data = rd.ReadLine(); // Next line...
							}

							break;

						case defaultPrefix + windowTag:

							asset.windows.Clear(); // Clears out to add new lighting

							data = rd.ReadLine();

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // Prefab Name (WindowObject), IntVector2, Direction
							{
								var wData = data.Split(';');
								asset.windows.Add(new WindowData()
								{
									window = FilterOutWhichOne<WindowObject>(wData[0]),
									position = wData[1].Split(',').ToIntVector2(),
									direction = wData[2].GetDirFromString()
								});


								data = rd.ReadLine(); // Next line...
							}

							break;

						case defaultPrefix + tileBasedTag:

							asset.tbos.Clear(); // Clears out to add new lighting

							data = rd.ReadLine();

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // Prefab Name (TileBasedObject), IntVector2, Direction
							{
								var wData = data.Split(';');
								asset.tbos.Add(new TileBasedObjectData
								{
									prefab = FilterOutWhichOne<TileBasedObject>(wData[0]),
									position = wData[1].Split(',').ToIntVector2(),
									direction = wData[2].GetDirFromString()
								});


								data = rd.ReadLine(); // Next line...
							}

							break;
						case defaultPrefix + eventTag:

							asset.events.Clear(); // Clears out to add new lighting

							var eventData = rd.ReadLine().Split(';'); // All data is here basically

							foreach (var ev in eventData) // RandomEventType
							{
								if (Enum.TryParse(ev, out RandomEventType res))
								{
									if (res.TryGetFirstInstance(out var v)) // Try to get the instance of course, we don't want to break this, do we? :)
										asset.events.Add(v);
								}
							}


							break;

						case defaultPrefix + posterTag:

							asset.posters.Clear(); // Clears out

							data = rd.ReadLine();

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // Prefab Name (PosterObject), IntVector2, Direction
							{
								var wData = data.Split(';');
								asset.posters.Add(new PosterData
								{
									poster = FilterOutWhichOne<PosterObject>(wData[0]),
									position = wData[1].Split(',').ToIntVector2(),
									direction = wData[2].GetDirFromString()
								});


								data = rd.ReadLine(); // Next line...
							}

							break;

						case defaultPrefix + buttonTag:

							asset.buttons.Clear(); // Clears out

							data = rd.ReadLine();

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // Prefab Name (GameButton), IntVector2, Direction >> Sublist for receivers: int, int, enum
							{
								var wData = data.Split(';');

								data = rd.ReadLine(); // Go to the next line afterwards

								var receivers = new List<ButtonReceiverData>();
								while (!data.StartsWith(defaultSecondSufix) && !rd.EndOfStream)
								{
									var sData = data.Split(',');
									receivers.Add(new ButtonReceiverData()
									{
										receiverIndex = int.Parse(sData[0]),
										receiverRoom = int.Parse(sData[1]),
										type = (ButtonReceiverType)Enum.Parse(typeof(ButtonReceiverType), sData[2]) // Since there are just 2 enums, it is expected to always work, no need to tryparse.... right?
									});

									data = rd.ReadLine();
								}

								asset.buttons.Add(new ButtonData
								{
									prefab = FilterOutWhichOne<GameButton>(wData[0]),
									position = wData[1].Split(',').ToIntVector2(),
									direction = wData[2].GetDirFromString(),
									receivers = receivers
								});


								data = rd.ReadLine(); // Next line...
							}

							break;

						case defaultPrefix + builderTag:

							asset.builders.Clear(); // Clears out

							data = rd.ReadLine();

							while (!data.StartsWith(defaultSufix) && !rd.EndOfStream) // Prefab Name (ObjectBuilder) >> sublists: List<IntVector2> then List<Direction>
							{
								var wData = data.Split(';');

								data = rd.ReadLine(); // Go to the next line afterwards

								var pos = new List<IntVector2>();
								while (!data.StartsWith(defaultSecondSufix) && !rd.EndOfStream) // Get IntVectors
								{
									pos.Add(data.Split(',').ToIntVector2());
									data = rd.ReadLine();
								}

								data = rd.ReadLine(); // Go to the next line afterwards

								var dir = new List<Direction>();
								while (!data.StartsWith(defaultSecondSufix) && !rd.EndOfStream) // Get directions
								{
									dir.Add(data.GetDirFromString());
									data = rd.ReadLine();
								}

								asset.builders.Add(new ObjectBuilderData()
								{
									builder = FilterOutWhichOne<ObjectBuilder>(wData[0]),
									pos = pos,
									dir = dir
								});


								data = rd.ReadLine(); // Next line...
							}

							break;

						default: break;

					}

				}
			}

		}

		// Get all the files in the folder and load them into the game
		public static void ReadDataFromFolder(string targetAsset) // TO-DO: Makes it load all of the data in the mod initialization just so it doesn't repeat the same process everytime
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

		//=======================================================
		//=======================================================
		//=======================================================
		// =============== Level Asset Saving ================
		//=======================================================
		//=======================================================
		//=======================================================

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
			Debug.Log($"Saving data from: {Path.GetFileName(path)}");
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


				writer.WriteLine($"{defaultPrefix}{exitTag}");
				Debug.Log("Saving Exits");
				foreach (var exit in data.exits) // Saving Exits
				{
					writer.WriteLine($"{exit.prefab.name};{exit.position.x},{exit.position.z};{exit.direction};{exit.spawn}");
				}

				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{lightTag}");
				Debug.Log("Saving Lighting");
				foreach (var light in data.lights) // Saving Lights
				{
					writer.WriteLine($"{(light.prefab != null ? light.prefab.name : "null")};{light.position.x},{light.position.z};{light.color.r}:{light.color.g}:{light.color.b}:{light.color.a};{light.strength}");
				}

				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{windowTag}");
				Debug.Log("Saving Windows");

				foreach (var win in data.windows) // Saving Windows
				{
					writer.WriteLine($"{win.window.name};{win.position.x},{win.position.z};{win.direction}");
				}

				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{tileBasedTag}");
				Debug.Log("Saving Tile-Based Objects");

				foreach (var obj in data.tbos) // Saving Tile-Based Objects
				{
					writer.WriteLine($"{obj.prefab.name};{obj.position.x},{obj.position.z};{obj.direction}");
				}

				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{eventTag}");
				Debug.Log("Saving Events");

				foreach (var ev in data.events) // Saving Random Events
				{
					writer.Write($"{ev.Type};"); // Only write, because only the type is necessary to find the prefab
				}


				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{posterTag}");
				Debug.Log("Saving Posters");

				foreach (var po in data.posters) // Saving Posters
				{
					writer.WriteLine($"{po.poster.name};{po.position.x},{po.position.z};{po.direction}");
				}

				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{buttonTag}");
				Debug.Log("Saving Buttons");

				foreach (var b in data.buttons) // Saving Buttons / Note: Receivers are classes, not scriptable objects, require secondaries sufixes
				{
					writer.WriteLine($"{b.prefab.name};{b.position.x},{b.position.z};{b.direction}"); // Basic Data

					foreach (var br in b.receivers) // Second list storing the data from receivers (each line is one receiver lol)
					{
						writer.WriteLine($"{br.receiverIndex},{br.receiverRoom},{br.type}");
					}
					writer.WriteLine(defaultSecondSufix); // Secondary Spacing
				}

				writer.WriteLine(defaultSufix); // Spacing

				writer.WriteLine($"{defaultPrefix}{builderTag}");
				Debug.Log("Saving Builders");

				foreach (var b in data.builders) // Saving Builders / Note: require 2 sublists, one for IntVector2, and the other for Directions
				{
					writer.WriteLine($"{b.builder.name}"); // Basic Data
					foreach (var i in b.pos)
					{
						writer.WriteLine($"{i.x},{i.z}");
					}
					writer.WriteLine(defaultSecondSufix); // Secondary Spacing
					foreach (var d in b.dir)
					{
						writer.WriteLine(d);
					}
					writer.WriteLine(defaultSecondSufix); // Secondary Spacing
				}

				writer.WriteLine(defaultSufix); // Spacing
			}
		}

		/// <summary>
		/// Saves the data into a file inside the default folder
		/// </summary>
		public static void SaveDataToFile(LevelDataContainer data, bool replaceIfExistent = true) => SaveDataToFile(LevelData.ConvertFromContainer(data), data.name,replaceIfExistent, true); // Saves the asset data but assigned as a data container to not mess up
		

		private static void CreateDefaultFolderIfNotExistent()
		{
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);
		}

		// Yeah
		public static string FolderPath => Path.Combine(ContentManager.modPath, "mapData");

		// Some types in constants for easy management
		const string defaultFileType = ".mapDat", defaultPrefix = "//>>", defaultSufix = "//<<", defaultSecondSufix = "//<><";

		// Tags (to differ each data type)
		const string mapSizeTag = "MapSizes", tilesTag = "tiles", roomsTag = "roomsData", doorsTag = "doorsData", exitTag = "exitData", lightTag = "lightingData", windowTag = "windowData", tileBasedTag = "tileBasedObjectData", eventTag = "eventData", posterTag = "posterData",
			buttonTag = "buttonData", builderTag = "buildersData";

		public const string levelAssetTag = "_LevelAsset", levelContainerTag = "_LevelDataContainer";
	}
}
