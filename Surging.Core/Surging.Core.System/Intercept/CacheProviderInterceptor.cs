﻿using Surging.Core.ProxyGenerator.Interceptors;
using System.Threading.Tasks;
using System.Linq;
using Surging.Core.Caching;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Surging.Core.System.Intercept
{
    public class CacheProviderInterceptor : IInterceptor
    {
        public async Task Intercept(IInvocation invocation)
        {
            var attribute =
                 invocation.Attributes.Where(p => p is InterceptMethodAttribute)
                 .Select(p => p as InterceptMethodAttribute).FirstOrDefault();
            var cacheKey = string.Format(attribute.Key, invocation.CacheKey);
            await CacheIntercept(attribute, cacheKey, invocation);
        }

        private async Task CacheIntercept(InterceptMethodAttribute attribute, string key, IInvocation invocation)
        {
            ICacheProvider cacheProvider = null;
            switch (attribute.Mode)
            {
                case CacheTargetType.Redis:
                    {
                        cacheProvider = CacheContainer.GetInstances<ICacheProvider>(string.Format("{0}.{1}",
                           attribute.CacheSectionType.ToString(), CacheTargetType.Redis.ToString()));
                        break;
                    }
                case CacheTargetType.MemoryCache:
                    {
                        cacheProvider = CacheContainer.GetInstances<ICacheProvider>(CacheTargetType.MemoryCache.ToString());
                        break;
                    }
            }
            if (cacheProvider != null) await Invoke(cacheProvider, attribute, key, invocation);
        }

        private async Task Invoke(ICacheProvider cacheProvider,InterceptMethodAttribute attribute, string key, IInvocation invocation)
        {
            switch (attribute.Method)
            {
                case CachingMethod.Get:
                    {
                        var retrunValue = await cacheProvider.GetFromCacheFirst(key, async() =>
                        {
                            await invocation.Proceed();
                            return invocation.ReturnValue;
                        }, invocation.ReturnType, attribute.Time);
                        invocation.ReturnValue = retrunValue;
                        break;
                    }
                default:
                    {
                        var keys = attribute.CorrespondingKeys.Select(correspondingKey => string.Format(correspondingKey, key)).ToList();
                        keys.ForEach(cacheProvider.Remove);
                        await invocation.Proceed();
                        break;
                    }
            }
        }
    }
}
