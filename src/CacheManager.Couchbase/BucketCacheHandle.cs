﻿using System;
using System.Security.Cryptography;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Newtonsoft.Json.Linq;

namespace CacheManager.Couchbase
{
    /// <summary>
    /// Cache handle implementation based on the couchbase .net client.
    /// </summary>
    public class BucketCacheHandle : BucketCacheHandle<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BucketCacheHandle"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public BucketCacheHandle(ICacheManager<object> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }

    /// <summary>
    /// Cache handle implementation based on the couchbase .net client.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class BucketCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private readonly IBucket bucket;
        private readonly BucketConfiguration bucketConfiguration;
        private readonly string bucketName = "default";
        private readonly ClientConfiguration configuration;
        private readonly string configurationName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.InvalidOperationException">
        /// If <c>configuration.HandleName</c> is not valid.
        /// </exception>
        public BucketCacheHandle(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            // we can configure the bucket name by having "<configKey>:<bucketName>" as handle's
            // name value
            var nameParts = configuration.HandleName.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length == 0)
            {
                throw new InvalidOperationException("Handle name is not valid " + configuration.HandleName);
            }

            this.configurationName = nameParts[0];

            if (nameParts.Length == 2)
            {
                this.bucketName = nameParts[1];
            }

            this.configuration = CouchbaseConfigurationManager.GetConfiguration(this.configurationName);
            this.bucketConfiguration = CouchbaseConfigurationManager.GetBucketConfiguration(this.configuration, this.bucketName);
            this.bucket = CouchbaseConfigurationManager.GetBucket(this.configuration, this.configurationName, this.bucketName);
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count
        {
            get { return (int)this.Stats.GetStatistic(CacheStatsCounterType.Items); }
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            // warning: takes ~20seconds to flush the bucket... thats rigged
            var manager = this.bucket.CreateManager(this.bucketConfiguration.Username, this.bucketConfiguration.Password);
            if (manager != null)
            {
                manager.Flush();
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.NotImplementedException">Not supported in this version.</exception>
        public override void ClearRegion(string region)
        {
            // TODO: not supported?
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            var fullKey = this.GetKey(item.Key, item.Region);
            if (item.ExpirationMode != ExpirationMode.None)
            {
                return this.bucket.Insert(fullKey, item, item.ExpirationTimeout).Success;
            }

            return this.bucket.Insert(fullKey, item).Success;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposeManaged">Indicator if managed resources should be released.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullkey = this.GetKey(key, region);
            var result = this.bucket.Get<CacheItem<TCacheValue>>(fullkey);

            //// TODO: implement sliding expiration whenever the guys from couchbase actually
            ////       implement that feature into that client...

            if (result.Success)
            {
                var cacheItem = result.Value;
                if (cacheItem.Value.GetType() == typeof(JValue))
                {
                    var value = cacheItem.Value as JValue;
                    cacheItem = cacheItem.WithValue((TCacheValue)value.ToObject(cacheItem.ValueType));
                }
                else if (cacheItem.Value.GetType() == typeof(JObject))
                {
                    var value = cacheItem.Value as JObject;
                    cacheItem = cacheItem.WithValue((TCacheValue)value.ToObject(cacheItem.ValueType));
                }

                return cacheItem;
            }

            return null;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var fullKey = this.GetKey(item.Key, item.Region);
            if (item.ExpirationMode != ExpirationMode.None)
            {
                this.bucket.Upsert(fullKey, item, item.ExpirationTimeout);
            }
            else
            {
                this.bucket.Upsert(fullKey, item);
            }
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key)
        {
            return this.RemoveInternal(key, null);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = this.GetKey(key, region);
            var result = this.bucket.Remove(fullKey);
            return result.Success;
        }

        private static string GetSHA256Key(string key)
        {
            using (var sha = SHA256Managed.Create())
            {
                byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private string GetKey(string key, string region = null)
        {
            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                fullKey = string.Concat(region, ":", key);
            }

            // Memcached still has a 250 character limit
            if (fullKey.Length >= 250)
            {
                return GetSHA256Key(fullKey);
            }

            return fullKey;
        }
    }
}