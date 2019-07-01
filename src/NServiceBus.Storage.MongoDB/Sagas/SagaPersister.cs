﻿using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

namespace NServiceBus.Storage.MongoDB
{
    class SagaPersister : ISagaPersister
    {
        public SagaPersister(string versionElementName)
        {
            this.versionElementName = versionElementName;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();

            var document = sagaData.ToBsonDocument();
            document.Add(versionElementName, 0);

            await storageSession.InsertOneAsync(sagaDataType, document).ConfigureAwait(false);
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();

            var version = storageSession.RetrieveVersion(sagaDataType);

            var update = Builders<BsonDocument>.Update
                .Inc(versionElementName, 1);

            var document = sagaData.ToBsonDocument();

            foreach (var element in document)
            {
                if (element.Name != versionElementName && element.Name != idElementName)
                {
                    update = update.Set(element.Name, element.Value);
                }
            }

            var result = await storageSession.UpdateOneAsync(sagaDataType, filterBuilder.Eq(idElementName, sagaData.Id) & filterBuilder.Eq(versionElementName, version), update).ConfigureAwait(false);

            if (result.ModifiedCount != 1)
            {
                throw new Exception($"The '{sagaDataType.Name}' saga with id '{sagaData.Id}' was updated by another process or no longer exists.");
            }
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData => GetSagaData<TSagaData>(typeof(TSagaData), idElementName, sagaId, session);

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var sagaDataType = typeof(TSagaData);
            var propertyElementName = sagaDataType.GetElementName(propertyName);

            return GetSagaData<TSagaData>(sagaDataType, propertyElementName, propertyValue, session);
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();

            var version = storageSession.RetrieveVersion(sagaDataType);

            var result = await storageSession.DeleteOneAsync(sagaDataType, filterBuilder.Eq(idElementName, sagaData.Id) & filterBuilder.Eq(versionElementName, version)).ConfigureAwait(false);

            if (result.DeletedCount != 1)
            {
                throw new Exception($"Saga can't be completed because it was updated by another process.");
            }
        }

        async Task<TSagaData> GetSagaData<TSagaData>(Type sagaDataType, string elementName, object elementValue, SynchronizedStorageSession session)
        {
            var storageSession = (StorageSession)session;

            var document = await storageSession.Find(sagaDataType, new BsonDocument(elementName, BsonValue.Create(elementValue))).SingleOrDefaultAsync().ConfigureAwait(false);

            if (document != null)
            {
                var version = document.GetValue(versionElementName);
                document.Remove(versionElementName);
                storageSession.StoreVersion(sagaDataType, version);

                return BsonSerializer.Deserialize<TSagaData>(document);
            }

            return default;
        }

        const string idElementName = "_id";
        readonly string versionElementName;
        readonly FilterDefinitionBuilder<BsonDocument> filterBuilder = Builders<BsonDocument>.Filter;
    }
}