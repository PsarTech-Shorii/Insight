using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Insight {
	[RequireComponent(typeof(InsightCommon))]
	public class ModuleManager : MonoBehaviour {
		private InsightServer server;
		private InsightClient client;

		private readonly Dictionary<Type, InsightModule> modules = new Dictionary<Type, InsightModule>();
		private readonly HashSet<Type> initializedModules = new HashSet<Type>();

		private void Awake() {
			server = GetComponent<InsightServer>();
			client = GetComponent<InsightClient>();

			if (server) server.enabled = false;
			if (client) client.enabled = false;
		}

		private void Start() {
			AddModules(GetComponentsInChildren<InsightModule>());
			InitializeModules();
			
			if (server) server.enabled = true;
			if (client) client.enabled = true;
		}

		private void AddModule(InsightModule _module) {
			if (modules.ContainsKey(_module.GetType())) {
				throw new Exception($"[ModuleManager] - A module already exists in the server: {_module.GetType()} !");
			}
			
			modules.Add(_module.GetType(), _module);
		}

		private void AddModules(IEnumerable<InsightModule> _modules) {
			foreach (var module in _modules) AddModule(module);
		}

		private void InitializeModule(InsightModule _module) {
			var moduleType = _module.GetType();
			
			// Module is already initialized
			if(initializedModules.Contains(moduleType)) return;
			
			// Not all dependencies have been initialized
			if (!_module.Dependencies.All(_e => initializedModules.Any(_e.IsAssignableFrom))) {
				foreach (var dependencyType in _module.Dependencies) {
					if (!modules.TryGetValue(dependencyType, out var dependency)) {
						throw new Exception($"[ModuleManager] - {moduleType} module must have a {dependencyType} module !");
					}
					InitializeModule(dependency);
				}
			}
			
			// Not all OPTIONAL dependencies have been initialized
			if (!_module.OptionalDependencies.All(_e => initializedModules.Any(_e.IsAssignableFrom))) {
				foreach (var dependencyType in _module.Dependencies) {
					if (modules.TryGetValue(dependencyType, out var dependency)) {
						InitializeModule(dependency);
					}
				}
			}
			
			// Initialize our module
			if (server) {
				_module.Initialize(server, this);
				Debug.Log($"[ModuleManager] - Loaded InsightServer Module: {moduleType}");
			}
			if (client) {
				_module.Initialize(client, this);
				Debug.Log($"[ModuleManager] - Loaded InsightClient Module: {moduleType}");
			}

			initializedModules.Add(moduleType);
		}

		private void InitializeModules() {
			foreach (var module in modules.Values) InitializeModule(module);
		}

		public T GetModule<T>() where T : InsightModule {
			modules.TryGetValue(typeof(T), out var module);

			if (module == null) {
				// Try to find an assignable module
				module = modules.Values.FirstOrDefault(_module => _module is T);
			}

			return module as T;
		}
	}
}