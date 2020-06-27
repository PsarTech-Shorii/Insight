using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Common {
	public class SceneInitializer : MonoBehaviour {
		[SerializeField] private List<string> scenes;
		[SerializeField] private bool initialize;

		private void Start() {
			var isInitialScene = true;
			foreach (var scene in scenes.Select(e => Settings.ScenesRoot + e)) {
				if (isInitialScene) {
					SceneManager.LoadScene(scene, initialize ? LoadSceneMode.Single : LoadSceneMode.Additive);
					isInitialScene = false;
				}
				else {
					SceneManager.LoadScene(scene, LoadSceneMode.Additive);
				}
			}
		}
	}
}