﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;

namespace NuClear.Replication.Core.Actors
{
    public sealed class LeafActor<TDataObject> : DataObjectsActorBase<TDataObject> where TDataObject : class
    {
        private readonly IBulkRepository<TDataObject> _bulkRepository;
        private readonly IEqualityComparerFactory _equalityComparerFactory;
        private readonly IDataChangesHandler<TDataObject> _dataChangesHandler;

        public LeafActor(
            IQuery query,
            IBulkRepository<TDataObject> bulkRepository,
            IEqualityComparerFactory equalityComparerFactory,
            IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor,
            IDataChangesHandler<TDataObject> dataChangesHandler)
            : base(query, storageBasedDataObjectAccessor)
        {
            _bulkRepository = bulkRepository;
            _equalityComparerFactory = equalityComparerFactory;
            _dataChangesHandler = dataChangesHandler;
        }

        public LeafActor(
           IQuery query,
           IBulkRepository<TDataObject> bulkRepository,
           IEqualityComparerFactory equalityComparerFactory,
           IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor)
            : this(query, bulkRepository, equalityComparerFactory, storageBasedDataObjectAccessor, new NullDataChangesHandler<TDataObject>())
        {
        }

        public override IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (!commands.Any())
            {
                return Array.Empty<IEvent>();
            }

            var events = new List<IEvent>();

            var changes = DetectChanges(commands, _equalityComparerFactory.CreateCompleteComparer<TDataObject>());

            var toCreate = changes.Difference.ToArray();
            var toDelete = changes.Complement.ToArray();

            _bulkRepository.Delete(toDelete);
            events.AddRange(_dataChangesHandler.HandleDeletes(toDelete));

            _bulkRepository.Create(toCreate);
            events.AddRange(_dataChangesHandler.HandleCreates(toCreate));

            return events;
        }
    }
}