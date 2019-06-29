﻿using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NServiceBus.Persistence.ComponentTests
{
    public class When_persisting_a_saga_entity : SagaPersisterTests<PropertyTypesTestSaga, PropertyTypesTestSagaData>
    {
        PropertyTypesTestSagaData entity;
        PropertyTypesTestSagaData savedEntity;
        RelatedClass relatedClass;

        [SetUp]
        public async Task Setup()
        {
            entity = new PropertyTypesTestSagaData
            {
                Id = Guid.NewGuid(),
                TestComponent = new TestComponent {Property = "Prop"},
                DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01").ToUniversalTime(),
                Status = StatusEnum.AnotherStatus,
                PolymorphicRelatedProperty = new PolymorphicProperty { SomeInt = 9 },
                RelatedClass = new RelatedClass { Id = Guid.NewGuid() }
            };

            relatedClass = entity.RelatedClass;

            await SaveSaga(entity).ConfigureAwait(false);

            savedEntity = await GetById(entity.Id).ConfigureAwait(false);
        }

        [Test]
        public void Public_setters_and_getters_of_concrete_classes_should_be_persisted()
        {
            Assert.AreEqual(entity.TestComponent, savedEntity.TestComponent);
        }

        [Test]
        public void Datetime_properties_should_be_persisted()
        {
            Assert.AreEqual(entity.DateTimeProperty, savedEntity.DateTimeProperty);
        }

        [Test]
        public void Enums_should_be_persisted()
        {
            Assert.AreEqual(entity.Status, savedEntity.Status);
        }

        [Test]
        public void Inherited_property_classes_should_be_persisted()
        {
            Assert.AreEqual(entity.PolymorphicRelatedProperty, savedEntity.PolymorphicRelatedProperty);
        }

        [Test]
        public void Related_entities_should_be_persisted()
        {
            Assert.AreEqual(relatedClass.Id, savedEntity.RelatedClass.Id);
        }
    }
}