﻿namespace NServiceBus.Storage.MongoDB
{
    static class MongoPersistenceConstants
    {
        public const string SubscriptionCollectionName = "subscriptions";
        public const string DeduplicationCollectionName = "deduplication";
        public const string SagaUniqueIdentityCollectionName = "saga_unique_ids";
    }
}