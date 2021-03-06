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

namespace Xigadee
{
    public static partial class AzureExtensionMethods
    {
        /// <summary>
        /// This is the Event Hub key type value.
        /// </summary>
        [ConfigSettingKey("EventHubs")]
        public const string KeyEventHubsConnection = "EventHubsConnection";
        /// <summary>
        /// This is the Event Hub connection
        /// </summary>
        /// <param name="config">The Microservice configuration.</param>
        /// <returns>Returns the connection string.</returns>
        [ConfigSetting("EventHubs")]
        public static string EventHubsConnection(this IEnvironmentConfiguration config) => config.PlatformOrConfigCache(KeyEventHubsConnection);

        #region EventHubConnectionValidate(this IEnvironmentConfiguration Configuration, string serviceBusConnection)
        /// <summary>
        /// This method validates that the Event Hub connection is set.
        /// </summary>
        /// <param name="Configuration">The configuration.</param>
        /// <param name="eventHubsConnection">The alternate connection.</param>
        /// <returns>Returns the connection from either the parameter or from the settings.</returns>
        private static string EventHubsConnectionValidate(this IEnvironmentConfiguration Configuration, string eventHubsConnection)
        {
            var conn = eventHubsConnection ?? Configuration.EventHubsConnection();

            if (string.IsNullOrEmpty(conn))
                throw new AzureConnectionException(KeyEventHubsConnection);

            return conn;
        }
        #endregion

        /// <summary>
        /// This extension allows the Event Hub connection values to be manually set as override parameters.
        /// </summary>
        /// <param name="pipeline">The incoming pipeline.</param>
        /// <param name="connection">The Event Hub connection.</param>
        /// <returns>The pass-through of the pipeline.</returns>
        public static P ConfigOverrideSetEventHubsConnection<P>(this P pipeline, string connection)
            where P : IPipeline
        {
            pipeline.ConfigurationOverrideSet(KeyEventHubsConnection, connection);
            return pipeline;
        }
    }
}
