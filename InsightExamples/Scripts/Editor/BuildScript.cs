using Common;
using UnityEditor;

namespace Editor {
	public static class BuildScript {
		private const BuildOptions ClientBuildOptions = BuildOptions.Development;
		private const BuildOptions ServerBuildOptions = BuildOptions.Development | BuildOptions.EnableHeadlessMode;

		[MenuItem("Tools/Build/Build All", false, 0)]
		public static void BuildAllMenu() {
			if (!string.IsNullOrEmpty(Settings.BuildPath)) {
				BuildMasterServer();
				BuildGameServer();
				BuildPlayerClient();
			}
		}

		[MenuItem("Tools/Build/MasterServer", false, 100)]
		public static void BuildMasterServerMenu() {
			if (!string.IsNullOrEmpty(Settings.BuildPath)) {
				BuildMasterServer();
			}
		}

		[MenuItem("Tools/Build/GameServer", false, 101)]
		public static void BuildGameServerMenu() {
			if (!string.IsNullOrEmpty(Settings.BuildPath)) {
				BuildGameServer();
			}
		}

		[MenuItem("Tools/Build/PlayerClient", false, 102)]
		public static void BuildPlayerClientMenu() {
			if (!string.IsNullOrEmpty(Settings.BuildPath)) {
				BuildPlayerClient();
			}
		}

		private static void BuildMasterServer() {
			string[] scenes = {
				Settings.ScenesRootBuilt + "MasterServer.unity"
			};
			PlayerSettings.productName = "MasterServer";
			BuildPipeline.BuildPlayer(scenes, Settings.BuildPath + "MasterServer/MasterServer" + 
			                                  Settings.BuildExtension, GetBuildTarget(), ServerBuildOptions);
		}

		private static void BuildGameServer() {
			string[] gameServerScenes = {
				Settings.ScenesRootBuilt + "GameServer.unity",
				Settings.ScenesRootBuilt + "Game.unity"
			};
			PlayerSettings.productName = "GameServer";
			BuildPipeline.BuildPlayer(gameServerScenes, Settings.BuildPath + "GameServer/GameServer" + 
			                                            Settings.BuildExtension, GetBuildTarget(), ServerBuildOptions);
		}

		private static void BuildPlayerClient() {
			string[] scenes = {
				Settings.ScenesRootBuilt + "PlayerClient.unity",
				Settings.ScenesRootBuilt + "Lobby.unity",
				Settings.ScenesRootBuilt + "Game.unity"
			};
			PlayerSettings.productName = "PlayerClient";
			BuildPipeline.BuildPlayer(scenes, Settings.BuildPath + "PlayerClient/PlayerClient" + 
			                                  Settings.BuildExtension, GetBuildTarget(), ClientBuildOptions);
		}

		private static BuildTarget GetBuildTarget() {
			return EditorUserBuildSettings.activeBuildTarget;
		}
	}
}