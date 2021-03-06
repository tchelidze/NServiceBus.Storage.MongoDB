﻿namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Features;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Bson.Serialization.Options;
    using global::MongoDB.Bson.Serialization.Serializers;
    using global::MongoDB.Driver;
    using Microsoft.Extensions.DependencyInjection;
    using Outbox;

    class OutboxStorage : Feature
    {
        OutboxStorage()
        {
            Defaults(s => s.EnableFeatureByDefault<SynchronizedStorage>());
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.TryGet(SettingsKeys.UseTransactions, out bool useTransactions) && useTransactions == false)
            {
                throw new Exception($"Transactions are required when the Outbox is enabled, but they have been disabled by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.");
            }

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

            if (!BsonClassMap.IsClassMapRegistered(typeof(TransportOperation)))
            {
                BsonClassMap.RegisterClassMap<TransportOperation>(cm =>
                {
                    cm.AutoMap();
                    cm.MapMember(c => c.Headers).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                    cm.MapMember(c => c.Options).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                });
            }

            if (!context.Settings.TryGet(SettingsKeys.TimeToKeepOutboxDeduplicationData, out TimeSpan timeToKeepOutboxDeduplicationData))
            {
                timeToKeepOutboxDeduplicationData = TimeSpan.FromDays(7);
            }

            var collectionSettings = new MongoCollectionSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            };

            var outboxCollection = client.GetDatabase(databaseName).GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)), collectionSettings);
            var outboxCleanupIndex = outboxCollection.Indexes.List().ToList().SingleOrDefault(indexDocument => indexDocument.GetElement("name").Value == outboxCleanupIndexName);
            var existingExpiration = outboxCleanupIndex?.GetElement("expireAfterSeconds").Value.ToInt32();

            var createIndex = outboxCleanupIndex is null;

            if (existingExpiration.HasValue && TimeSpan.FromSeconds(existingExpiration.Value) != timeToKeepOutboxDeduplicationData)
            {
                outboxCollection.Indexes.DropOne(outboxCleanupIndexName);
                createIndex = true;
            }

            if (createIndex)
            {
                var indexModel = new CreateIndexModel<OutboxRecord>(Builders<OutboxRecord>.IndexKeys.Ascending(record => record.Dispatched), new CreateIndexOptions
                {
                    ExpireAfter = timeToKeepOutboxDeduplicationData,
                    Name = outboxCleanupIndexName,
                    Background = true
                });

                outboxCollection.Indexes.CreateOne(indexModel);
            }

            context.Services.AddSingleton<IOutboxStorage>(new OutboxPersister(client, databaseName, collectionNamingConvention));
        }

        const string outboxCleanupIndexName = "OutboxCleanup";
    }
}