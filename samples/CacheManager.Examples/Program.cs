﻿using System;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using Microsoft.Practices.Unity;

namespace CacheManager.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            EventsExample();
            UnityInjectionExample();
            AppConfigLoadInstalledCacheCfg();
            SimpleCustomBuildConfigurationUsingConfigBuilder();
            SimpleCustomBuildConfigurationUsingFactory();
            // RedisSample();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void EventsExample()
        {
            var cache = CacheFactory.FromConfiguration<object>("myCache");
            cache.OnAdd += (sender, args) => Console.WriteLine("Added " + args.Key);
            cache.OnGet += (sender, args) => Console.WriteLine("Got " + args.Key);
            cache.OnRemove += (sender, args) => Console.WriteLine("Removed " + args.Key);
         
            cache.Add("key", "value");
            var val = cache.Get("key");
            cache.Remove("key");
        }

        private static void UnityInjectionExample()
        {
            UnityContainer container = new UnityContainer();
            container.RegisterType<ICacheManager<object>>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory((c) => CacheFactory.FromConfiguration<object>("myCache")));

            container.RegisterType<UnityInjectionExampleTarget>();

            // resolving the test target object should also resolve the cache instance
            var target = container.Resolve<UnityInjectionExampleTarget>();
            target.PutSomethingIntoTheCache();

            // our cache manager instance should still be there so should the object we added in the previous step.
            var checkTarget = container.Resolve<UnityInjectionExampleTarget>();
            checkTarget.GetSomething();
        }

        class UnityInjectionExampleTarget
        {
            private ICacheManager<object> cache;

            public UnityInjectionExampleTarget(ICacheManager<object> cache)
            {
                if (cache == null)
                {
                    throw new ArgumentNullException("cache");
                }

                this.cache = cache;
            }

            public void PutSomethingIntoTheCache()
            {
                this.cache.Put("myKey", "something");
            }

            public void GetSomething()
            {
                var value = this.cache.Get("myKey");
                var x = value;
                if (value == null)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static void AppConfigLoadInstalledCacheCfg()
        {
            var cache = CacheFactory.FromConfiguration<object>("myCache");
            cache.Add("key", "value");
        }

        private static void SimpleCustomBuildConfigurationUsingFactory()
        {
            var cache = CacheFactory.Build("myCacheName", settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithSystemRuntimeCacheHandle("handle1")
                        .EnablePerformanceCounters()
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
            });

            cache.Add("key", "value");
        }

        private static void SimpleCustomBuildConfigurationUsingConfigBuilder()
        {
            // this is using the CacheManager.Core.Configuration.ConfigurationBuilder to build a custom config
            // you can do the same with the CacheFactory
            var cfg = ConfigurationBuilder.BuildConfiguration(settings =>
                {
                    settings.WithUpdateMode(CacheUpdateMode.Up)
                        .WithSystemRuntimeCacheHandle("handle1")
                            .EnablePerformanceCounters()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
                });

            var cache = CacheFactory.FromConfiguration<string>("stringCache", cfg);
            cache.Add("key", "value");

            // reusing the configuration and using the same cache for different types:
            var numbers = CacheFactory.FromConfiguration<int>("numberCache", cfg);
            numbers.Add("intKey", 2323);
            numbers.Update("intKey", v => v + 1);
        }

        private static void RedisSample()
        {
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

            cache.Add("test", 123456);

            cache.Update("test", p => p + 1);

            var result = cache.Get("test");
        }
    }
}