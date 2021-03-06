using System;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.ServiceModel.Dispatcher;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Practices.Unity;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;
using RightpointLabs.ConferenceRoom.Services.SignalR;
using Unity.WebApi;

namespace RightpointLabs.ConferenceRoom.Services
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();

            var connectionStrings = System.Web.Configuration.WebConfigurationManager.ConnectionStrings;
            var connectionString = connectionStrings["Mongo"].ConnectionString;
            var providerName = connectionStrings["Mongo"].ProviderName;
            var exchangeUsername = ConfigurationManager.AppSettings["username"];
            var exchangePassword = ConfigurationManager.AppSettings["password"];
            var exchangeServiceUrl = ConfigurationManager.AppSettings["serviceUrl"];
            var plivoAuthId = ConfigurationManager.AppSettings["plivoAuthId"];
            var plivoAuthToken = ConfigurationManager.AppSettings["plivoAuthToken"];
            var plivoFrom = ConfigurationManager.AppSettings["plivoFrom"];
            var gdoBaseUrl = ConfigurationManager.AppSettings["gdoBaseUrl"];
            var gdoApiKey = ConfigurationManager.AppSettings["gdoApiKey"];
            var gdoUsername = ConfigurationManager.AppSettings["gdoUsername"];
            var gdoPassword = ConfigurationManager.AppSettings["gdoPassword"];

            container.RegisterType<IMongoConnectionHandler, MongoConnectionHandler>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(connectionString, providerName));

            var serviceBuilder = ExchangeConferenceRoomService.GetExchangeServiceBuilder(
                exchangeUsername,
                exchangePassword,
                exchangeServiceUrl);

            container.RegisterType<IInstantMessagingService>(new HierarchicalLifetimeManager(),
                new InjectionFactory(c => new InstantMessagingService(
                    exchangeUsername,
                    exchangePassword)));

            container.RegisterType<ISmsMessagingService>(new HierarchicalLifetimeManager(),
                new InjectionFactory(c => new SmsMessagingService(
                    plivoAuthId,
                    plivoAuthToken,
                    plivoFrom)));

            container.RegisterType<IGdoService>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(c => new GdoService(
                    new Uri(gdoBaseUrl),
                    gdoApiKey,
                    gdoUsername,
                    gdoPassword)));

            container.RegisterType<ISmsAddressLookupService, SmsAddressLookupService>(new HierarchicalLifetimeManager());
            container.RegisterType<ISignatureService, SignatureService>(new ContainerControlledLifetimeManager());
            container.RegisterType<Func<ExchangeService>>(new HierarchicalLifetimeManager(), new InjectionFactory(c => serviceBuilder));
            container.RegisterType<IBroadcastService, SignalrBroadcastService>(new HierarchicalLifetimeManager());
            container.RegisterType<IConferenceRoomService, ExchangeConferenceRoomService>(new HierarchicalLifetimeManager());
            container.RegisterType<IMeetingRepository, MeetingRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<ISecurityRepository, SecurityRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<IConnectionManager>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => GlobalHost.ConnectionManager));
            container.RegisterType<IExchangeServiceManager, ExchangeServiceManager>(new ContainerControlledLifetimeManager());
            container.RegisterType<IDateTimeService>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new DateTimeService(TimeSpan.FromHours(0))));
            container.RegisterType<IMeetingCacheService, MeetingCacheService>(new ContainerControlledLifetimeManager()); // singleton cache
            container.RegisterType<ISimpleTimedCache, SimpleTimedCache>(new ContainerControlledLifetimeManager()); // singleton cache
            container.RegisterType<IRoomRepository, RoomRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<IBuildingService, BuildingService>(new HierarchicalLifetimeManager());
            container.RegisterType<IBuildingRepository, BuildingRepository>(new HierarchicalLifetimeManager());

            // create change notifier in a child container and register as a singleton with the main container (avoids creating it's dependencies in the global container)
            var child = container.CreateChildContainer();
            var changeNotificationService = child.Resolve<ChangeNotificationService>();
            container.RegisterInstance(typeof(IChangeNotificationService), changeNotificationService, new ContainerControlledLifetimeManager());

            
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            
            // e.g. container.RegisterType<ITestService, TestService>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }

        private class SignalrBroadcastService : IBroadcastService
        {
            private readonly IConnectionManager _connectionManager;

            public SignalrBroadcastService(IConnectionManager connectionManager)
            {
                _connectionManager = connectionManager;
            }

            public void BroadcastUpdate(string roomAddress)
            {
                var context = _connectionManager.GetHubContext<UpdateHub>();
                context.Clients.All.Update(roomAddress);
            }
        }
    }
}