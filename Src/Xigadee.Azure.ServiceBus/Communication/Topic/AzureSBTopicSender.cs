﻿#region Copyright
// Copyright Hitachi Consulting
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

#region using
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
#endregion
namespace Xigadee
{
    [DebuggerDisplay("AzureSBTopicSender: {ChannelId}")]
    public class AzureServiceBusTopicSender : AzureSBSenderBase<TopicClient, Microsoft.Azure.ServiceBus.Message>
    {

        protected override AzureClientHolder<TopicClient, Microsoft.Azure.ServiceBus.Message> ClientCreate(SenderPartitionConfig partition)
        {
            var client =  base.ClientCreate(partition);

            client.Type = "Topic Sender";

            client.Name = mPriorityClientNamer(Connection.EntityName, partition.Priority);

            client.AssignMessageHelpers();

            //client.FabricInitialize = () => Connection.TopicFabricInitialize(client.Name);

            //client.ClientCreate = () => TopicClient.CreateFromConnectionString(Connection.ConnectionString, client.Name);

            client.MessageTransmit = async (b) => await client.Client.SendAsync(b);

            return client;
        }
    }
}
