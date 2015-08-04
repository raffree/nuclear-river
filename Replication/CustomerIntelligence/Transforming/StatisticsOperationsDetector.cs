﻿using System.Collections.Generic;
using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Operations;
using NuClear.Storage.Readings;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming
{
    public class StatisticsOperationsDetector
    {
        private readonly ErmFactInfo _factInfo;
        private readonly IQuery _query;

        public StatisticsOperationsDetector(ErmFactInfo factInfo, IQuery query)
        {
            _factInfo = factInfo;
            _query = query;
        }

        public IReadOnlyCollection<CalculateStatisticsOperation> DetectOperations(IReadOnlyCollection<long> factIds)
        {
            if (_factInfo.CalculateStatisticsSpecProvider == null)
            {
                return new List<CalculateStatisticsOperation>();
            }

            var mapSpec = _factInfo.CalculateStatisticsSpecProvider(factIds);
            return mapSpec.Map(_query).ToList();
        }
    }
}