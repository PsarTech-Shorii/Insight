using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Insight {
	public class ReceivedMessageEvent : UnityEvent<InsightMessageBase> {}
	public class RepliedMessageEvent : UnityEvent<InsightMessageBase, CallbackStatus> {}

	public abstract class InsightModule : MonoBehaviour {
		private static Dictionary<Type, GameObject> _instances;

		private readonly List<Type> _dependencies = new List<Type>();
		private readonly List<Type> _optionalDependencies = new List<Type>();

		public ReceivedMessageEvent onReceive = new ReceivedMessageEvent();
		public RepliedMessageEvent onResponse = new RepliedMessageEvent();

		/// <summary>
		///     Returns a list of module types this module depends on
		/// </summary>
		public IEnumerable<Type> Dependencies => _dependencies;
		public IEnumerable<Type> OptionalDependencies => _optionalDependencies;

		/// <summary>
		///     Called by master server, when module should be started
		/// </summary>
		public virtual void Initialize(InsightServer server, ModuleManager manager) {
			Debug.LogWarning("[Module Manager] Initialize InsightServer not found for module");
		}

		public virtual void Initialize(InsightClient client, ModuleManager manager) {
			Debug.LogWarning("[Module Manager] Initialize InsightClient not found for module");
		}

		/// <summary>
		///     Adds a dependency to list. Should be called in Awake or Start methods of
		///     module
		/// </summary>
		/// <typeparam name="T"></typeparam>
		protected void AddDependency<T>() {
			_dependencies.Add(typeof(T));
		}

		protected void AddOptionalDependency<T>() {
			_optionalDependencies.Add(typeof(T));
		}
	}
}