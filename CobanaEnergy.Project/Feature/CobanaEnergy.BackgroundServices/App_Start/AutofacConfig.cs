using System.Reflection;
using System.Web.Http;
using System.Collections.Generic;
using Autofac;
using Autofac.Integration.WebApi;
using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.Models;
using CobanaEnergy.BackgroundServices.CQRS.Commands;
using CobanaEnergy.BackgroundServices.CQRS.Queries;
using CobanaEnergy.BackgroundServices.CQRS.Handlers.Commands;
using CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries;
using CobanaEnergy.BackgroundServices.Services;

namespace CobanaEnergy.BackgroundServices.App_Start
{
    /// <summary>
    /// Autofac Dependency Injection configuration
    /// </summary>
    public static class AutofacConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            // Register API controllers
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            // Register DbContext
            builder.Register(c => new ApplicationDBContext())
                .As<ApplicationDBContext>()
                .InstancePerRequest();

            // ===================================================================
            // Logger Service
            // ===================================================================
            // Logger will be initialized in BaseApiController.Initialize() 
            // where ControllerContext is available
            builder.RegisterType<LoggerService>()
                .As<ILoggerService>()
                .InstancePerRequest();

            // ===================================================================
            // CQRS - Query Handlers (Read Operations)
            // ===================================================================
            
            // Reusable query handlers
            builder.RegisterType<GetContractsByStatusQueryHandler>()
                .As<IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>>>()
                .InstancePerRequest();

            builder.RegisterType<GetCommissionRecordByEIdQueryHandler>()
                .As<IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto>>()
                .InstancePerRequest();

            builder.RegisterType<GetContractDetailsByTypeQueryHandler>()
                .As<IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto>>()
                .InstancePerRequest();

            builder.RegisterType<GetPostSaleObjectionByEIdQueryHandler>()
                .As<IQueryHandler<GetPostSaleObjectionByEIdQuery, PostSaleObjectionDto>>()
                .InstancePerRequest();

            // ===================================================================
            // CQRS - Command Handlers (Write Operations)
            // ===================================================================
            
            builder.RegisterType<ProcessFutureContractsCommandHandler>()
                .As<ICommandHandler<ProcessFutureContractsCommand, ProcessContractsResult>>()
                .InstancePerRequest();

            builder.RegisterType<ProcessOverdueContractsCommandHandler>()
                .As<ICommandHandler<ProcessOverdueContractsCommand, ProcessOverdueContractsResult>>()
                .InstancePerRequest();

            builder.RegisterType<ProcessObjectionDateCommandHandler>()
                .As<ICommandHandler<ProcessObjectionDateCommand, ProcessObjectionDateResult>>()
                .InstancePerRequest();

            builder.RegisterType<ProcessObjectionCountCommandHandler>()
                .As<ICommandHandler<ProcessObjectionCountCommand, ProcessObjectionCountResult>>()
                .InstancePerRequest();

            builder.RegisterType<ProcessRenewalWindowDateCommandHandler>()
                .As<ICommandHandler<ProcessRenewalWindowDateCommand, ProcessRenewalWindowDateResult>>()
                .InstancePerRequest();

            builder.RegisterType<ProcessContractEndedAgLostDateCommandHandler>()
                .As<ICommandHandler<ProcessContractEndedAgLostDateCommand, ProcessContractEndedDateResult>>()
                .InstancePerRequest();

            builder.RegisterType<ProcessContractEndedNotRenewedDateCommandHandler>()
                .As<ICommandHandler<ProcessContractEndedNotRenewedDateCommand, ProcessContractEndedDateResult>>()
                .InstancePerRequest();

            builder.RegisterType<ProcessContractEndedRenewedDateCommandHandler>()
                .As<ICommandHandler<ProcessContractEndedRenewedDateCommand, ProcessContractEndedDateResult>>()
                .InstancePerRequest();


            // Build the container
            var container = builder.Build();

            // Set the dependency resolver for Web API
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}

