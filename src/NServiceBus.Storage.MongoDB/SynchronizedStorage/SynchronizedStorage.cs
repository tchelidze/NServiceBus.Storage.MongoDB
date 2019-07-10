﻿using System;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

            if (!context.Settings.TryGet(SettingsKeys.UseTransactions, out bool useTransactions))
            {
                useTransactions = true;
            }

            try
            {
                client.GetDatabase(databaseName);
            }
            catch (ArgumentException ex)
            {
                throw new Exception($"The persistence database name '{databaseName}' is invalid. Configure a valid database name by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().DatabaseName(databaseName)'.", ex);
            }

            try
            {
                using (var session = client.StartSession())
                {
                    if (useTransactions)
                    {
                        if (client.Cluster.Description.Type != ClusterType.ReplicaSet)
                        {
                            throw new Exception("Transactions are only supported on a replica set. Disable support for transactions by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.");
                        }

                        try
                        {
                            session.StartTransaction();
                            session.AbortTransaction();
                        }
                        catch (NotSupportedException ex)
                        {
                            throw new Exception($"Transactions are not supported by the MongoDB server. Disable support for transactions by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.", ex);
                        }
                    }
                }
            }
            catch (NotSupportedException ex)
            {
                throw new Exception("Sessions are not supported by the MongoDB server. The NServiceBus.Storage.MongoDB persistence requires MongoDB server version 3.6 or greater.", ex);
            }
            catch (TimeoutException ex)
            {
                throw new Exception("Unable to connect to the MongoDB server. Check the connection settings, and verify the server is running and accessible.", ex);
            }

            context.Container.ConfigureComponent(() => new StorageSessionFactory(client, useTransactions, databaseName, collectionNamingConvention), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<StorageSessionAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}