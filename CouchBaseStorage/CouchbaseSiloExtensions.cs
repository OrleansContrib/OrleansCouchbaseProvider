#region

using CouchbaseProviders.Options;
using CouchbaseProviders.Reminders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;
using System;
using Couchbase.Configuration.Client;
using CouchbaseProviders.CouchbaseComm;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Messaging;

#endregion

// ReSharper disable AccessToStaticMemberViaDerivedType
// ReSharper disable CheckNamespace

namespace Orleans.Hosting
{
    /// <summary>
    ///     Extension methods for configuration classes 
    /// </summary>
    public static partial class CouchbaseSiloExtensions
    {

        /// <summary>
        /// Configure ISiloHostBuilder to use CouchbaseBasedMembership
        /// </summary>
        public static ISiloHostBuilder UseCouchbaseClustering(this ISiloHostBuilder builder, Action<CouchbaseProvidersSettings> configurator = null)
        {
            return builder.ConfigureServices(services => 
                services.AddCouchbaseMembershipTable(configurator));
        }

        public static ISiloHostBuilder UseCouchbaseClustering(this ISiloHostBuilder builder, IConfigurationRoot configuration)
        {
            return builder.ConfigureServices(services => 
                services.AddCouchbaseMembershipTable(configuration));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use CouchbaseMembershipTable
        /// </summary>
        public static ISiloHostBuilder UseCouchbaseMembershipTable(this ISiloHostBuilder builder, IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddCouchbaseMembershipTable(configuration));
        }

        /// <summary>
        /// Configure silo to use CouchbaseMembershipTable.
        /// </summary>
        public static IServiceCollection AddCouchbaseMembershipTable(this IServiceCollection services,
            Action<CouchbaseProvidersSettings> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IMembershipTable, CouchbaseMembershipProvider>();
//            services.AddSingleton<IConfigurationValidator, CouchbaseOptionsValidator<CouchbaseMembershipTableOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use CouchbaseMembershipTable.
        /// </summary>
        public static IServiceCollection AddCouchbaseMembershipTable(this IServiceCollection services,
            IConfigurationRoot configuration)
        {
            services.Configure<CouchbaseGatewayOptions>(configuration.GetSection("CouchbaseGatewayOptions"));
            services.Configure<CouchbaseClientDefinition>(configuration.GetSection("Couchbase"));
            services.Configure<CouchbaseProvidersSettings>(configuration.GetSection("CouchbaseProvidersSettings"));
            services.AddSingleton<IBucketFactory, BucketFactory>();                
            services.AddSingleton<IMembershipTable, CouchbaseMembershipProvider>();
//            services.AddSingleton<IConfigurationValidator, CouchbaseOptionsValidator<CouchbaseMembershipTableOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use CouchbaseMembershipTable.
        /// </summary>
        public static IServiceCollection AddCouchbaseMembershipTable(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<CouchbaseProvidersSettings>(configuration.GetSection(typeof(CouchbaseProvidersSettings).Name));
            services.AddSingleton<IMembershipTable, CouchbaseMembershipProvider>();
//            services.AddSingleton<IConfigurationValidator, CouchbaseOptionsValidator<CouchbaseProvidersSettings>>();

            return services;
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use CouchbaseReminderTable.
        /// </summary>
        public static ISiloHostBuilder UseCouchbaseReminders(this ISiloHostBuilder builder,
            Action<CouchbaseProvidersSettings> configurator = null)
        {
            return builder.ConfigureServices(services => services.AddCouchbaseReminders(configurator));
        }

        /// <summary>
        /// Configure ISiloHostBuilder to use CouchbaseReminderTable
        /// </summary>
        public static ISiloHostBuilder UseCouchbaseReminders(this ISiloHostBuilder builder,
            IConfiguration configuration)
        {
            return builder.ConfigureServices(services => services.AddCouchbaseReminders(configuration));
        }

        /// <summary>
        /// Configure silo to use CouchbaseReminderTable.
        /// </summary>
        public static IServiceCollection AddCouchbaseReminders(this IServiceCollection services,
            Action<CouchbaseProvidersSettings> configurator = null)
        {
            services.Configure(configurator ?? (x => { }));
            services.AddSingleton<IReminderTable, CouchbaseReminderProvider>();
//            services.AddSingleton<IConfigurationValidator, CouchbaseOptionsValidator<CouchbaseRemindersOptions>>();

            return services;
        }

        /// <summary>
        /// Configure silo to use CouchbaseReminderTable.
        /// </summary>
        public static IServiceCollection AddCouchbaseReminders(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<CouchbaseProvidersSettings>(configuration.GetSection(typeof(CouchbaseProvidersSettings).Name));
            services.AddSingleton<IReminderTable, CouchbaseReminderProvider>();
//            services.AddSingleton<IConfigurationValidator, CouchbaseOptionsValidator<CouchbaseRemindersOptions>>();

            return services;
        }


        public static IClientBuilder UseCouchbaseGatewayListProvider(this IClientBuilder builder, Action<CouchbaseGatewayOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseCouchbaseGatewayListProvider(configureOptions));
        }

        public static IClientBuilder UseCouchbaseGatewayListProvider(this IClientBuilder builder, IConfigurationRoot configuration)
        {
            return builder.ConfigureServices(services =>
            {
                services.Configure<CouchbaseGatewayOptions>(configuration.GetSection("CouchbaseGatewayOptions"));
                services.Configure<CouchbaseClientDefinition>(configuration.GetSection("Couchbase"));
                services.Configure<CouchbaseProvidersSettings>(configuration.GetSection("CouchbaseProvidersSettings"));
                services.AddSingleton<IBucketFactory, BucketFactory>();                
//                services.AddSingleton<IMembershipTable, CouchbaseMembershipProvider>();
                services.AddSingleton<IGatewayListProvider, CouchbaseGatewayListProvider>();
            });
        }
        public static IClientBuilder UseCouchbaseGatewayListProvider(this IClientBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddOptions<CouchbaseGatewayOptions>();
                services.AddSingleton<IGatewayListProvider, CouchbaseGatewayListProvider>();
            });
        }

        public static IClientBuilder UseCouchbaseGatewayListProvider(this IClientBuilder builder, Action<OptionsBuilder<CouchbaseGatewayOptions>> configureOptions)
        {
            return builder.ConfigureServices(services => services.UseCouchbaseGatewayListProvider(configureOptions));
        }

        public static IServiceCollection UseCouchbaseGatewayListProvider(this IServiceCollection services,
            Action<CouchbaseGatewayOptions> configureOptions)
        {
            return services.UseCouchbaseGatewayListProvider(ob => ob.Configure(configureOptions));
        }

        public static IServiceCollection UseCouchbaseGatewayListProvider(this IServiceCollection services,
            Action<OptionsBuilder<CouchbaseGatewayOptions>> configureOptions)
        {
            configureOptions?.Invoke(services.AddOptions<CouchbaseGatewayOptions>());
            services.AddSingleton<IBucketFactory, BucketFactory>();
            return services.AddSingleton<IGatewayListProvider, CouchbaseGatewayListProvider>();
        }
    }

}