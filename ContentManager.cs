using System.Linq;
using UnityEngine;

namespace BBCRAdds.Main
{
	public class ContentManager : MonoBehaviour
	{
		private void Awake()
		{
			i = this;
		}

		public static ContentManager i = null;

		public static string modPath = "";

		public bool DebugMode = false;
	}

	public static class EnvironmentData
	{
		public static EnvironmentController ec;

		/// <summary>
		/// Gets the level asset from the game (a simple method to use Resources), it returns by default the main asset used for the playable maps
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static LevelAsset GetAsset(string name = MainAssetName) => Resources.FindObjectsOfTypeAll<LevelAsset>().First(x => x.name == name); // TO-DO: Remove this, it is useless
		

		public const string MainAssetName = "ClassicRemasteredV2";
	}


}
