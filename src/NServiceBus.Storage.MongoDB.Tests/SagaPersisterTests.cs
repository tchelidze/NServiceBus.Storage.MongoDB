﻿namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Sagas;

    public class SagaPersisterTests<TSaga, TSagaData> : SagaPersisterTests
        where TSaga : Saga<TSagaData>, new()
        where TSagaData : class, IContainSagaData, new()
    {
        protected Task SaveSaga(TSagaData saga, params Type[] availableTypes) => SaveSaga<TSaga, TSagaData>(saga, availableTypes);
        protected Task<TSagaData> GetById(Guid sagaId, params Type[] availableTypes) => GetById<TSaga, TSagaData>(sagaId, availableTypes);
    }

    public class SagaPersisterTests
    {
        [OneTimeSetUp]
        public virtual async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration();
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public virtual async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        protected async Task SaveSaga<TSaga, TSagaData>(TSagaData saga, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var correlationProperty = SetActiveSagaInstanceForSave(insertContextBag, new TSaga(), saga, availableTypes);

                await configuration.SagaStorage.Save(saga, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }
        }

        protected async Task<TSagaData> GetById<TSaga, TSagaData>(Guid sagaId, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var readContextBag = configuration.GetContextBagForSagaStorage();
            TSagaData sagaData;
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstanceForGet<TSaga, TSagaData>(readContextBag, new TSagaData(), availableTypes);
                sagaData = await configuration.SagaStorage.Get<TSagaData>(sagaId, readSession, readContextBag);

                await readSession.CompleteAsync();
            }

            return sagaData;
        }

        protected SagaCorrelationProperty SetActiveSagaInstanceForSave<TSaga, TSagaData>(ContextBag context, TSaga saga, TSagaData sagaData, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>
            where TSagaData : class, IContainSagaData, new()
        {
            var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));
            var sagaInstance = new ActiveSagaInstance(saga, sagaMetadata, () => DateTime.UtcNow);
            var correlationProperty = SagaCorrelationProperty.None;
            if (sagaMetadata.TryGetCorrelationProperty(out var correlatedProp))
            {
                var prop = sagaData.GetType().GetProperty(correlatedProp.Name);

                var value = prop.GetValue(sagaData);

                correlationProperty = new SagaCorrelationProperty(correlatedProp.Name, value);
            }

            if (sagaData.Id == Guid.Empty)
            {
                sagaData.Id = configuration.SagaIdGenerator.Generate(new SagaIdGeneratorContext(correlationProperty, sagaMetadata, context));
            }

            sagaInstance.AttachNewEntity(sagaData);
            context.Set(sagaInstance);

            return correlationProperty;
        }

        protected void SetActiveSagaInstanceForGet<TSaga, TSagaData>(ContextBag context, TSagaData sagaData, params Type[] availableTypes)
            where TSaga : Saga<TSagaData>, new()
            where TSagaData : class, IContainSagaData, new()
        {
            var sagaMetadata = configuration.SagaMetadataCollection.FindByEntity(typeof(TSagaData));
            var sagaInstance = new ActiveSagaInstance(new TSaga(), sagaMetadata, () => DateTime.UtcNow);

            sagaInstance.AttachNewEntity(sagaData);
            context.Set(sagaInstance);
        }

        protected PersistenceTestsConfiguration configuration;
    }
}