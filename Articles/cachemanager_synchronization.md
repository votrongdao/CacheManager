#Cache Synchronization

## Multi-Layer Use Case
A common, more complex scenario would be, that you have a distributed cache, e.g. Redis, and you want to access that layer from multiple clients and share the cached data across those clients because the creation of the cached items is expensive, or you want to store data in one place and use it by multiple clients...  

Distributed caches are fast, but not as fast as in-process caches which keep your cache values in memory and do not have to use expensive serialization or network resources.   

In addition an application will usually read from cache a lot more than writing to it. 
Now if we put an in-process cache in front of the distributed cache, to read directly from memory, this would drastically increase the overall application's performance.   
To give you just a rough idea of the read performance difference, it can be up to 100 times faster or even more...
If a Redis cache for example can handle 10k Gets per second, a memory cache can perfrom 2 million.

### Challenges
There are some challenges with this scenario. We now store cache values in memory, what happens if the cache item was removed from cache by one client...
Of course, it will still be cached in memory by all other clients.

Let's take the following scenario: 

* ClientA removes an item from the cache. 
	* Cache Manager will call `Remove` on both cache handles.
* ClientB does a Get on the same item
	* Cache Manager doesn't find the item in the distributed cache, but in the in-process cache layer of ClientB

This means that ClientB works with out of sync data. 

To prevent this, Cache Manager has a feature called **CacheBackPlate** which will try to synchronize multiple cache clients.

## Cache Back Plates
A cache back plate can be added to the cache manager during configuration. 

### Configuration
Example for .config xml configuration:

        <cache 
	        name="redisWithBackPlate" 
	        backPlateName="redis1" 
	        backPlateType="CacheManager.StackExchange.Redis.RedisCacheBackPlate, CacheManager.StackExchange.Redis">
          <handle name="default" ref="systemCache"/>
          <handle name="redis1" ref="redis" expirationMode="None" isBackPlateSource="true"/>
        </cache>

Example for configuration by code:

    var cache = CacheFactory.Build<int>("myCache", settings =>
    {
        settings
            .WithSystemRuntimeCacheHandle("inProcessCache")
            .And
            .WithRedisConfiguration("redis", config =>
            {
                config.WithAllowAdmin()
                    .WithDatabase(0)
                    .WithEndpoint("localhost", 6379);
            })
            .WithMaxRetries(1000)
            .WithRetryTimeout(100)
            .WithRedisBackPlate("redis")
            .WithRedisCacheHandle("redis", true);
    });

In both cases configuring a back plate requires **one cache handle** being set as the back plate's source. 
In case of the xml configuration, it is the `isBackPlateSource` attribute on the cacheHandle tag and by code it is the second parameter on the `WithHandle` method being set to true.

### Back Plate's Source
The back plate's source is usually the one distributed cache layer of the Cache Manager instance. 

When for example an item gets removed by one client, the other client has to remove the same item from all other cache handles but the source (because it was already removed). So for remove this is not that important.
But let's say a cache item was updated by ClientA and ClientB still has the old version in local in-process cache. With the source being set, Cache Manager can evict the item from all ClientB's local in-process caches and on the next `Get` the new version will be retrieved from the "source".

### How does it work?
The back plate works with messages. Every time an item gets removed or updated Cache Manager will send a message to the back plate storing the information needed to update the other clients.
All other clients will receive those messages asynchronously and will react accordingly.

That being said, because of the network traffic generated and the overhead that produces, the performance of the cache will be go down slightly. Also, the synchronization will not happen on all clients at the same time, so there might be (very small) delays. 


[TOC]