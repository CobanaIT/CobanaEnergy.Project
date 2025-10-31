using Autofac;
using Autofac.Integration.Mvc;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Service.BackgroundServices;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Filters
{
    public static class AutofacConfig
    {
        public static IContainer Container { get; private set; }
        public static void RegisterDependencies()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(Assembly.GetExecutingAssembly());

            builder.RegisterType<ApplicationDBContext>()
                   .AsSelf()
                   .InstancePerLifetimeScope();

            // ✅ Register SignalR IConnectionManager
            builder.Register(c => GlobalHost.ConnectionManager)
                   .As<IConnectionManager>()
                   .SingleInstance();

            builder.RegisterType<CampaignMonitorService>()
               .AsSelf()
               .InstancePerLifetimeScope();

            builder.RegisterType<UserSessionMonitorService>()
               .AsSelf()
               .InstancePerLifetimeScope();

            Container = builder.Build();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(Container));

        }
    }
}

//Autofac Lifetimes
//builder.RegisterType<MyService>().As<IMyService>().SingleInstance();- Singleton
//.InstancePerRequest(); - Scoped / Per HTTP Request
//builder.RegisterType<MyUtility>().As<IMyUtility>().InstancePerDependency();- Transient / New Instance
//builder.RegisterType<MyService>().As<IMyService>().InstancePerLifetimeScope();- Per Lifetime Scope (Manual scopes)