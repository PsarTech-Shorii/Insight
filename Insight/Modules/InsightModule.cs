using System;
using System.Collections.Generic;
using UnityEngine;

namespace Insight {
	public delegate void ReceiveMessageEvent(InsightMessageBase message);
	public delegate void ReceiveResponseEvent(InsightMessageBase callbackMsg, CallbackStatus status);
	
	public abstract class InsightModule : MonoBehaviour {
		private static Dictionary<Type, GameObject> _instances;

		private readonly List<Type> _dependencies = new List<Type>();
		private readonly List<Type> _optionalDependencies = new List<Type>();
		
		public event ReceiveMessageEvent OnReceiveMessage;
		public event ReceiveResponseEvent OnReceiveResponse;

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

		protected void ReceiveMessage(InsightMessageBase message) {
			OnReceiveMessage?.Invoke(message);
		}

		protected void ReceiveResponse(InsightMessageBase callbackMsg, CallbackStatus status) {
			OnReceiveResponse?.Invoke(callbackMsg, status);
		}
	}
}