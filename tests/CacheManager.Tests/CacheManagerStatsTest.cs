﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Cache;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    /// <summary>
    ///
    /// </summary>
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class CacheManagerStatsTest : BaseCacheManagerTest
    {
        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Stats_AddGet<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var addCalls = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
                var getCalls = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.GetCalls));
                var misses = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Misses));
                var hits = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Hits));
                var items = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Items));

                // act
                // get without region, should not return anything and should not trigger the event
                var a1 = cache.Add("key1", "something");
                var a2 = cache.Add("key1", "something"); // should not increase

                // bot gets should increase first handle +1 and hits +1
                var r1 = cache.Get("key1");
                var r2 = cache["key1"];

                // should increase all handles get + 1 and misses +1
                cache.Get("key1", "region");

                // assert
                a1.Should().BeTrue();
                a2.Should().BeFalse();
                r1.Should().Be("something");
                r2.Should().Be("something");
                // each cachhandle stats should have one addCall increase
                addCalls.ShouldAllBeEquivalentTo(Enumerable.Repeat(1, cache.CacheHandles.Count));

                // we called get 3 times but only the first handle should have a stat increase!
                // but we had one miss, so all handles have been called to retrieve the item
                getCalls.ShouldAllBeEquivalentTo(
                    new[] { 3 }.Concat(Enumerable.Repeat(1, cache.CacheHandles.Count - 1)));

                // we have one miss
                misses.ShouldAllBeEquivalentTo(Enumerable.Repeat(1, cache.CacheHandles.Count));

                // first one should have 2 hits, all others 0 hits.
                hits.ShouldAllBeEquivalentTo(
                    new[] { 2 }.Concat(Enumerable.Repeat(0, cache.CacheHandles.Count - 1)));

                items.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(1, cache.CacheHandles.Count));
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Stats_Clear<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var clears = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.ClearCalls));
                var a1 = cache.Add("key1", "something");
                var a2 = cache.Add("key2", "something");

                // act
                cache.ClearRegion("region"); // should not trigger
                cache.Clear();
                cache.Clear();

                // assert
                // all handles should have 2 clear increases.
                clears.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count));
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Stats_ClearRegion<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var clears = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls));
                var a1 = cache.Add("key1", "something");
                var a2 = cache.Add("key2", "something");
                var a3 = cache.Add("key2", "something", "region");

                // act
                cache.ClearRegion("region");
                cache.Clear();  // should not trigger
                cache.ClearRegion("region2");

                // assert
                // all handles should have 2 clearRegion increases.
                clears.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count));
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Stats_Put<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var puts = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.PutCalls));

                // act
                cache.Put("key1", "something");
                cache.Put("key2", "something");
                cache.Put("key2", "something", "region");

                // assert
                // all handles should have 2 clearRegion increases.
                puts.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(3, cache.CacheHandles.Count));
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Stats_Update<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var puts = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.PutCalls));
                var gets = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.GetCalls));
                var hits = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Hits));
                cache.Add("key1", "something");
                cache.Add("key2", "something");

                // act
                cache.Update("key1", v => "somethingelse");
                cache.Update("key2", v => "somethingelse");

                // assert
                puts.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count));
                gets.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count));
                hits.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count));
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Stats_Remove<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var adds = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
                var removes = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.RemoveCalls));

                // act
                var r1 = cache.Remove("key2");               // false
                var r2 = cache.Remove("key2", "region");     // false

                var a1 = cache.Add("key1", "something");            // true
                var a2 = cache.Add("key2", "something");            // true
                var a3 = cache.Add("key2", "something", "region");  // true
                var a4 = cache.Add("key1", "something");            // false
                var r3 = cache.Remove("key2");                      // true
                var r4 = cache.Remove("key2", "region");            // true
                var a5 = cache.Add("key2", "something");            // true
                var a6 = cache.Add("key2", "something", "region");  // true

                // assert
                (r1 && r2).Should().BeFalse();
                (r3 && r4).Should().BeTrue();
                a4.Should().BeFalse();
                (a1 && a2 && a3 && a5 && a6).Should().BeTrue();

                // all handles should have 5 add increases.
                adds.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(5, cache.CacheHandles.Count));

                // we have removed 2 items
                removes.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count));
            }
        }
                
        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Stats_Threaded<T>(T cache) where T : ICacheManager<object>
        {
            var puts = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.PutCalls));
            var adds = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
            var threads = 5;
            var iterations = 10;

            using (cache)
            {
                cache.Clear();
                ThreadTestHelper.Run(() =>
                {
                    cache.Add("key1", "hi");

                    cache.Put("key1", "changed");
                }, threads, iterations);
            }

            // item should have been added only once
            adds.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(1, cache.CacheHandles.Count));

            puts.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(threads * iterations, cache.CacheHandles.Count));
        }
    }
}