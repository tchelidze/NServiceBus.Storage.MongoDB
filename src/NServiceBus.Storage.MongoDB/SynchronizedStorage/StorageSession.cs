﻿using MongoDB.Bson;
using MongoDB.Driver;
using NServiceBus.Persistence;
using System;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB
{
    class StorageSession : CompletableSynchronizedStorageSession
    {
        public StorageSession(IMongoDatabase database)
        {
            this.database = database;
        }

        public IMongoCollection<BsonDocument> GetCollection(Type type) => database.GetCollection<BsonDocument>(GetCollectionName(type)).WithReadPreference(ReadPreference.Primary).WithWriteConcern(WriteConcern.WMajority);

        public Task CompleteAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        protected string GetCollectionName(Type entityType)
        {
            return entityType.Name.ToLower();
        }

        readonly IMongoDatabase database;
    }
}
