﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Surging.Core.CPlatform.Support
{
   public class ServiceInvokeListenInfo
    {

        /// <summary>
        /// 远程调用请求数
        /// </summary>
        public int? RemoteServiceRequests { get; set; }

        /// <summary>
        /// 本地调用请求数
        /// </summary>
        public int LocalServiceRequests { get; set; }

        /// <summary>
        /// 首次调用时间
        /// </summary>
        public DateTime FirstInvokeTime { get; set; } =  DateTime.Now;
        /// <summary>
        /// 最后一次远程调用时间
        /// </summary>
        public DateTime FinalRemoteInvokeTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 失败调用请求数
        /// </summary>
        public int FaultRemoteServiceRequests { get; set; }

        /// <summary>
        /// 距上次失败调用次数
        /// </summary>
        public int SinceFaultRemoteServiceRequests { get; set; }

        /// <summary>
        /// 并发数
        /// </summary>
        public int ConcurrentRequests { get; set; }
    }
}
