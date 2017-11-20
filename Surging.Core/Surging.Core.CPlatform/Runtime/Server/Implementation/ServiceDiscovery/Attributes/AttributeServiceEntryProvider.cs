﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Surging.Core.CPlatform.Runtime.Server.Implementation.ServiceDiscovery.Attributes
{
    /// <summary>
    /// Service标记类型的服务条目提供程序。
    /// </summary>
    public class AttributeServiceEntryProvider : IServiceEntryProvider
    {
        #region Field

        private readonly IEnumerable<Type> _types;
        private readonly IClrServiceEntryFactory _clrServiceEntryFactory;
        private readonly ILogger<AttributeServiceEntryProvider> _logger;

        #endregion Field

        #region Constructor

        public AttributeServiceEntryProvider(IEnumerable<Type> types, IClrServiceEntryFactory clrServiceEntryFactory, ILogger<AttributeServiceEntryProvider> logger)
        {
            _types = types;
            _clrServiceEntryFactory = clrServiceEntryFactory;
            _logger = logger;
        }

        #endregion Constructor

        #region Implementation of IServiceEntryProvider

        /// <summary>
        /// 获取服务条目集合。
        /// </summary>
        /// <returns>服务条目集合。</returns>
        public IEnumerable<ServiceEntry> GetEntries()
        {
            var services = _types.Where(i =>
            {
                var typeInfo = i.GetTypeInfo();
                return typeInfo.IsInterface && typeInfo.GetCustomAttribute<ServiceBundleAttribute>() != null;
            }).Distinct().ToArray();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation($"发现了以下服务：{string.Join(",", services.Select(i => i.ToString()))}。");
            }
            var entries = new List<ServiceEntry>();
            foreach (var service in services)
            {
                entries.AddRange( _clrServiceEntryFactory.CreateServiceEntry(service));
            }
            return entries;
        }

        #endregion Implementation of IServiceEntryProvider
    }
}