﻿using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Persistence;

namespace NServiceBus.Storage.MongoDB
{
    class StorageSession : CompletableSynchronizedStorageSession
    {
        public StorageSession(IClientSessionHandle mongoSession, string databaseName, ContextBag contextBag, Func<Type, string> collectionNamingConvention, bool ownsMongoSession)
        {
            this.mongoSession = mongoSession;

            database = mongoSession.Client.GetDatabase(databaseName, new MongoDatabaseSettings
            {
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            });

            this.contextBag = contextBag;
            this.collectionNamingConvention = collectionNamingConvention;
            this.ownsMongoSession = ownsMongoSession;
        }

        public Task<string> IndexesCreateOneAsync(Type type, CreateIndexModel<BsonDocument> model) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).Indexes.CreateOneAsync(model);

        public Task InsertOneAsync<T>(T document) => database.GetCollection<T>(collectionNamingConvention(typeof(T))).InsertOneAsync(mongoSession, document);

        public Task InsertOneAsync(Type type, BsonDocument document) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).InsertOneAsync(mongoSession, document);

        public Task<UpdateResult> UpdateOneAsync(Type type, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).UpdateOneAsync(mongoSession, filter, update);

        public Task<DeleteResult> DeleteOneAsync(Type type, FilterDefinition<BsonDocument> filter) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).DeleteOneAsync(mongoSession, filter);

        public IFindFluent<BsonDocument, BsonDocument> Find(Type type, FilterDefinition<BsonDocument> filter) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).Find(filter);








        public IMongoCollection<T> GetCollection<T>(string name, MongoCollectionSettings settings = null) => database.GetCollection<T>(name, settings);





        public void StoreVersion(Type type, BsonValue version) => contextBag.Set(type.FullName, version);

        public BsonValue RetrieveVersion(Type type) => contextBag.Get<BsonValue>(type.FullName);

        public Task CompleteAsync()
        {
            if (ownsMongoSession)
            {
                return InternalCompleteAsync();
            }

            return TaskEx.CompletedTask;
        }

        internal Task InternalCompleteAsync()
        {
            if (mongoSession.IsInTransaction)
            {
                return mongoSession.CommitTransactionAsync();
            }

            return TaskEx.CompletedTask;
        }

        public void Dispose()
        {
            if (ownsMongoSession)
            {
                InternalDispose();
            }
        }

        internal void InternalDispose()
        {
            if (mongoSession.IsInTransaction)
            {
                try
                {
                    mongoSession.AbortTransaction();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception thrown while aborting transaction", ex);
                }
            }

            mongoSession.Dispose();
        }

        static readonly ILog Log = LogManager.GetLogger<StorageSession>();

        readonly IClientSessionHandle mongoSession;
        readonly IMongoDatabase database;
        readonly ContextBag contextBag;
        readonly Func<Type, string> collectionNamingConvention;
        readonly bool ownsMongoSession;
    }
}
