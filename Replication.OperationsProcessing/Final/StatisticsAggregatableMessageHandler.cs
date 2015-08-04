﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming;
using NuClear.Messaging.API.Processing;
using NuClear.Messaging.API.Processing.Actors.Handlers;
using NuClear.Messaging.API.Processing.Stages;
using NuClear.Replication.OperationsProcessing.Performance;
using NuClear.Telemetry;

namespace NuClear.Replication.OperationsProcessing.Final
{
    public sealed class StatisticsAggregatableMessageHandler : IMessageProcessingHandler
    {
        private readonly StatisticsFinalTransformation _statisticsTransformation;
        private readonly ITelemetryPublisher _telemetryPublisher;

        public StatisticsAggregatableMessageHandler(StatisticsFinalTransformation statisticsTransformation, ITelemetryPublisher telemetryPublisher)
        {
            _statisticsTransformation = statisticsTransformation;
            _telemetryPublisher = telemetryPublisher;
        }

        public IEnumerable<StageResult> Handle(IReadOnlyDictionary<Guid, List<IAggregatableMessage>> processingResultsMap)
        {
            return processingResultsMap.Select(pair => Handle(pair.Key, pair.Value)).ToArray();
        }

        private StageResult Handle(Guid bucketId, IEnumerable<IAggregatableMessage> messages)
        {
            try
            {
                var operations = messages.OfType<StatisticsAggregatableMessage>().Single().Operations;

                _statisticsTransformation.Recalculate(operations);
                _telemetryPublisher.Publish<StatisticsProcessedOperationCountIdentity>(operations.Count());

                return MessageProcessingStage.Handling.ResultFor(bucketId).AsSucceeded();
            }
            catch (Exception ex)
            {
                return MessageProcessingStage.Handling.ResultFor(bucketId).AsFailed().WithExceptions(ex);
            }
        }
    }
}