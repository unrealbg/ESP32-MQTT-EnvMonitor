namespace ESP32_NF_MQTT_DHT.Modules
{
    using System;
    using System.IO;
    using System.Reflection;

    using ESP32_NF_MQTT_DHT.Helpers;
    using ESP32_NF_MQTT_DHT.Modules.Contracts;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Simple module manager that tracks modules and controls their lifecycle.
    /// Also supports dynamic discovery/loading of modules from external PE assemblies.
    /// Supports two kinds of external modules:
    ///  - Strong modules implementing Contracts.IModule (compiled against this firmware's contracts)
    ///  - Duck-typed modules exposing: string Name {get;}, void Start(), void Stop(),
    ///    and optionally void Init(IServiceProvider sp) for DI access. These are wrapped by an adapter.
    /// </summary>
    public sealed class ModuleManager : IModuleManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly object _lock = new object();
        private IModule[] _modules = new IModule[4];
        private int _count = 0;

        public ModuleManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Register(IModule module)
        {
            if (module == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_count == _modules.Length)
                {
                    var n = _modules.Length * 2;
                    var tmp = new IModule[n];
                    for (int i = 0; i < _modules.Length; i++)
                    {
                        tmp[i] = _modules[i];
                    }

                    _modules = tmp;
                }

                _modules[_count++] = module;
            }
        }

        /// <summary>
        /// Loads .pe assemblies from a directory and registers any IModule implementations found.
        /// Also supports duck-typed modules with Name/Start/Stop methods.
        /// </summary>
        /// <param name="dir">Directory containing module .pe files.</param>
        /// <returns>Number of modules loaded and registered.</returns>
        public int LoadFromDirectory(string dir)
        {
            int loaded = 0;
            try
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    LogHelper.LogInformation("Modules directory missing: " + dir);
                    return 0;
                }

                var files = Directory.GetFiles(dir);
                if (files == null || files.Length == 0)
                {
                    return 0;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    var path = files[i];
                    if (!EndsWith(path, ".pe"))
                    {
                        continue;
                    }

                    try
                    {
                        var pe = File.ReadAllBytes(path);
                        var asm = Assembly.Load(pe);
                        if (asm == null)
                        {
                            LogHelper.LogWarning("Failed to load module assembly: " + path);
                            continue;
                        }

                        var types = asm.GetTypes();
                        for (int t = 0; t < types.Length; t++)
                        {
                            var type = types[t];

                            // Strong contract implementation
                            if (this.ImplementsIModule(type))
                            {
                                var strong = this.TryCreateStrongModule(type);
                                if (strong != null)
                                {
                                    this.Register(strong);
                                    loaded++;
                                    LogHelper.LogInformation("Registered module from OTA: " + strong.Name);
                                    continue;
                                }
                            }

                            // Duck-typed module support
                            if (this.HasModuleShape(type))
                            {
                                var adapter = this.TryCreateDuckModule(type);
                                if (adapter != null)
                                {
                                    this.Register(adapter);
                                    loaded++;
                                    LogHelper.LogInformation("Registered duck-typed module from OTA: " + adapter.Name);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("Module load error for '" + path + "': " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError("LoadFromDirectory error: " + ex.Message);
            }

            return loaded;
        }

        public void StartAll()
        {
            lock (_lock)
            {
                for (int i = 0; i < _count; i++)
                {
                    try
                    {
                        LogHelper.LogInformation("Starting module: " + _modules[i].Name);
                        _modules[i].Start();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("Failed to start module '" + _modules[i].Name + "': " + ex.Message);
                    }
                }
            }
        }

        public void StopAll()
        {
            lock (_lock)
            {
                for (int i = _count - 1; i >= 0; i--)
                {
                    try
                    {
                        LogHelper.LogInformation("Stopping module: " + _modules[i].Name);
                        _modules[i].Stop();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogError("Failed to stop module '" + _modules[i].Name + "': " + ex.Message);
                    }
                }
            }
        }

        private static bool EndsWith(string s, string suffix)
        {
            if (s == null || suffix == null)
            {
                return false;
            }

            int n = s.Length, m = suffix.Length;
            if (m > n)
            {
                return false;
            }

            for (int i = 0; i < m; i++)
            {
                if (s[n - m + i] != suffix[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool ImplementsIModule(Type type)
        {
            try
            {
                if (type == null)
                {
                    return false;
                }

                if (type.IsInterface)
                {
                    return false;
                }

                if (type.IsAbstract)
                {
                    return false;
                }

                var target = typeof(Contracts.IModule);
                var ifaces = type.GetInterfaces();

                if (ifaces == null)
                {
                    return false;
                }

                for (int i = 0; i < ifaces.Length; i++)
                {
                    if (ifaces[i] == target || ifaces[i].FullName == target.FullName)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private IModule TryCreateStrongModule(Type type)
        {
            // Try DI first
            try
            {
                if (_serviceProvider != null)
                {
                    var obj = ActivatorUtilities.CreateInstance(_serviceProvider, type);
                    return obj as IModule;
                }
            }
            catch
            {
                // ignored
            }

            // Fallback to default ctor
            try
            {
                var obj = Activator.CreateInstance(type, null);
                return obj as IModule;
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private bool HasModuleShape(Type type)
        {
            try
            {
                if (type == null)
                {
                    return false;
                }

                if (type.IsInterface)
                {
                    return false;
                }

                if (type.IsAbstract)
                {
                    return false;
                }

                var nameGetter = type.GetMethod("get_Name");
                var start = type.GetMethod("Start");
                var stop = type.GetMethod("Stop");

                if (nameGetter == null || start == null || stop == null) return false;

                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private IModule TryCreateDuckModule(Type type)
        {
            object instance = null;

            // Try DI create
            try
            {
                if (_serviceProvider != null)
                {
                    instance = ActivatorUtilities.CreateInstance(_serviceProvider, type);
                }
            }
            catch
            {
                // ignored
            }

            // Fallback: default ctor
            if (instance == null)
            {
                try
                {
                    instance = Activator.CreateInstance(type, null);
                }
                catch
                {
                    // ignored
                }
            }

            if (instance == null)
            {
                return null;
            }

            // Try optional Init(IServiceProvider) for DI access
            try
            {
                var init = type.GetMethod("Init");
                if (init != null)
                {
                    var pars = init.GetParameters();
                    if (pars != null && pars.Length == 1 && _serviceProvider != null)
                    {
                        // Call even without strict param type checking to keep it flexible
                        init.Invoke(instance, new object[] { _serviceProvider });
                    }
                }
            }
            catch
            {
                // ignored
            }

            return new ReflectionModuleAdapter(instance, type);
        }

        /// <summary>
        /// Adapter that wraps a duck-typed object exposing Name/Start/Stop as an IModule.
        /// </summary>
        private sealed class ReflectionModuleAdapter : IModule
        {
            private readonly object _target;
            private readonly MethodInfo _start;
            private readonly MethodInfo _stop;
            private readonly MethodInfo _nameGetter;

            public ReflectionModuleAdapter(object target, Type type)
            {
                _target = target;
                _start = type.GetMethod("Start");
                _stop = type.GetMethod("Stop");
                _nameGetter = type.GetMethod("get_Name");
            }

            public string Name
            {
                get
                {
                    try
                    {
                        return (string)_nameGetter.Invoke(_target, null);
                    }
                    catch
                    {
                        return "DuckModule";
                    }
                }
            }

            public void Start()
            {
                try
                {
                    _start.Invoke(_target, null);
                }
                catch
                {
                    // ignored
                }
            }

            public void Stop()
            {
                try
                {
                    _stop.Invoke(_target, null);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
