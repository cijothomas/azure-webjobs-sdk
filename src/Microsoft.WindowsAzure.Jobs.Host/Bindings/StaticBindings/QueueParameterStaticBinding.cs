﻿using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.WindowsAzure.Jobs
{
    internal class QueueParameterStaticBinding : ParameterStaticBinding
    {
        // Is this enqueue or dequeue?
        public bool IsInput { get; set; }

        // Route params produced from this queue. 
        // This likely corresponds to simply properties on the QueueInput Parameter type.
        public string[] Params { get; set; }

        [JsonIgnore]
        private string _queueName;

        public string QueueName
        {
            get
            {
                return _queueName;
            }
            set
            {
                string name = value.ToLowerInvariant(); // must be lowercase. coerce here to be nice.
                QueueClient.ValidateQueueName(name);
                this._queueName = name;
            }
        }

        public override IEnumerable<string> ProducedRouteParameters
        {
            get
            {
                return Params ?? new string[0];
            }
        }

        public override ParameterRuntimeBinding Bind(IRuntimeBindingInputs inputs)
        {
            string invokeString = null;
            if (this.IsInput)
            {
                ITriggerNewQueueMessage trigger = inputs as ITriggerNewQueueMessage;

                if (trigger == null)
                {
                    throw new InvalidOperationException("Direct calls are not supported for QueueInput methods.");
                }

                invokeString = trigger.QueueMessageInput.AsString;
            }
            return this.BindFromInvokeString(inputs, invokeString);
        }

        public override ParameterRuntimeBinding BindFromInvokeString(IRuntimeBindingInputs inputs, string invokeString)
        {
            if (this.IsInput)
            {
                return new QueueInputParameterRuntimeBinding { Content = invokeString };
            }
            else
            {
                // invokeString is ignored. 
                // Will set on out parameter.
                return new QueueOutputParameterRuntimeBinding
                {
                    QueueOutput = new CloudQueueDescriptor
                    {
                        AccountConnectionString = inputs.AccountConnectionString,
                        QueueName = this.QueueName
                    }
                };
            }
        }

        public override string Description
        {
            get
            {
                if (this.IsInput)
                {
                    return string.Format("dequeue from '{0}'", this.QueueName);
                }
                else
                {
                    return string.Format("enqueue to '{0}'", this.QueueName);
                }
            }
        }
    }
}