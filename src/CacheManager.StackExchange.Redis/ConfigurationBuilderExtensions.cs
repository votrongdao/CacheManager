﻿using System;
using CacheManager.Core.Configuration;
using CacheManager.Redis;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the redis cache handle.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the back plate for the cache manager.
        /// <para>
        /// The <paramref name="redisConfigurationId"/> is used to define the redis configuration,
        /// the back plate should use to connect to the redis server.
        /// </para>
        /// <para>
        /// If a back plate is defined, at least one cache handle must be marked as back plate
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="redisConfigurationId">
        /// The id of the configuration the back plate should use.
        /// </param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithRedisBackPlate(this ConfigurationBuilderCachePart part, string redisConfigurationId)
        {
            return part.WithBackPlate<RedisCacheBackPlate>(redisConfigurationId);
        }

        /// <summary>
        /// Add a <see cref="RedisCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a redis configuration to be defined with the
        /// <paramref name="redisConfigurationId"/> matching the configuration's id.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="redisConfigurationId">
        /// The redis configuration identifier will be used as name for the cache handle and to
        /// retrieve the connection configuration.
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationId)
        {
            return WithRedisCacheHandle(part, redisConfigurationId, false);
        }

        /// <summary>
        /// Add a <see cref="RedisCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a redis configuration to be defined with the
        /// <paramref name="redisConfigurationId"/> matching the configuration's id.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="redisConfigurationId">
        /// The redis configuration identifier will be used as name for the cache handle and to
        /// retrieve the connection configuration.
        /// </param>
        /// <param name="isBackPlateSource">
        /// Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or handleType are null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationId, bool isBackPlateSource)
        {
            return part.WithHandle(typeof(RedisCacheHandle<>), redisConfigurationId, isBackPlateSource);
        }
    }
}