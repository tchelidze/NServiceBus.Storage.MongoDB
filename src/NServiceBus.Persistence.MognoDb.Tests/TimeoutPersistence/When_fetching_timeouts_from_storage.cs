﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Persistence.MognoDb.Tests.SubscriptionPersistence;
using NServiceBus.Persistence.MongoDB.Timeout;
using NServiceBus.Support;
using NServiceBus.Timeout.Core;
using NUnit.Framework;

namespace NServiceBus.Persistence.MognoDb.Tests.TimeoutPersistence
{
    [TestFixture]
    public class When_fetching_timeouts_from_storage : MongoFixture
    {
        [Test]
        public void Should_persist_with_common_nservicebus_headers()
        {
            var nextTime = DateTime.UtcNow.AddHours(1);

            Storage.Add(new TimeoutData
            {
                Time = nextTime,
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { Headers.MessageId, Guid.NewGuid().ToString() }, { Headers.NServiceBusVersion, Guid.NewGuid().ToString() }, { Headers.OriginatingAddress, Guid.NewGuid().ToString() } },
                OwningTimeoutManager = "MyTestEndpoint",
            });
        }

        [Test]
        public void Should_return_the_complete_list_of_timeouts()
        {
            const int numberOfTimeoutsToAdd = 10;

            for (var i = 0; i < numberOfTimeoutsToAdd; i++)
            {
                Storage.Add(new TimeoutData
                {
                    Time = DateTime.UtcNow.AddHours(-1),
                    Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                    SagaId = Guid.NewGuid(),
                    State = new byte[] { 0, 0, 133 },
                    Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                    OwningTimeoutManager = "MyTestEndpoint",
                });
            }
            
            DateTime nextTimeToRunQuery;
            Assert.AreEqual(numberOfTimeoutsToAdd, Storage.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery).Count());
        }

        [Test]
        public void Should_return_the_next_time_of_retrieval()
        {
            var nextTime = DateTime.UtcNow.AddHours(1);

            Storage.Add(new TimeoutData
            {
                Time = nextTime,
                Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[] { 0, 0, 133 },
                Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                OwningTimeoutManager = "MyTestEndpoint",
            });

            DateTime nextTimeToRunQuery;
            Storage.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

            Assert.IsTrue((nextTime - nextTimeToRunQuery).TotalSeconds < 1);
        }
    }
}
