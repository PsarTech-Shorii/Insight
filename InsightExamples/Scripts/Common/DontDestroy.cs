using UnityEngine;

namespace Common {
	public class DontDestroy : MonoBehaviour {
		private void Start() {
			DontDestroyOnLoad(this);
		}
	}
}