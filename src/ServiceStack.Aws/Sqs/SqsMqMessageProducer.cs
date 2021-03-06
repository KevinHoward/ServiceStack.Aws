﻿using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqMessageProducer : IMessageProducer, IOneWayClient
    {
        protected static readonly ILog log = LogManager.GetLogger(typeof(SqsMqMessageProducer));

        protected readonly ISqsMqBufferFactory sqsMqBufferFactory;
        protected readonly SqsQueueManager sqsQueueManager;

        public SqsMqMessageProducer(ISqsMqBufferFactory sqsMqBufferFactory, SqsQueueManager sqsQueueManager)
        {
            this.sqsMqBufferFactory = sqsMqBufferFactory;
            this.sqsQueueManager = sqsQueueManager;
        }

        public Action OnPublishedCallback { get; set; }

        public void Publish<T>(T messageBody)
        {
            var message = messageBody as IMessage;

            if (message != null)
            {
                Publish(message.ToInQueueName(), message);
            }
            else
            {
                Publish(new Message<T>(messageBody));
            }
        }

        public void Publish<T>(IMessage<T> message)
        {
            Publish(message.ToInQueueName(), message);
        }

        public void SendOneWay(object requestDto)
        {
            Publish(MessageFactory.Create(requestDto));
        }

        public void SendOneWay(string queueName, object requestDto)
        {
            Publish(queueName, MessageFactory.Create(requestDto));
        }

        public void SendAllOneWay(IEnumerable<object> requests)
        {
            if (requests == null)
                return;

            foreach (var request in requests)
            {
                SendOneWay(request);
            }
        }

        public void Publish(string queueName, IMessage message)
        {
            var queueDefinition = sqsQueueManager.GetOrCreate(queueName);
            var sqsBuffer = sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.Send(CreateSendMessageRequest(message, queueDefinition));

            if (OnPublishedCallback != null)
            {
                OnPublishedCallback();
            }
        }

        public SendMessageRequest CreateSendMessageRequest(IMessage message, string queueName)
        {
            var queueDefinition = sqsQueueManager.GetOrCreate(queueName);
            return CreateSendMessageRequest(message, queueDefinition);
        }

        public static SendMessageRequest CreateSendMessageRequest(IMessage message, SqsQueueDefinition queueDefinition)
        {
            using (AwsClientUtils.__requestAccess())
            {
                return new SendMessageRequest
                {
                    QueueUrl = queueDefinition.QueueUrl,
                    MessageBody = AwsClientUtils.ToJson(message)
                };
            }
        }

        public void Publish(string queueName, SendMessageRequest msgRequest)
        {
            var queueDefinition = sqsQueueManager.GetOrCreate(queueName);
            var sqsBuffer = sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.Send(msgRequest);

            if (OnPublishedCallback != null)
            {
                OnPublishedCallback();
            }
        }

        public void Dispose()
        {
            // NOTE: Do not dispose the bufferFactory or queueManager here, this object didn't create them, it was given them
        }
    }
}