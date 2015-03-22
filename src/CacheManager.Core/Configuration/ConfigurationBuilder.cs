﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CacheManager.Core.Configuration
{
    /// <summary>
    /// Helper class to load cache manager configurations from file or to build new configurations in a fluent way.
    /// <para>
    /// This only loads configurations. To build a cache manager instance, use <c>CacheFactory</c> and pass in the
    /// configuration. Or use the <c>Build</c> methods of <c>CacheFactory</c>!
    /// </para>
    /// </summary>
    /// <see cref="CacheFactory"/>
    public static class ConfigurationBuilder
    {
        private const string ConfigurationSectionName = "cacheManager";
        private const string Hours = "h";
        private const string Minutes = "m";
        private const string Seconds = "s";

        /// <summary>
        /// Builds a <c>CacheManagerConfiguration</c> which can be used to create a new cache manager instance.
        /// <para>
        /// Pass the configuration to <c>CacheFactory.FromConfiguration</c> to create a valid cache manager.
        /// </para>
        /// </summary>
        /// <param name="cacheName">The cache manager's name.</param>
        /// <param name="settings">The configuration settings to define the cache handles and other properties.</param>
        /// <returns>The <c>CacheManagerConfiguration</c>.</returns>
        public static CacheManagerConfiguration<TCacheValue> BuildConfiguration<TCacheValue>(string cacheName, Action<ConfigurationBuilderCachePart<TCacheValue>> settings)
        {
            if (string.IsNullOrWhiteSpace(cacheName))
            {
                throw new ArgumentNullException("cacheName");
            }

            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            var part = new ConfigurationBuilderCachePart<TCacheValue>(cacheName);
            settings(part);
            return part.Configuration;
        }

        /// <summary>
        /// Loads a configuration from web.config or app.config.
        /// <para>
        /// The <paramref name="configName"/> must match with the name attribute of one of the configured cache elements.
        /// </para>
        /// </summary>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <see cref="CacheManagerConfiguration{T}"/>
        public static CacheManagerConfiguration<TCacheValue> LoadConfiguration<TCacheValue>(string configName)
        {
            return LoadConfiguration<TCacheValue>(ConfigurationSectionName, configName);
        }

        /// <summary>
        /// Loads a configuration from web.config or app.config, by section and config name.
        /// <para>
        /// The <paramref name="configName"/> must match with the name attribute of one of the configured cache elements.
        /// </para>
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <see cref="CacheManagerConfiguration{T}"/>
        public static CacheManagerConfiguration<TCacheValue> LoadConfiguration<TCacheValue>(string sectionName, string configName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentNullException("sectionName");
            }

            var section = ConfigurationManager.GetSection(sectionName) as CacheManagerSection;
            if (section == null)
            {
                throw new InvalidOperationException("No section defined with name " + sectionName);
            }
            return LoadFromSection<TCacheValue>(section, configName);
        }

        /// <summary>
        /// Loads a configuration from the given <paramref name="configFileName" />.
        /// <para>
        /// The <paramref name="configName" /> must match with the name attribute of one of the
        /// configured cache elements.</para>
        /// </summary>
        /// <param name="configFileName">
        /// The full path of the file to load the configuration from.
        /// </param>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configFileName" /> or <paramref name="configName" /> are null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the file specified by <paramref name="configFileName" /> does not exist.
        /// </exception>
        /// <see cref="CacheManagerConfiguration{T}"/>
        public static CacheManagerConfiguration<TCacheValue> LoadConfigurationFile<TCacheValue>(string configFileName, string configName)
        {
            return LoadConfigurationFile<TCacheValue>(configFileName, ConfigurationSectionName, configName);
        }

        /// <summary>
        /// Loads a configuration from the given <paramref name="configFileName" /> and <paramref name="sectionName"/>.
        /// <para>
        /// The <paramref name="configName" /> must match with the name attribute of one of the
        /// configured cache elements.</para>
        /// </summary>
        /// <param name="configFileName">
        /// The full path of the file to load the configuration from.
        /// </param>
        /// <param name="sectionName">The name of the configuration section.</param>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configFileName" /> or <paramref name="configName" /> are null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the file specified by <paramref name="configFileName" /> does not exist.
        /// </exception>
        /// <see cref="CacheManagerConfiguration{T}"/>
        public static CacheManagerConfiguration<TCacheValue> LoadConfigurationFile<TCacheValue>(string configFileName, string sectionName, string configName)
        {
            if (string.IsNullOrWhiteSpace(configFileName))
            {
                throw new ArgumentNullException("configFileName");
            }
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentNullException("sectionName");
            }
            if (string.IsNullOrWhiteSpace(configName))
            {
                throw new ArgumentNullException("configName");
            }

            if (!File.Exists(configFileName))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "Configuration file not found [{0}].", configFileName));
            }

            var fileConfig = new ExeConfigurationFileMap();
            fileConfig.ExeConfigFilename = configFileName; // setting exe config file name, this is the one the GetSection method expects.

            // open the file map
            System.Configuration.Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(fileConfig, ConfigurationUserLevel.None);

            // use the opened configuration and load our section
            var section = cfg.GetSection(sectionName) as CacheManagerSection;
            if (section == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "No section with name {1} found in file {0}", configFileName, sectionName));
            }

            return LoadFromSection<TCacheValue>(section, configName);
        }

        // todo: refactor -> high complexity
        internal static CacheManagerConfiguration<TCacheValue> LoadFromSection<TCacheValue>(CacheManagerSection section, string configName)
        {
            if (string.IsNullOrWhiteSpace(configName))
            {
                throw new ArgumentNullException("configName");
            }

            var handleDefsSection = section.CacheHandleDefinitions;

            if (handleDefsSection.Count == 0)
            {
                throw new InvalidOperationException("There are no cache handles defined.");
            }

            // load handle definitions as lookup
            var handleDefs = new SortedList<string, CacheHandleConfiguration<TCacheValue>>();
            foreach (CacheHandleDefinition def in handleDefsSection)
            {
                //// don't validate at this point, otherwise we will get an exception if any defined handle doesn't match with the requested type...
                //// CacheReflectionHelper.ValidateCacheHandleGenericTypeArguments(def.HandleType, cacheValue);

                var normId = def.Id.ToUpper(CultureInfo.InvariantCulture);
                handleDefs.Add(
                    normId, 
                    new CacheHandleConfiguration<TCacheValue>(configName, def.Id)
                    {
                        HandleType = def.HandleType,
                        ExpirationMode = def.DefaultExpirationMode,
                        ExpirationTimeout = GetTimeSpan(def.DefaultTimeout, "defaultTimeout")
                    });
            }

            // retrieve the handles collection with the correct name
            CacheManagerHandleCollection managerCfg = section.CacheManagers.FirstOrDefault(p => p.Name.Equals(configName, StringComparison.OrdinalIgnoreCase));

            if (managerCfg == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "No cache manager configuration found for name [{0}]", configName));
            }

            int? maxRetries = managerCfg.MaximumRetries;
            if (maxRetries.HasValue && maxRetries.Value <= 0)
            {
                throw new InvalidOperationException("Maximum number of retries must be greater than zero.");
            }

            int? retryTimeout = managerCfg.RetryTimeout;
            if (retryTimeout.HasValue && retryTimeout.Value < 0)
            {
                throw new InvalidOperationException("Retry timeout must be greater than or equal to zero.");
            }
            
            // build configuration
            var cfg = new CacheManagerConfiguration<TCacheValue>(configName, maxRetries.HasValue ? maxRetries.Value : int.MaxValue, retryTimeout.HasValue ? retryTimeout.Value : 10);
            cfg.CacheUpdateMode = managerCfg.UpdateMode;

            foreach (CacheManagerHandle handleItem in managerCfg)
            {
                var normRefId = handleItem.RefHandleId.ToUpper(CultureInfo.InvariantCulture);
                if (!handleDefs.ContainsKey(normRefId))
                {
                    throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture, "Referenced cache handle [{0}] cannot be found in cache handles definition.", handleItem.RefHandleId));
                }

                var handleDef = handleDefs[normRefId];

                var handle = new CacheHandleConfiguration<TCacheValue>(configName, handleItem.Name)
                {
                    HandleType = handleDef.HandleType,
                    ExpirationMode = handleDef.ExpirationMode,
                    ExpirationTimeout = handleDef.ExpirationTimeout,
                    EnableStatistics = managerCfg.EnableStatistics,
                    EnablePerformanceCounters = managerCfg.EnablePerformanceCounters
                };

                // override default timeout if it is defined in this section.
                if (ParameterIsModified(handleItem, "timeout"))
                {
                    handle.ExpirationTimeout = GetTimeSpan(handleItem.Timeout, "timeout");
                }

                // override default expiration mode if it is defined in this section.
                if (ParameterIsModified(handleItem, "expirationMode"))
                {
                    handle.ExpirationMode = handleItem.ExpirationMode;
                }

                if (handle.ExpirationMode != ExpirationMode.None && handle.ExpirationTimeout == TimeSpan.Zero)
                {
                    throw new ConfigurationErrorsException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expiration mode set without a valid timeout specified for handle [{0}]",
                            handle.HandleName));
                }

                cfg.CacheHandles.Add(handle);
            }

            if (cfg.CacheHandles.Count == 0)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "There are no valid cache handles linked to the cache manager configuration [{0}]", configName));
            }

            // load redis options
            foreach (var redisOption in section.RedisOptions)
            {
                var endpoints = new List<ServerEndPoint>();
                foreach (var endpoint in redisOption.Endpoints)
                {
                    endpoints.Add(new ServerEndPoint(endpoint.Host, endpoint.Port));
                }

                cfg.RedisConfigurations.Add(
                    new RedisConfiguration(
                        id: redisOption.Id,
                        database: redisOption.Database,
                        endpoints: endpoints,
                        connectionString: redisOption.ConnectionString,
                        password: redisOption.Password,
                        isSsl: redisOption.Ssl,
                        sslHost: redisOption.SslHost,
                        connectionTimeout: redisOption.ConnectionTimeout == 0 ? 5000 : redisOption.ConnectionTimeout,
                        allowAdmin: redisOption.AllowAdmin));
            }

            return cfg;
        }
        //// Parses the timespan setting from configuration.
        //// Cfg value can be suffixed with s|h|m for seconds hours or minutes...
        //// Depending on the suffix we have to construct the returned TimeSpan.
        private static TimeSpan GetTimeSpan(string timespanCfgValue, string propName)
        {
            if (string.IsNullOrWhiteSpace(timespanCfgValue))
            {
                // default value coming from the system.configuration seems to be empty string...
                return TimeSpan.Zero;
            }

            string normValue = timespanCfgValue.ToUpper(CultureInfo.InvariantCulture);

            bool hasSuffix = Regex.IsMatch(normValue, @"\b[0-9]+[S|H|M]\b");

            string suffix = hasSuffix ? new string(normValue.Last(), 1) : string.Empty;

            int timeoutValue = 0;
            if (!int.TryParse(hasSuffix ? normValue.Substring(0, normValue.Length - 1) : normValue, out timeoutValue))
            {
                throw new ConfigurationErrorsException(
                    string.Format(CultureInfo.InvariantCulture, "The value of the property '{1}' cannot be parsed [{0}].", timespanCfgValue, propName));
            }

            // if minutes or no suffix is defined, we use minutes.
            if (!hasSuffix || suffix.Equals(Minutes, StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromMinutes(timeoutValue);
            }

            // hours
            if (suffix.Equals(Hours, StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromHours(timeoutValue);
            }

            // last option would be seconds
            return TimeSpan.FromSeconds(timeoutValue);
        }

        private static bool ParameterIsModified(ConfigurationElement elem, string property)
        {
            var propInfo = elem.ElementInformation.Properties[property];
            if (propInfo == null)
            {
                throw new InvalidOperationException("Looking for a non existing attribute");
            }

            return propInfo.IsModified;
        }
    }

    /// <summary>
    /// Used to build a <c>CacheHandleConfiguration</c>.
    /// </summary>
    /// <see cref="CacheManagerConfiguration{T}"/>
    public sealed class ConfigurationBuilderCacheHandlePart<TCacheValue>
    {
        private ConfigurationBuilderCachePart<TCacheValue> parent;

        internal ConfigurationBuilderCacheHandlePart(CacheHandleConfiguration<TCacheValue> cfg, ConfigurationBuilderCachePart<TCacheValue> parentPart)
        {
            this.Configuration = cfg;
            this.parent = parentPart;
        }

        /// <summary>
        /// Adds another cache configuration. Can be used to add multiple cache handles.
        /// </summary>
        public ConfigurationBuilderCachePart<TCacheValue> And
        {
            get
            {
                return this.parent;
            }
        }

        internal CacheHandleConfiguration<TCacheValue> Configuration { get; private set; }

        /// <summary>
        /// Disables performance counters for this cache handle.
        /// </summary>
        public ConfigurationBuilderCacheHandlePart<TCacheValue> DisablePerformanceCounters()
        {
            this.Configuration.EnablePerformanceCounters = false;
            return this;
        }

        /// <summary>
        /// Disables statistic gathering for this cache handle.
        /// <para>This also disables performance counters as statistics are required for the counters.</para>
        /// </summary>
        public ConfigurationBuilderCacheHandlePart<TCacheValue> DisableStatistics()
        {
            this.Configuration.EnableStatistics = false;
            this.Configuration.EnablePerformanceCounters = false;
            return this;
        }

        /// <summary>
        /// Enables performance counters for this cache handle.
        /// <para>This also enables statistics, as this is required for performance counters.</para>
        /// </summary>
        public ConfigurationBuilderCacheHandlePart<TCacheValue> EnablePerformanceCounters()
        {
            this.Configuration.EnablePerformanceCounters = true;
            this.Configuration.EnableStatistics = true;
            return this;
        }

        /// <summary>
        /// Enables statistic gathering for this cache handle.
        /// <para>The statistics can be accessed via cacheHandle.Stats.GetStatistic.</para>
        /// </summary>
        public ConfigurationBuilderCacheHandlePart<TCacheValue> EnableStatistics()
        {
            this.Configuration.EnableStatistics = true;
            return this;
        }

        /// <summary>
        /// Sets the expiration mode and timeout of the cache handle.
        /// </summary>
        /// <param name="expirationMode">The expiration mode.</param>
        /// <param name="timeout">The timeout.</param>
        /// <exception cref="InvalidOperationException">Thrown if expiration mode is not 'None' and timeout is zero.</exception>
        /// <seealso cref="ExpirationMode"/>
        public ConfigurationBuilderCacheHandlePart<TCacheValue> WithExpiration(ExpirationMode expirationMode, TimeSpan timeout)
        {
            if (expirationMode != ExpirationMode.None && timeout == TimeSpan.Zero)
            {
                throw new InvalidOperationException("If expiration mode is not set to 'None', timeout cannot be zero.");
            }

            this.Configuration.ExpirationMode = expirationMode;
            this.Configuration.ExpirationTimeout = timeout;
            return this;
        }
    }

    /// <summary>
    /// Used to build a <c>CacheManagerConfiguration</c>.
    /// </summary>
    /// <see cref="CacheManagerConfiguration{T}"/>
    public sealed class ConfigurationBuilderCachePart<TCacheValue>
    {
        internal ConfigurationBuilderCachePart(string cacheName)
        {
            this.Configuration = new CacheManagerConfiguration<TCacheValue>(cacheName);
        }

        internal CacheManagerConfiguration<TCacheValue> Configuration { get; private set; }

        /// <summary>
        /// Add a cache handle configuration with the required name and type attributes.
        /// </summary>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <typeparam name="TCacheHandle">The type of the cache handle implementation</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if handleName or handleType are null.</exception>
        public ConfigurationBuilderCacheHandlePart<TCacheValue> WithHandle<TCacheHandle>(string handleName) where TCacheHandle : ICacheHandle<TCacheValue>
        {
            if (string.IsNullOrWhiteSpace(handleName))
            {
                throw new ArgumentNullException("handleName");
            }

            var handleCfg = CacheHandleConfiguration<TCacheValue>.Create<TCacheHandle>(this.Configuration.Name, handleName);
            this.Configuration.CacheHandles.Add(handleCfg);
            var part = new ConfigurationBuilderCacheHandlePart<TCacheValue>(handleCfg, this);
            return part;
        }

        /// <summary>
        /// Sets the update mode of the cache.
        /// <para>
        /// If nothing is set, the default will be <c>CacheUpdateMode.None</c>.
        /// </para>
        /// </summary>
        /// <param name="updateMode">The update mode.</param>
        /// <seealso cref="CacheUpdateMode"/>
        public ConfigurationBuilderCachePart<TCacheValue> WithUpdateMode(CacheUpdateMode updateMode)
        {
            this.Configuration.CacheUpdateMode = updateMode;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of retries per action.
        /// <para>Default is <see cref="int.MaxValue"/>.</para>
        /// <para>Not every cache handle implements this, usually only distributed caches will use it.</para>
        /// </summary>
        /// <param name="retries">The maximum number of retries.</param>
        /// <returns>The configuration builder.</returns>
        public ConfigurationBuilderCachePart<TCacheValue> WithMaxRetries(int retries)
        {
            if (retries <= 0)
            {
                throw new InvalidOperationException("Maximum number of retries must be greater than 0.");
            }

            this.Configuration.MaxRetries = retries;
            return this;
        }

        /// <summary>
        /// Sets the timeout between each retry of an action in milliseconds.
        /// <para>Default is 10.</para>
        /// <para>Not every cache handle implements this, usually only distributed caches will use it.</para>
        /// </summary>
        /// <param name="timeoutMillis">The timeout in milliseconds.</param>
        /// <returns>The configuration builder.</returns>
        public ConfigurationBuilderCachePart<TCacheValue> WithRetryTimeout(int timeoutMillis)
        {
            if (timeoutMillis < 0)
            {
                throw new InvalidOperationException("Retry timeout must be greater than or equal to zero.");
            }

            this.Configuration.RetryTimeout = timeoutMillis;
            return this;
        }

        /// <summary>
        /// Defines a redis configuration.
        /// <para>
        /// This will only be used and is only needed if the redis cache handle implementation will be used.
        /// </para>
        /// </summary>
        /// <param name="config">The redis configuration object.</param>
        /// <returns>The configuration builder.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redis")]
        public ConfigurationBuilderCachePart<TCacheValue> WithRedisConfiguration(RedisConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.Configuration.RedisConfigurations.Add(config);
            return this;
        }
    }
}