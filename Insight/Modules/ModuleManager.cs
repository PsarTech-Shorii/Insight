using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Insight {
	[RequireComponent(typeof(InsightCommon))]
	public class ModuleManager : MonoBehaviour {
		private InsightServer _server;
		private InsightClient _client;

		private readonly Dictionary<Type, InsightModule> _modules = new Dictionary<Type, InsightModule>();
		private readonly HashSet<Type> _initializedModules = new HashSet<Type>();
		
		private bool _initializeComplete;
		private bool _tempAutoStartValue;

		private void Awake() {
			_server = GetComponent<InsightServer>();
			_client = GetComponent<InsightClient>();

			if (_server) {
				_tempAutoStartValue = _server.autoStart;
				_server.autoStart = false; //Wait until modules are loaded to AutoStart
			}
			if (_client) {
				_tempAutoStartValue = _client.autoStart;
				_client.autoStart = false; //Wait until modules are loaded to AutoStart
			}
			
			/*// Disable while waiting for modules to load
			if (_server) {
				Debug.Log("[ModuleManager] - Disable Insight server");
				_server.enabled = false;
			}
			if (_client) {
				Debug.Log("[ModuleManager] - Disable Insight client");
				_client.enabled = false;
			}*/
		}

		private void Update() {
			if (!_initializeComplete) {
				_initializeComplete = true;
				
				// Add modules
				AddModules(GetComponentsInChildren<InsightModule>());

				// Initialize modules
				InitializeModules();

				//Now that modules are loaded check for original AutoStart value
				if (_server && _tempAutoStartValue) {
					_server.autoStart = _tempAutoStartValue;
					_server.StartInsight();
				}

				if (_client && _tempAutoStartValue) {
					_client.autoStart = _tempAutoStartValue;
					_client.StartInsight();
				}
				
				/*// Enable now that the modules are loaded
				if (_server) {
					Debug.Log("[ModuleManager] - Enable Insight server");
					_server.enabled = true;
				}
				if (_client) {
					Debug.Log("[ModuleManager] - Enable Insight client");
					_client.enabled = true;
				}*/
			}
		}

		private void AddModule(InsightModule module) {
			if (_modules.ContainsKey(module.GetType())) {
				throw new Exception($"[ModuleManager] - A module already exists in the server: {module.GetType()} !");
			}
			
			_modules.Add(module.GetType(), module);
		}

		private void AddModules(IEnumerable<InsightModule> modules) {
			foreach (var module in modules) AddModule(module);
		}

		private void InitializeModule(InsightModule module) {
			var moduleType = module.GetType();
			
			// Module is already initialized
			if(_initializedModules.Contains(moduleType)) return;
			
			// Not all dependencies have been initialized
			if (!module.Dependencies.All(e => _initializedModules.Any(e.IsAssignableFrom))) {
				foreach (var dependencyType in module.Dependencies) {
					if (!_modules.TryGetValue(dependencyType, out var dependency)) {
						throw new Exception($"[ModuleManager] - {moduleType} module must have a {dependencyType} module !");
					}
					InitializeModule(dependency);
				}
			}
			
			// Not all OPTIONAL dependencies have been initialized
			if (!module.OptionalDependencies.All(e => _initializedModules.Any(e.IsAssignableFrom))) {
				foreach (var dependencyType in module.Dependencies) {
					if (_modules.TryGetValue(dependencyType, out var dependency)) {
						InitializeModule(dependency);
					}
				}
			}
			
			// Initialize our module
			if (_server) {
				module.Initialize(_server, this);
				Debug.Log($"[ModuleManager] - Loaded InsightServer Module: {moduleType}");
			}
			if (_client) {
				module.Initialize(_client, this);
				Debug.Log($"[ModuleManager] - Loaded InsightClient Module: {moduleType}");
			}

			_initializedModules.Add(moduleType);
		}

		private void InitializeModules() {
			foreach (var module in _modules.Values) InitializeModule(module);
		}

		public T GetModule<T>() where T : InsightModule {
			_modules.TryGetValue(typeof(T), out var module);

			if (module == null) {
				// Try to find an assignable module
				module = _modules.Values.FirstOrDefault(m => m is T);
			}

			return module as T;
		}
	}
}