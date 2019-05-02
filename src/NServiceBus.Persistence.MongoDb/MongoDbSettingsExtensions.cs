﻿namespace NServiceBus.Persistence.MongoDB
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Persistence.MongoDB.Database;

    public static class MongoDbSettingsExtensions
    {
        public static PersistenceExtensions<MongoDbPersistence> SetConnectionStringName(
            this PersistenceExtensions<MongoDbPersistence> cfg, string connectionStringName)
        {
            cfg.GetSettings().Set(MongoPersistenceSettings.ConnectionStringName, connectionStringName);
            return cfg;
        }

        public static PersistenceExtensions<MongoDbPersistence> SetConnectionString(
            this PersistenceExtensions<MongoDbPersistence> cfg, string connectionString)
        {
            cfg.GetSettings().Set(MongoPersistenceSettings.ConnectionString, connectionString);
            return cfg;
        }

    }
}