using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace BBCRAdds.Extensions
{
	public static class Extensions
	{
		/// <summary>
		/// Compare both values like an Equal sign
		/// </summary>
		/// <param name="f"></param>
		/// <param name="f2"></param>
		/// <returns>True if both values are "equal" and false if they aren't</returns>
		public static bool Compare(this float f, float f2) => Math.Abs(f - f2) <= Mathf.Epsilon;
		/// <summary>
		/// Compare both values like an Equal sign
		/// </summary>
		/// <param name="f"></param>
		/// <param name="f2"></param>
		/// <returns>True if both values are "equal" and false if they aren't</returns>
		public static bool Compare(this double f, double f2) => Math.Abs(f - f2) <= Mathf.Epsilon;




		/// <summary>
		/// Replaces an item at <paramref name="index"/> of a List
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static void Replace<T>(this IList<T> values, int index, T value)
		{
			if (index < 0 || index >= values.Count || values.Count == 0)
				throw new ArgumentOutOfRangeException($"The index: {index} is out of the list range (Length: {values.Count})");

			values.RemoveAt(index);
			values.Insert(index, value);
		}
		/// <summary>
		/// Does specific action using <paramref name="func"/> set
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="func"></param>
		/// <returns></returns>
		public static IEnumerable<T> DoAndReturn<T>(this IEnumerable<T> values, Func<T, T> func)
		{
			using (var enumerator = values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					yield return func(enumerator.Current);
				}
			}
		}
		/// <summary>
		/// Extension to remove an item from a collection based on the <paramref name="val"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="val"></param>
		/// <returns>A collection without the item provided</returns>
		/// <exception cref="NullReferenceException"></exception>

		public static IEnumerable<T> RemoveIn<T>(this IEnumerable<T> values, T val)
		{
			return values.Where(x => !ReferenceEquals(x, val) && !Equals(val, x));
		}
		/// <summary>
		/// Extension to remove an item at <paramref name="index"/> from a collection
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="index"></param>
		/// <returns>A collection without the item provided</returns>
		/// <exception cref="NullReferenceException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static IEnumerable<T> RemoveInAt<T>(this IEnumerable<T> values, int index)
		{
			int numeration = 0;
			using (var enumerator = values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (numeration++ != index)
						yield return enumerator.Current;
				}
			}
		}
		/// <summary>
		/// Extension to find the index of an element inside the collection
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="val"></param>
		/// <returns>The index of the element or -1 if it hasn't been found</returns>
		public static int IndexAt<T>(this IEnumerable<T> values, T val)
		{
			int index = 0;
			using (IEnumerator<T> enumerator = values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (ReferenceEquals(enumerator.Current, val) || Equals(val, enumerator.Current))
						return index;

					index++;
				}
			}
			return -1;
		}

		/// <summary>
		/// Extension to find the index of the last element inside the collection
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="val"></param>
		/// <returns>The index of the element or -1 if it hasn't been found</returns>
		public static int LastIndexAt<T>(this IEnumerable<T> values, T val)
		{
			int curIndex = -1;
			int index = 0;
			using (IEnumerator<T> enumerator = values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (ReferenceEquals(enumerator.Current, val) || Equals(val, enumerator.Current))
						curIndex = index;

					index++;
				}
			}
			return curIndex;
		}

		/// <summary>
		/// Extension to find the index of an element inside the collection based on the passed conditions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="val"></param>
		/// <returns>The index of the element or -1 if it hasn't been found</returns>
		public static int IndexAt<T>(this IEnumerable<T> values, Predicate<T> func)
		{
			int index = 0;
			using (IEnumerator<T> enumerator = values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (func(enumerator.Current))
						return index;

					index++;
				}
			}
			return -1;
		}

		/// <summary>
		/// Extension to find the index of the last element inside the collection based on the passed conditions
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="values"></param>
		/// <param name="val"></param>
		/// <returns>The index of the element or -1 if it hasn't been found</returns>
		public static int LastIndexAt<T>(this IEnumerable<T> values, Predicate<T> func)
		{
			int curIndex = -1;
			int index = 0;
			using (IEnumerator<T> enumerator = values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (func(enumerator.Current))
						curIndex = index;

					index++;
				}
			}
			return curIndex;
		}
		/// <summary>
		/// Extrension to get a direction from a string (<paramref name="dir"/>)
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		public static Direction GetDirFromString(this string dir)
		{
			switch (dir.ToLower())
			{
				case "north":
					return Direction.North;
				case "east":
					return Direction.East;
				case "west":
					return Direction.West;
				case "south":
					return Direction.South;
				default:
					return Direction.Null;

			}
		}
		/// <summary>
		/// Converts a string array into a Vector3, must be an array of length 3, each item represents an axis of it
		/// </summary>
		/// <param name="nums"></param>
		/// <returns></returns>
		public static Vector3 ToVector3(this string[] nums)
		{
			if (nums.Length != 3) return default;
			Vector3 vec = default;

			if (float.TryParse(nums[0], out float res))
				vec.x = res;
			if (float.TryParse(nums[1], out res))
				vec.y = res;
			if (float.TryParse(nums[2], out res))
				vec.z = res;
			return vec;
		}

		/// <summary>
		/// Converts a string array into a Vector2, must be an array of length 2, each item represents an axis of it
		/// </summary>
		/// <param name="nums"></param>
		/// <returns></returns>
		public static Vector2 ToVector2(this string[] nums)
		{
			if (nums.Length != 2) return default;
			Vector2 vec = default;

			if (float.TryParse(nums[0], out float res))
				vec.x = res;
			if (float.TryParse(nums[1], out res))
				vec.y = res;

			return vec;
		}

		/// <summary>
		/// Converts a string array into a IntVector2, must be an array of length 2, each item represents an axis of it
		/// </summary>
		/// <param name="nums"></param>
		/// <returns></returns>
		public static IntVector2 ToIntVector2(this string[] nums)
		{
			if (nums.Length != 2) return default;
			IntVector2 vec = default;

			if (int.TryParse(nums[0], out int res))
				vec.x = res;
			if (int.TryParse(nums[1], out res))
				vec.z = res;

			return vec;
		}

		/// <summary>
		/// Converts a string array into a Quaternion, must be an array of length 4, each item represents an axis of it
		/// </summary>
		/// <param name="nums"></param>
		/// <returns></returns>
		public static Quaternion ToRotation(this string[] nums)
		{
			if (nums.Length != 4) return default;
			Quaternion vec = default;

			if (float.TryParse(nums[0], out float res))
				vec.x = res;
			if (float.TryParse(nums[1], out res))
				vec.y = res;
			if (float.TryParse(nums[2], out res))
				vec.z = res;
			if (float.TryParse(nums[3], out res))
				vec.w = res;

			return vec;
		}

		public static RoomData CopyRoomData(this RoomData tarRoom, bool cloneActivity = false) // This boolean will never be used in code, it's for when I add rooms inside the game, so I don't have to search for the activity aswell
		{
			return new RoomData
			{
				activity = cloneActivity ? tarRoom.activity : null,
				category = tarRoom.category,
				ceilMat = tarRoom.ceilMat,
				ceilTex = tarRoom.ceilTex,
				doorMats = tarRoom.doorMats,
				florMat = tarRoom.florMat,
				florTex = tarRoom.florTex,
				hasActivity = tarRoom.hasActivity,
				itemValue = tarRoom.itemValue,
				name = tarRoom.name,
				offLimits = tarRoom.offLimits,
				roomFunction = tarRoom.roomFunction,
				type = tarRoom.type,
				wallMat = tarRoom.wallMat,
				wallTex = tarRoom.wallTex
			};
		}
	}
}
