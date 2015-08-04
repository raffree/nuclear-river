﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Data.Context;
using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.DataMappers;
using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Mergers;
using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Metadata;
using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Operations;
using NuClear.AdvancedSearch.Replication.Data;
using NuClear.Telemetry.Probing;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming
{
    public sealed partial class CustomerIntelligenceTransformation
    {
        private readonly ICustomerIntelligenceContext _source;
        private readonly ICustomerIntelligenceContext _target;
        private readonly IDataMapper _mapper;
        private readonly ITransactionManager _transactionManager;

        public CustomerIntelligenceTransformation(ICustomerIntelligenceContext source, ICustomerIntelligenceContext target, IDataMapper mapper, ITransactionManager transactionManager)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            _source = source;
            _target = target;
            _mapper = mapper;
            _transactionManager = transactionManager;
        }


        public void Transform(IEnumerable<AggregateOperation> operations)
        {
            using (Probe.Create("ETL2 Transforming"))
            {
                _transactionManager.WithinTransaction(() => DoTransform(operations));
            }
        }

        private void DoTransform(IEnumerable<AggregateOperation> operations)
        {
            var slices = operations.GroupBy(x => new { Operation = x.GetType(), x.AggregateType, x.AggregateId })
                                   .OrderByDescending(x => x.Key.Operation, new AggregateOperationPriorityComparer());

            foreach (var slice in slices)
            {
                var operation = slice.Key.Operation;
                var aggregateType = slice.Key.AggregateType;
                var aggregateIds = slice.Select(x => x.AggregateId).ToList();

                IAggregateInfo aggregateInfo;
                if (!Aggregates.TryGetValue(aggregateType, out aggregateInfo))
                {
                    throw new NotSupportedException(string.Format("The '{0}' aggregate not supported.", aggregateType));
                }

                using (Probe.Create("ETL2 Transforming", aggregateType.Name))
                {
                    if (operation == typeof(InitializeAggregate))
                    {
                        InitializeAggregate(aggregateInfo, aggregateIds);
                        continue;
                    }

                    if (operation == typeof(RecalculateAggregate))
                    {
                        RecalculateAggregate(aggregateInfo, aggregateIds);
                        continue;
                    }

                    if (operation == typeof(DestroyAggregate))
                    {
                        DestroyAggregate(aggregateInfo, aggregateIds);
                        continue;
                    }
                }
            }
        }

        private void InitializeAggregate(IAggregateInfo aggregateInfo, IReadOnlyCollection<long> ids)
        {
            var sourceActualIds = aggregateInfo.QueryIdsByIds(_source, ids);
            var targetActualIds = aggregateInfo.QueryIdsByIds(_target, ids);
            var mergeResult = MergeTool.Merge(sourceActualIds, targetActualIds);

            var aggregateDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, aggregateInfo);
            aggregateDataMapper.Insert(aggregateInfo.QueryByIds(_source, mergeResult.Difference.ToList()));

            foreach (var valueObject in aggregateInfo.ValueObjects)
            {
                var sourceValueObjects = valueObject.QueryByParentIds(_source, ids);
                var targetValueObjects = valueObject.QueryByParentIds(_target, ids);

                var valueObjectMerger = MergerFactory.CreateValueObjectMerger(valueObject);
                var valueObjectMergeResult = valueObjectMerger.Merge(sourceValueObjects, targetValueObjects);

                var valueObjectDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, valueObject);
                valueObjectDataMapper.Insert(valueObjectMergeResult.Difference);
            }
        }

        private void RecalculateAggregate(IAggregateInfo aggregateInfo, IReadOnlyCollection<long> ids)
        {
            var sourceActualIds = aggregateInfo.QueryIdsByIds(_source, ids);
            var targetActualIds = aggregateInfo.QueryIdsByIds(_target, ids);
            var mergeResult = MergeTool.Merge(sourceActualIds, targetActualIds);

            foreach (var valueObject in aggregateInfo.ValueObjects)
            {
                var sourceValueObjects = valueObject.QueryByParentIds(_source, ids);
                var targetValueObjects = valueObject.QueryByParentIds(_target, ids);

                var valueObjectMerger = MergerFactory.CreateValueObjectMerger(valueObject);
                var valueObjectMergeResult = valueObjectMerger.Merge(sourceValueObjects, targetValueObjects);

                var valueObjectDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, valueObject);
                valueObjectDataMapper.Delete(valueObjectMergeResult.Complement);
                valueObjectDataMapper.Insert(valueObjectMergeResult.Difference);
                valueObjectDataMapper.Update(valueObjectMergeResult.Intersection);
            }

            var aggregateDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, aggregateInfo);
            aggregateDataMapper.Delete(aggregateInfo.QueryByIds(_target, mergeResult.Complement.ToList()));
            aggregateDataMapper.Insert(aggregateInfo.QueryByIds(_source, mergeResult.Difference.ToList()));
            aggregateDataMapper.Update(aggregateInfo.QueryByIds(_source, mergeResult.Intersection.ToList()));

            foreach (var entity in aggregateInfo.Entities)
            {
                var sourceEntityIds = entity.QueryIdsByParentIds(_source, ids);
                var targetEntityIds = entity.QueryIdsByParentIds(_target, ids);

                var entityDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, entity);
                var entityMergeResult = MergeTool.Merge(sourceEntityIds, targetEntityIds);

                entityDataMapper.Delete(entity.QueryByIds(_target, entityMergeResult.Complement.ToList()));
                entityDataMapper.Insert(entity.QueryByIds(_source, entityMergeResult.Difference.ToList()));
                entityDataMapper.Update(entity.QueryByIds(_source, entityMergeResult.Intersection.ToList()));
            }
        }

        private void DestroyAggregate(IAggregateInfo aggregateInfo, IReadOnlyCollection<long> ids)
        {
            var sourceActualIds = aggregateInfo.QueryIdsByIds(_source, ids);
            var targetActualIds = aggregateInfo.QueryIdsByIds(_target, ids);
            var mergeResult = MergeTool.Merge(sourceActualIds, targetActualIds);

            foreach (var entity in aggregateInfo.Entities)
            {
                var sourceEntityIds = entity.QueryIdsByParentIds(_source, ids);
                var targetEntityIds = entity.QueryIdsByParentIds(_target, ids);

                var entityDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, entity);
                var entityMergeResult = MergeTool.Merge(sourceEntityIds, targetEntityIds);

                entityDataMapper.Delete(entity.QueryByIds(_target, entityMergeResult.Complement.ToList()));
            }

            foreach (var valueObject in aggregateInfo.ValueObjects)
            {
                var sourceValueObjects = valueObject.QueryByParentIds(_source, ids);
                var targetValueObjects = valueObject.QueryByParentIds(_target, ids);

                var valueObjectMerger = MergerFactory.CreateValueObjectMerger(valueObject);
                var valueObjectMergeResult = valueObjectMerger.Merge(sourceValueObjects, targetValueObjects);

                var valueObjectDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, valueObject);
                valueObjectDataMapper.Delete(valueObjectMergeResult.Complement);
            }

            var aggregateDataMapper = DataMapperFactory.CreateTypedDataMapper(_mapper, aggregateInfo);
            aggregateDataMapper.Delete(aggregateInfo.QueryByIds(_target, mergeResult.Complement.ToList()));
        }
    }
}

