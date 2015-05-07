﻿using System.Collections.Generic;
using System.Linq;
using System.Transactions;

using LinqToDB;

using NuClear.Messaging.API;
using NuClear.Messaging.API.Flows.Metadata;
using NuClear.Messaging.API.Receivers;
using NuClear.Model.Common.Entities;
using NuClear.OperationsProcessing.Transports.SQLStore.Final;

namespace NuClear.Replication.OperationsProcessing.Transports.SQLStore
{
    public class SqlStoreReceiver : IMessageReceiver
    {
        private readonly MessageFlowMetadata _sourceFlowMetadata;
        private readonly IDataContext _context;

        public SqlStoreReceiver(MessageFlowMetadata sourceFlowMetadata, IDataContext context)
        {
            _sourceFlowMetadata = sourceFlowMetadata;
            _context = context;
        }

        public IReadOnlyList<IMessage> Peek()
        {
            IEnumerable<PerformedOperationFinalProcessing> flowRecords;
            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                flowRecords = _context.GetTable<PerformedOperationFinalProcessing>()
                    .Where(processing => processing.MessageFlowId == _sourceFlowMetadata.MessageFlow.Id)
                    .ToArray();
                scope.Complete();
            }

            return flowRecords.Any()
                       ? new[] { CreateMessage(flowRecords) }
                       : new PerformedOperationsFinalProcessingMessage[0];
        }

        public void Complete(IEnumerable<IMessage> successfullyProcessedMessages, IEnumerable<IMessage> failedProcessedMessages)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                Complete(successfullyProcessedMessages.Cast<PerformedOperationsFinalProcessingMessage>(),
                         failedProcessedMessages.Cast<PerformedOperationsFinalProcessingMessage>());

                scope.Complete();
            }
        }

        private PerformedOperationsFinalProcessingMessage CreateMessage(IEnumerable<PerformedOperationFinalProcessing> flowRecords)
        {
            return new PerformedOperationsFinalProcessingMessage
            {
                EntityId = 0,
                MaxAttemptCount = 0,
                EntityName = EntityType.Instance.None(),
                Flow = _sourceFlowMetadata.MessageFlow,
                FinalProcessings = flowRecords,
            };
        }

        private void Complete(IEnumerable<PerformedOperationsFinalProcessingMessage> successfullyProcessedMessages,
                              IEnumerable<PerformedOperationsFinalProcessingMessage> failedProcessedMessages)
        {
            foreach (var message in successfullyProcessedMessages.SelectMany(message => message.FinalProcessings))
            {
                _context.Delete(message);
            }

            // FIXME {all, 05.05.2015}: Что делать при ошибках во время обработки?
            // Считаю, что нельзя повторно вызывать обработку сообщений - будет нарушен порядок.
            // С другой стороны, если пропустить сообщение - тоже порядок нарушается. Стоит полностью останавливать репликацию?
            foreach (var message in failedProcessedMessages.SelectMany(message => message.FinalProcessings))
            {
                _context.Delete(message);
            }
        }
    }
}