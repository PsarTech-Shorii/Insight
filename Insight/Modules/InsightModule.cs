using System;
using System.Collections.Generic;
using UnityEngine;

namespace Insight {
	public delegate void ReceiveMessageEvent(InsightMessageBase _message);
	public delegate void ReceiveResponseEvent(InsightMessageBase _callbackMsg, CallbackStatus _status);
	
	public abstract class InsightModule : MonoBehaviour {
		private static Dictionary<Type, GameObject> instances;

		private readonly List<Type> dependencies = new List<Type>();
		private readonly List<Type> optionalDependencies = new List<Type>();
		
		public event ReceiveMessageEvent OnReceiveMessage;
		public event ReceiveResponseEvent OnReceiveResponse;

		/// <summary>
		///     Returns a list of module types this module depends on
		/// </summary>
		public IEnumerable<Type> Dependencies => dependencies;
		public IEnumerable<Type> OptionalDependencies => optionalDependencies;

		/// <summary>
		///     Called by master server, when module should be started
		/// </summary>
		public virtual void Initialize(InsightServer _server, ModuleManager _manager) {
			Debug.LogWarning("[Module Manager] Initialize InsightServer not found for module");
		}

		public virtual void Initialize(InsightClient _client, ModuleManager _manager) {
			Debug.LogWarning("[Module Manager] Initialize InsightClient not found for module");
		}

		/// <summary>
		///     Adds a dependency to list. Should be called in Awake or Start methods of
		///     module
		/// </summary>
		/// <typeparam name="T"></typeparam>
		protected void AddDependency<T>() {
			dependencies.Add(typeof(T));
		}

		protected void AddOptionalDependency<T>() {
			optionalDependencies.Add(typeof(T));
		}

		protected void ReceiveMessage(InsightMessageBase _message) {
			OnReceiveMessage?.Invoke(_message);
		}

		protected void ReceiveResponse(InsightMessageBase _callbackMsg, CallbackStatus _status) {
			OnReceiveResponse?.Invoke(_callbackMsg, _status);
		}
	}
}