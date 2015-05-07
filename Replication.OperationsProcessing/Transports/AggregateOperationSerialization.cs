﻿using System;
using System.Xml.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Operations;
using NuClear.Model.Common.Entities;
using NuClear.Replication.OperationsProcessing.Metadata.Model.Context;

namespace NuClear.Replication.OperationsProcessing.Transports
{
    public static class AggregateOperationSerialization
    {
        public static XElement ToXml(AggregateOperation operation)
        {
            var entityType = EntityTypeMap<CustomerIntelligenceContext>.AsEntityName(operation.AggregateType);

            var element = new XElement("operation");
            element.Add(new XAttribute("entity", operation.AggregateId));
            element.Add(new XAttribute("entitytype", entityType.Id));
            element.Add(new XAttribute("operation", operation.GetType().Name));

            return element;
        }

        public static AggregateOperation FromXml(XElement operation)
        {
            var operationType = GetOperationType(operation);
            var entityType = EntityType.Instance.Parse(int.Parse(operation.Attribute("entitytype").Value));
            var entity = long.Parse(operation.Attribute("entity").Value);

            return (AggregateOperation)Activator.CreateInstance(operationType,
                                                                EntityTypeMap<CustomerIntelligenceContext>.AsEntityType(entityType),
                                                                entity);
        }

        private static Type GetOperationType(XElement operation)
        {
            var operationType = operation.Attribute("operation").Value;
            switch (operationType)
            {
                case "DestroyAggregate":
                    return typeof(DestroyAggregate);
                case "InitializeAggregate":
                    return typeof(InitializeAggregate);
                case "RecalculateAggregate":
                    return typeof(RecalculateAggregate);
                default:
                    throw new ArgumentException(string.Format("{0} is not supported opertaion type", operationType), "operation");
            }
        }
    }
}