﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using IPA.Config.Providers;
using IPA.Utilities;
#if NET3
using Net3_Proxy;
using Path = Net3_Proxy.Path;
using Array = Net3_Proxy.Array;
#endif

namespace IPA.Config
{
    /// <summary>
    /// A class to handle updating ConfigProviders automatically
    /// </summary>
    public class Config
    {
        static Config()
        {
            JsonConfigProvider.RegisterConfig();
        }

        /// <summary>
        /// Specifies that a particular parameter is preferred to be a specific type of <see cref="T:IPA.Config.IConfigProvider" />. If it is not available, also specifies backups. If none are available, the default is used.
        /// </summary>
        [AttributeUsage(AttributeTargets.Parameter)]
        public class PreferAttribute : Attribute
        {
            /// <summary>
            /// The order of preference for the config type. 
            /// </summary>
            /// <value>the list of config extensions in order of preference</value>
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string[] PreferenceOrder { get; private set; }

            /// <inheritdoc />
            /// <summary>
            /// Constructs the attribute with a specific preference list. Each entry is the extension without a '.'
            /// </summary>
            /// <param name="preference">The preferences in order of preference.</param>
            public PreferAttribute(params string[] preference)
            {
                PreferenceOrder = preference;
            }
        }

        /// <summary>
        /// Specifies a preferred config name, instead of using the plugin's name.
        /// </summary>
        [AttributeUsage(AttributeTargets.Parameter)]
        public class NameAttribute : Attribute
        {
            /// <summary>
            /// The name to use for the config.
            /// </summary>
            /// <value>the name to use for the config</value>
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string Name { get; private set; }

            /// <inheritdoc />
            /// <summary>
            /// Constructs the attribute with a specific name.
            /// </summary>
            /// <param name="name">the name to use for the config.</param>
            public NameAttribute(string name)
            {
                Name = name;
            }
        }

        private static readonly Dictionary<string, IConfigProvider> registeredProviders = new Dictionary<string, IConfigProvider>();

        /// <summary>
        /// Registers a <see cref="IConfigProvider"/> to use for configs.
        /// </summary>
        /// <typeparam name="T">the type to register</typeparam>
        public static void Register<T>() where T : IConfigProvider => Register(typeof(T));

        /// <summary>
        /// Registers a <see cref="IConfigProvider"/> to use for configs.
        /// </summary>
        /// <param name="type">the type to register</param>
        public static void Register(Type type)
        {
            var inst = Activator.CreateInstance(type) as IConfigProvider;
            if (inst == null)
                throw new ArgumentException($"Type not an {nameof(IConfigProvider)}");

            if (registeredProviders.ContainsKey(inst.Extension))
                throw new InvalidOperationException($"Extension provider for {inst.Extension} already exists");

            registeredProviders.Add(inst.Extension, inst);
        }

        private static Dictionary<Config, FileInfo> files = new Dictionary<Config, FileInfo>();

        /// <summary>
        /// Gets a <see cref="Config"/> object using the specified list of preferred config types.
        /// </summary>
        /// <param name="configName">the name of the mod for this config</param>
        /// <param name="extensions">the preferred config types to try to get</param>
        /// <returns>a <see cref="Config"/> using the requested format, or of type JSON.</returns>
        public static Config GetConfigFor(string configName, params string[] extensions)
        {
            var chosenExt = extensions.FirstOrDefault(s => registeredProviders.ContainsKey(s)) ?? "json";
            var provider = registeredProviders[chosenExt];

            var config = new Config(configName, provider);

            var filename = Path.Combine(BeatSaber.UserDataPath, configName + "." + provider.Extension);
            files.Add(config, new FileInfo(filename));

            RegisterConfigObject(config);

            return config;
        }
        
        internal static Config GetProviderFor(string modName, ParameterInfo info)
        {
            var prefs = Array.Empty<string>();
            if (info.GetCustomAttribute<PreferAttribute>() is PreferAttribute prefer)
                prefs = prefer.PreferenceOrder;
            if (info.GetCustomAttribute<NameAttribute>() is NameAttribute name)
                modName = name.Name;

            return GetConfigFor(modName, prefs);
        }

        private static void RegisterConfigObject(Config obj)
        {
            // TODO: implement
        }

        /// <summary>
        /// Gets the name associated with this <see cref="Config"/> object.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Gets the <see cref="IConfigProvider"/> associated with this <see cref="Config"/> object.
        /// </summary>
        public IConfigProvider Provider { get; private set; }

        internal readonly HashSet<IConfigStore> Stores = new HashSet<IConfigStore>();

        /// <summary>
        /// Adds an <see cref="IConfigStore"/> to this <see cref="Config"/> object.
        /// </summary>
        /// <param name="store">the <see cref="IConfigStore"/> to add to this instance</param>
        /// <returns><see langword="true"/> if the <see cref="IConfigStore"/> was not already registered to this <see cref="Config"/> object,
        /// otherwise <see langword="false"/></returns>
        public bool AddStore(IConfigStore store) => Stores.Add(store);

        private Config(string name, IConfigProvider provider)
        {
            Name = name; Provider = provider;
        }
    }
}
