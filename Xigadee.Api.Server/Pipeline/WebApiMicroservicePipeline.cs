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
using System.Web.Http;

namespace Xigadee
{
    /// <summary>
    /// This extension pipeline is used by the Web Api pipeline.
    /// </summary>
    public class WebApiMicroservicePipeline: MicroservicePipeline
    {
        #region Declarations
        /// <summary>
        /// Eat me!
        /// </summary>
        protected MicroservicePipeline mInnerPipeline;
        #endregion
        #region Constructor

        public WebApiMicroservicePipeline(MicroservicePipeline pipeline, HttpConfiguration httpConfig = null)
        {
            if (pipeline == null)
                throw new ArgumentNullException("pipeline", "pipeline cannot be null");

            Service = pipeline.Service;
            Configuration = pipeline.Configuration;
            HttpConfig = httpConfig ?? new HttpConfiguration();
        }

        /// <summary>
        /// The default configuration.
        /// </summary>
        /// <param name="service">The microservice.</param>
        /// <param name="config">The microservice config.</param>
        /// <param name="httpConfig">The http configuration.</param>
        public WebApiMicroservicePipeline(IMicroservice service
            , IEnvironmentConfiguration config
            , HttpConfiguration httpConfig = null):base(service, config)
        {
            Service = service;
            Configuration = config;
            HttpConfig = httpConfig ?? new HttpConfiguration();
        }

        #endregion

        #region HttpConfig
        /// <summary>
        /// This is the http configuration class used for the Web Api instance.
        /// </summary>
        public HttpConfiguration HttpConfig { get; protected set; }
        #endregion
    }
}
