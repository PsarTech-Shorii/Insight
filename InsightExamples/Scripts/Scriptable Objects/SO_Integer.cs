using UnityEngine;
using UnityEngine.Events;

namespace Scriptable_Objects {
	[CreateAssetMenu(menuName = "Primitive/Integer")]
	public class SO_Integer : ScriptableObject {
		[SerializeField] private int data;
		public UnityEvent hasChanged;

		public int Data {
			get => data;

			set {
				data = value;
				hasChanged?.Invoke();
			}
		}
	}
}