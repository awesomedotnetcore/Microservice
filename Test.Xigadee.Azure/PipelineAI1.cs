﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xigadee;

namespace Test.Xigadee.Azure
{
    [TestClass]
    public class PipelineAzure1
    {
        DebugMemoryDataCollector mDataCollector;

        private void ConfigureServiceRoot<P>(P pipe) where P: MicroservicePipeline
        {
            pipe
                .AddDataCollector<DebugMemoryDataCollector>((c) => mDataCollector = c)
                .AddLogger<TraceEventLogger>()
                .AddPayloadSerializerDefaultJson();
        }

        private void ChannelInConfigure(ChannelPipelineIncoming inPipe)
        {
            inPipe
                .AttachResourceProfile("TrackIt")
                //.AppendBoundaryLogger(new MemoryBoundaryLogger(), (p, bl) => bLogger = bl)
                ;
        }

        [TestMethod]
        public void PipelineAI1()
        {
            try
            {
                var pipeline = new MicroservicePipeline("TestPipeline");

                ChannelPipelineIncoming cpipeIn = null;
                ChannelPipelineOutgoing cpipeOut = null;
                PersistenceSharedService<Guid, Blah> persistence = null;
                PersistenceBlahMemory persistBlah = null;
                int signalChange = 0;

                pipeline
                    .AdjustPolicyTaskManager((t) =>
                    {
                        t.ConcurrentRequestsMin = 1;
                        t.ConcurrentRequestsMax = 4;
                    })
                    .AddCallOut(ConfigureServiceRoot)
                    .AddChannelIncoming("internalIn", internalOnly: true)
                        .AttachCallOut(ChannelInConfigure)
                        .AttachCommand(new PersistenceBlahMemory(), assign:(p) => persistBlah = p)
                        .AttachCommand(new PersistenceSharedService<Guid, Blah>(), assign:(c) => persistence = c, channelResponse: cpipeOut)
                        .Revert((c) => cpipeIn = c)
                    .AddChannelOutgoing("internalOut", internalOnly: true)
                        .Revert((c) => cpipeOut = c);

                persistBlah.OnEntityChangeAction += ((o, e) => { signalChange++; });

                pipeline.Start();


                Guid cId = Guid.NewGuid();
                var blah = new Blah { ContentId = cId, Message = "Hello", VersionId = Guid.NewGuid() };
                var result = persistence.Create(blah).Result;
                Assert.IsTrue(result.IsSuccess);

                var result2 = persistence.Read(cId).Result;
                Assert.IsTrue(result2.IsSuccess);

                blah.VersionId = Guid.NewGuid();
                var result3 = persistence.Update(blah).Result;
                Assert.IsTrue(result3.IsSuccess);

                var result4 = persistence.Delete(blah.ContentId).Result;
                Assert.IsTrue(result4.IsSuccess);


                Assert.IsTrue(signalChange == 3);
                pipeline.Stop();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

        }
    }
}