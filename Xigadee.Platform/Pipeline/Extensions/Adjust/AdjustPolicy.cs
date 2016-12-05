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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xigadee
{
    public static partial class CorePipelineExtensions
    {
        public static P AdjustPolicyMicroservice<P>(this P pipeline
            , Action<MicroservicePolicy> msAssign = null) where P: MicroservicePipeline

        {
            msAssign?.Invoke(pipeline.Service.PolicyMicroservice);

            return pipeline;
        }

        public static P AdjustPolicyTaskManager<P>(this P pipeline
            , Action<TaskManagerPolicy> msAssign = null) where P: MicroservicePipeline
        {
            msAssign?.Invoke(pipeline.Service.PolicyTaskManager);

            return pipeline;
        }

        public static P AdjustPolicyResourceTracker<P>(this P pipeline
            , Action<ResourceTrackerPolicy> msAssign = null) where P : MicroservicePipeline
        {
            msAssign?.Invoke(pipeline.Service.PolicyResourceTracker);

            return pipeline;
        }

        public static P AdjustPolicyCommandContainer<P>(this P pipeline
            , Action<CommandContainerPolicy> msAssign = null) where P : MicroservicePipeline
        {
            msAssign?.Invoke(pipeline.Service.PolicyCommandContainer);

            return pipeline;
        }

        public static P AdjustPolicyCommunication<P>(this P pipeline
            , Action<CommunicationPolicy> msAssign = null) where P : MicroservicePipeline
        {
            msAssign?.Invoke(pipeline.Service.PolicyCommunication);

            return pipeline;
        }

        public static P AdjustPolicyScheduler<P>(this P pipeline
            , Action<SchedulerPolicy> msAssign = null) where P : MicroservicePipeline
        {
            msAssign?.Invoke(pipeline.Service.PolicyScheduler);

            return pipeline;
        }

        public static P AdjustPolicySecurity<P>(this P pipeline
            , Action<SecurityPolicy> msAssign = null) where P : MicroservicePipeline
        {
            msAssign?.Invoke(pipeline.Service.PolicySecurity);

            return pipeline;
        }

        public static P AdjustPolicyDataCollection<P>(this P pipeline
            , Action<DataCollectionPolicy> msAssign = null) where P : MicroservicePipeline
        {
            msAssign?.Invoke(pipeline.Service.PolicyDataCollection);

            return pipeline;
        }
    }
}