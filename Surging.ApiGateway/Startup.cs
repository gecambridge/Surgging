﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Surging.Core.ApiGateWay;
using Surging.Core.ApiGateWay.OAuth.Implementation.Configurations;
using Surging.Core.Caching.Configurations;
using Surging.Core.Codec.MessagePack;
using Surging.Core.Codec.ProtoBuffer;
using Surging.Core.Consul;
using Surging.Core.Consul.Configurations;
using Surging.Core.CPlatform;
using Surging.Core.DotNetty;
using Surging.Core.ProxyGenerator;
using Surging.Core.ProxyGenerator.Utilitys;
using Surging.Core.System.Intercept;
using Surging.Core.System.Ioc;
//using Surging.Core.Zookeeper.Configurations;
using System;

namespace Surging.ApiGateway
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public IContainer ApplicationContainer { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(env.ContentRootPath)
              .AddCacheFile("Configs/cacheSettings.json", optional: false)
              .AddJsonFile("Configs/appsettings.json", optional: true, reloadOnChange: true)
              .AddGatewayFile("Configs/gatewaySettings.json", optional: false)
              .AddJsonFile($"Configs/appsettings.{env.EnvironmentName}.json", optional: true);
              

            Configuration = builder.Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return RegisterAutofac(services);
        }

        private IServiceProvider RegisterAutofac(IServiceCollection services)
        {
            services.AddMvc().AddJsonOptions(options => {
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });
            services.AddLogging();
            var builder = new ContainerBuilder();
            builder.Initialize();
            builder.RegisterServices();
            builder.RegisterRepositories();
            builder.RegisterModules();
            builder.Populate(services);
            builder.AddMicroService(option =>
            {

                option.AddClient();
                option.AddClientIntercepted(typeof(CacheProviderInterceptor));
                //option.UseZooKeeperManager(new ConfigInfo("127.0.0.1:2181"));
                option.UseConsulManager(new ConfigInfo("127.0.0.1:8500"));
                option.UseDotNettyTransport();
                option.AddApiGateWay();
                //option.UseProtoBufferCodec();
                option.UseMessagePackCodec();
                builder.Register(p => new CPlatformContainer(ServiceLocator.Current));
            });
            ServiceLocator.Current = builder.Build();
            return new AutofacServiceProvider(ServiceLocator.Current);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            var myProvider = new FileExtensionContentTypeProvider();
            myProvider.Mappings.Add(".tpl", "text/plain");
            app.UseStaticFiles(new StaticFileOptions() { ContentTypeProvider = myProvider });
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                "Path",
                "{*path}",
                new { controller = "Services", action = "Path" });
            });
        }
    }
}