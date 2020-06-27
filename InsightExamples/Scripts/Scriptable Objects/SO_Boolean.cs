using UnityEngine;
using UnityEngine.Events;

namespace Scriptable_Objects {
	[CreateAssetMenu(menuName = "Primitive/Boolean")]
	public class SO_Boolean : ScriptableObject {
		[SerializeField] private bool data;
		public UnityEvent hasChanged;

		public bool Data {
			get => data;

			set {
				data = value;
				hasChanged?.Invoke();
			}
		}
	}
}