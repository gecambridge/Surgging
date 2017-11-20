﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Surging.Core.CPlatform.Messages;
using System.Collections.Concurrent;
using Surging.Core.CPlatform.Runtime.Client;

namespace Surging.Core.CPlatform.Support.Implementation
{
    public class BreakeRemoteInvokeService : IBreakeRemoteInvokeService
    {
        private readonly IServiceCommandProvider _commandProvider;
        private readonly IRemoteInvokeService _remoteInvokeService;
        private readonly ConcurrentDictionary<string, ServiceInvokeListenInfo> _serviceInvokeListenInfo = new ConcurrentDictionary<string, ServiceInvokeListenInfo>();

        public BreakeRemoteInvokeService(IServiceCommandProvider commandProvider, IRemoteInvokeService remoteInvokeService)
        {
            _commandProvider = commandProvider;
            _remoteInvokeService = remoteInvokeService;
        }

        public async Task<RemoteInvokeResultMessage> InvokeAsync(IDictionary<string, object> parameters, string serviceId, string serviceKey, bool decodeJOject)
        {
            var serviceInvokeInfos = _serviceInvokeListenInfo.GetOrAdd(serviceId, new ServiceInvokeListenInfo());
            var command =await _commandProvider.GetCommand(serviceId);
            var intervalSeconds = (DateTime.Now - serviceInvokeInfos.FinalRemoteInvokeTime).TotalSeconds;
            bool reachConcurrentRequest() => serviceInvokeInfos.ConcurrentRequests > command.MaxConcurrentRequests;
            bool reachRequestVolumeThreshold() => intervalSeconds <= 10
                && serviceInvokeInfos.SinceFaultRemoteServiceRequests > command.BreakerRequestVolumeThreshold;
            bool reachErrorThresholdPercentage() =>
                serviceInvokeInfos.FaultRemoteServiceRequests / (serviceInvokeInfos.RemoteServiceRequests??1)*100 > command.BreakeErrorThresholdPercentage;
            if (command.BreakerForceClosed)
            {
                _serviceInvokeListenInfo.AddOrUpdate(serviceId, new ServiceInvokeListenInfo(), (k, v) => { v.LocalServiceRequests++; return v; });
                return null;
            }
            else
            {
                if (reachConcurrentRequest() || reachRequestVolumeThreshold() || reachErrorThresholdPercentage())
                {
                    if (intervalSeconds*1000 > command.BreakeSleepWindowInMilliseconds)
                    {
                        return await MonitorRemoteInvokeAsync(parameters, serviceId, serviceKey, decodeJOject);
                    }
                    else
                    {
                        _serviceInvokeListenInfo.AddOrUpdate(serviceId, new ServiceInvokeListenInfo(), (k, v) => { v.LocalServiceRequests++; return v; });
                        return null;
                    }
                }
                else
                {
                    return await  MonitorRemoteInvokeAsync(parameters, serviceId, serviceKey, decodeJOject);
                }
            }
            throw new NotImplementedException();
        }

        private async Task<RemoteInvokeResultMessage> MonitorRemoteInvokeAsync(IDictionary<string, object> parameters, string serviceId, string serviceKey, bool decodeJOject)
        {
            var serviceInvokeInfo = _serviceInvokeListenInfo.GetOrAdd(serviceId, new ServiceInvokeListenInfo());
            try
            {
                _serviceInvokeListenInfo.AddOrUpdate(serviceId, new ServiceInvokeListenInfo(), (k, v) =>
                {
                    v.RemoteServiceRequests = v.RemoteServiceRequests == null ? 1 : ++v.RemoteServiceRequests;
                    v.FinalRemoteInvokeTime = DateTime.Now;
                    ++v.ConcurrentRequests;
                    return v;
                });
               var message = await _remoteInvokeService.InvokeAsync(new RemoteInvokeContext
                {
                    InvokeMessage = new RemoteInvokeMessage
                    {
                        Parameters = parameters,
                        ServiceId = serviceId,
                        ServiceKey = serviceKey,
                         DecodeJOject= decodeJOject,
                    }
                });
                _serviceInvokeListenInfo.AddOrUpdate(serviceId, new ServiceInvokeListenInfo(), (k, v) =>
                {
                    v.SinceFaultRemoteServiceRequests = 0;
                    --v.ConcurrentRequests; return v;
                });

                return message;
            }
            catch
            {
                _serviceInvokeListenInfo.AddOrUpdate(serviceId, new ServiceInvokeListenInfo(), (k, v) =>
                {
                    ++v.FaultRemoteServiceRequests;
                    ++v.SinceFaultRemoteServiceRequests;
                    --v.ConcurrentRequests;
                    return v;
                });
                return   null;
            }
        }
    }
}
