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
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Xigadee;
#endregion
namespace Xigadee
{
    public abstract partial class CommandBase<S, P, H>
    {
        #region Events
        /// <summary>
        /// This event can is fired when the state of the master job is changed.
        /// </summary>
        public event EventHandler<MasterJobStateChangeEventArgs> OnMasterJobStateChange;
        /// <summary>
        /// This event is fired when the masterjob receives or issues communication requests to other masterjobs.
        /// </summary>
        public event EventHandler<MasterJobCommunicationEventArgs> OnMasterJobCommunication;
        #endregion
        #region Declarations
        /// <summary>
        /// This is the context that 
        /// </summary>
        protected MasterJobContext mMasterJobContext;

        protected Random mRandom = new Random(Environment.TickCount);
        #endregion

        #region MasterJobTearUp()
        /// <summary>
        /// This method sets up the master job.
        /// </summary>
        protected virtual void MasterJobTearUp()
        {
            if (mPolicy.MasterJobNegotiationChannelIdIncoming == null)
                throw new CommandStartupException("Masterjobs are enabled, but the NegotiationChannelIdIncoming has not been set");
            if (mPolicy.MasterJobNegotiationChannelType == null)
                throw new CommandStartupException("Masterjobs are enabled, but the NegotiationChannelType has not been set");

            mMasterJobContext = new MasterJobContext();

            mMasterJobContext.OnMasterJobStateChange += (object o, MasterJobStateChangeEventArgs s) =>
            {
                s.ServiceName = OriginatorId.Name;
                s.CommandName = GetType().Name;
                OnMasterJobStateChange?.Invoke(this, s);
            };

            CommandRegister(MasterJobNegotiationChannelIdIncoming, mPolicy.MasterJobNegotiationChannelType, null, MasterJobStateNotificationIncoming);

            mMasterJobContext.State = MasterJobState.VerifyingComms;

            //Register the schedule used for poll requests.
            var masterjobPoll = new Schedule(MasterJobStateNotificationOutgoing, $"MasterJob: {mPolicy.MasterJobName ?? FriendlyName}");

            masterjobPoll.Frequency = mPolicy.MasterJobPollFrequency ?? TimeSpan.FromSeconds(20);
            masterjobPoll.InitialWait = mPolicy.MasterJobPollInitialWait ?? TimeSpan.FromSeconds(5);
            masterjobPoll.IsLongRunning = false;

            SchedulerRegister(masterjobPoll);
        }
        #endregion
        #region MasterJobTearDown()
        /// <summary>
        /// This method stops and running master job processes.
        /// </summary>
        public virtual void MasterJobTearDown()
        {
            if (mMasterJobContext.State == MasterJobState.Active)
                MasterJobStop();
        } 
        #endregion

        #region NegotiationTransmit(string action)
        /// <summary>
        /// This method transmits the notification message to the other instances.
        /// The message is specified to be processed externally so to ensure that we can determine whether 
        /// the comms are active.
        /// </summary>
        /// <param name="action">The master job action to transmit.</param>
        protected virtual Task NegotiationTransmit(string action)
        {
            var payload = TransmissionPayload.Create(mPolicy.TransmissionPayloadTraceEnabled);
            payload.TraceWrite("Create", "Command/NegotiationTransmit");

            payload.Options = ProcessOptions.RouteExternal;

            var message = payload.Message;
            //Note: historically there was only one channel, so we use the incoming channel if the outgoing has
            //not been specified.
            message.ChannelId = mPolicy.MasterJobNegotiationChannelIdOutgoing ?? mPolicy.MasterJobNegotiationChannelIdIncoming;
            message.MessageType = mPolicy.MasterJobNegotiationChannelType;
            message.ActionType = action;

            message.ChannelPriority = mPolicy.MasterJobNegotiationChannelPriority;

            //Go straight to the dispatcher as we don't want to use the tracker for this job
            //as it is transmit only. Only send messages if the service is in a running state.
            switch (Status)
            {
                case ServiceStatus.Running:
                case ServiceStatus.Stopping:
                    TaskManager(this, null, payload);
                    try
                    {
                        OnMasterJobCommunication?.Invoke(this, new MasterJobCommunicationEventArgs(
                            OriginatorId.Name, GetType().Name, MasterJobCommunicationDirection.Outgoing, mMasterJobContext.State, action, mMasterJobContext.StateChangeCounter));
                    }
                    catch (Exception ex)
                    {
                        Collector?.LogException("OnMasterJobCommunication outgoing unexpected event error.", ex);
                        payload.TraceWrite($"Error: {ex.Message}", "Command/NegotiationTransmit/OnMasterJobCommunication");
                    }
                    break;
            }

            return Task.FromResult(0);
        }
        #endregion

        #region --> MasterJobStateNotificationIncoming(TransmissionPayload rq, List<TransmissionPayload> rs)
        /// <summary>
        /// This method processes state notifications from other instances of the MasterJob.
        /// </summary>
        /// <param name="rq">The request.</param>
        /// <param name="rs">The responses - this is not used.</param>
        protected virtual async Task MasterJobStateNotificationIncoming(TransmissionPayload rq, List<TransmissionPayload> rs)
        {
            mMasterJobContext.MessageLastIn = DateTime.UtcNow;
            rq.SignalSuccess();

            //If we are not active then do nothing.
            if (mMasterJobContext.State == MasterJobState.Disabled)
                return;

            //Filter out any messages sent from this service.
            if (string.Equals(rq.Message.OriginatorServiceId, OriginatorId.ExternalServiceId, StringComparison.InvariantCultureIgnoreCase))
            {
                //We can now say that the masterjob channel is working, so we can now enable the job for negotiation.
                if (mMasterJobContext.State == MasterJobState.VerifyingComms)
                    mMasterJobContext.State = MasterJobState.Starting;

                return;
            }

            //Raise an event for the incoming communication.
            OnMasterJobCommunication?.Invoke(this, new MasterJobCommunicationEventArgs(
                OriginatorId.Name, GetType().Name, MasterJobCommunicationDirection.Incoming, mMasterJobContext.State, rq.Message.ActionType, mMasterJobContext.StateChangeCounter
                , rq.Message.OriginatorServiceId));


            if (MasterJobStates.IAmStandby.Equals(rq.Message.ActionType, StringComparison.InvariantCultureIgnoreCase))
            {
                mMasterJobContext.PartnerAdd(rq.Message.OriginatorServiceId, true);
            }
            else if (MasterJobStates.IAmMaster.Equals(rq.Message.ActionType, StringComparison.InvariantCultureIgnoreCase))
            {
                if (mMasterJobContext.State == MasterJobState.Active)
                    MasterJobStop();

                mMasterJobContext.PartnerAdd(rq.Message.OriginatorServiceId, false);

                mMasterJobContext.State = MasterJobState.Inactive;
                mMasterJobContext.MasterRecordSet(rq.Message.OriginatorServiceId);
                mMasterJobContext.MasterPollAttemptsReset();
                await NegotiationTransmit(MasterJobStates.IAmStandby);
            }
            else if (MasterJobStates.ResyncMaster.Equals(rq.Message.ActionType, StringComparison.InvariantCultureIgnoreCase))
            {
                mMasterJobContext.State = MasterJobState.Starting;
                mMasterJobContext.MasterRecordClear();
            }
            else if (MasterJobStates.WhoIsMaster.Equals(rq.Message.ActionType, StringComparison.InvariantCultureIgnoreCase))
            {
                if (mMasterJobContext.State == MasterJobState.Active)
                    await MasterJobSyncIAmMaster();
            }
            else if (MasterJobStates.RequestingControl1.Equals(rq.Message.ActionType, StringComparison.InvariantCultureIgnoreCase))
            {
                if (mMasterJobContext.State == MasterJobState.Active)
                    await MasterJobSyncIAmMaster();
                else if (mMasterJobContext.State <= MasterJobState.Requesting1)
                    mMasterJobContext.State = MasterJobState.Inactive;
            }
            else if (MasterJobStates.RequestingControl2.Equals(rq.Message.ActionType, StringComparison.InvariantCultureIgnoreCase))
            {
                if (mMasterJobContext.State == MasterJobState.Active)
                    await MasterJobSyncIAmMaster();
                else if (mMasterJobContext.State <= MasterJobState.Requesting2)
                    mMasterJobContext.State = MasterJobState.Inactive;
            }
            else if (MasterJobStates.TakingControl.Equals(rq.Message.ActionType, StringComparison.InvariantCultureIgnoreCase))
            {
                if (mMasterJobContext.State == MasterJobState.Active)
                    await MasterJobSyncIAmMaster();
                else if (mMasterJobContext.State <= MasterJobState.TakingControl)
                    mMasterJobContext.State = MasterJobState.Inactive;
            }
            else if (!string.IsNullOrEmpty(rq.Message.ActionType))
            {
                Collector?.LogMessage(LoggingLevel.Warning, $"{rq.Message.ActionType} is not a valid negotiating action type for master job {FriendlyName}", "MasterJob");
            }
        }
        #endregion
        #region POLL: MasterJobStateNotificationOutgoing(Schedule state, CancellationToken token) -->
        /// <summary>
        /// This is the master job outgoing negotiation logic.
        /// </summary>
        /// <param name="schedule">The current schedule object.</param>
        /// <param name="token">The cancellation token</param>
        protected virtual async Task MasterJobStateNotificationOutgoing(Schedule schedule, CancellationToken token)
        {
            //We set a random poll time to reduce the potential for deadlocking
            //and to make the negotiation messaging more efficient.
            schedule.Frequency = TimeSpan.FromSeconds(5 + mRandom.Next(10));

            switch (mMasterJobContext.State)
            {
                case MasterJobState.VerifyingComms:
                    await NegotiationTransmit(MasterJobStates.WhoIsMaster);
                    break;
                case MasterJobState.Starting:
                    await NegotiationTransmit(MasterJobStates.WhoIsMaster);
                    mMasterJobContext.State = MasterJobState.Requesting1;
                    mMasterJobContext.MasterPollAttemptsIncrement();
                    break;
                case MasterJobState.Inactive:
                    await NegotiationTransmit(MasterJobStates.WhoIsMaster);
                    if (mMasterJobContext.MasterPollAttemptsExceeded())
                        mMasterJobContext.State = MasterJobState.Starting;
                    else if (mMasterJobContext.MasterPollAttempts == 0)
                        schedule.Frequency = TimeSpan.FromSeconds(10 + mRandom.Next(60));
                    mMasterJobContext.MasterPollAttemptsIncrement();
                    break;
                case MasterJobState.Requesting1:
                    await NegotiationTransmit(MasterJobStates.RequestingControl1);
                    mMasterJobContext.State = MasterJobState.Requesting2;
                    break;
                case MasterJobState.Requesting2:
                    await NegotiationTransmit(MasterJobStates.RequestingControl2);
                    mMasterJobContext.State = MasterJobState.TakingControl;
                    break;
                case MasterJobState.TakingControl:
                    await NegotiationTransmit(MasterJobStates.TakingControl);
                    MasterJobStart();
                    break;
                case MasterJobState.Active:
                    await MasterJobSyncIAmMaster();
                    schedule.Frequency = TimeSpan.FromSeconds(5 + mRandom.Next(25));
                    break;
                default:
                    return;
            }

            mMasterJobContext.MessageLastOut = DateTime.UtcNow;
        }
        #endregion

        #region MasterJobSyncIAmMaster()
        /// <summary>
        /// This method is used to send a sync message when the master job becomes active.
        /// </summary>
        /// <returns></returns>
        private async Task MasterJobSyncIAmMaster()
        {
            await NegotiationTransmit(MasterJobStates.IAmMaster);
            mMasterJobContext.MasterRecordClear(true);
        }
        #endregion

        #region MasterJobCommandsRegister()
        /// <summary>
        /// You should override this command to register incoming requests when the master job becomes active.
        /// </summary>
        protected virtual void MasterJobCommandsRegister()
        {

        }
        #endregion
        #region MasterJobCommandsUnregister()
        /// <summary>
        /// You should override this method to unregister active commands when the job is shutting down or moves to an inactive state.
        /// </summary>
        protected virtual void MasterJobCommandsUnregister()
        {

        }
        #endregion

        #region MasterJobStart()
        /// <summary>
        /// This method registers each job with the scheduler.
        /// </summary>
        protected virtual void MasterJobStart()
        {
            mMasterJobContext.Start();

            mMasterJobContext.State = MasterJobState.Active;

            MasterJobCommandsRegister();

            foreach (var job in mMasterJobContext.Jobs.Values)
            {
                try
                {
                    job.Initialise?.Invoke(job.Schedule);
                }
                catch (Exception ex)
                {
                    StatisticsInternal.Ex = ex;
                    Collector?.LogException($"MasterJob '{job.Name} could not be initialised.'",ex);
                }

                SchedulerRegister(job.Schedule);
            }

            //MasterJobSyncMaster().Wait();
        }
        #endregion
        #region MasterJobStop()
        /// <summary>
        /// This method removes each job from the scheduler.
        /// </summary>
        protected virtual void MasterJobStop()
        {
            var oldState = mMasterJobContext.State;
            mMasterJobContext.State = MasterJobState.Disabled;

            if (oldState == MasterJobState.Active)
            {
                NegotiationTransmit(MasterJobStates.ResyncMaster);

                foreach (var job in mMasterJobContext.Jobs.Values)
                {
                    try
                    {
                        job.Cleanup?.Invoke(job.Schedule);
                    }
                    catch (Exception ex)
                    {
                        Collector?.LogException($"MasterJob '{job.Name}' stop failed", ex);
                    }

                    Scheduler.Unregister(job.Schedule);
                }

                MasterJobCommandsUnregister();
            }

            //Reset the state to inactive so that it could restart at some point.
            mMasterJobContext.State = MasterJobState.Inactive;
            //NegotiationTransmit(MasterJobStates.IAmStandby).Wait();

        }
        #endregion

        #region MasterJobExecute(Schedule schedule)
        /// <summary>
        /// This method retrieves the master job from the collection and calls the 
        /// relevant action.
        /// </summary>
        /// <param name="schedule">The schedule to activate.</param>
        /// <param name="cancel">The cancellation token</param>
        protected virtual async Task MasterJobExecute(Schedule schedule, CancellationToken cancel)
        {
            var id = schedule.Id;
            if (mMasterJobContext.Jobs.ContainsKey(id))
                try
                {
                    await mMasterJobContext.Jobs[id].Action(schedule);
                }
                catch (Exception ex)
                {
                    Collector?.LogException($"MasterJob '{mMasterJobContext.Jobs[id].Name}' execute failed", ex);
                    throw;
                }
        }
        #endregion

        #region MasterJobRegister ...
        /// <summary>
        /// This method registers a master job that will be called at the schedule specified 
        /// when the job is active and the instance becomes active.
        /// </summary>
        /// <param name="frequency">Frequency that the job runs for</param>
        /// <param name="action">The action to call.</param>
        /// <param name="name">Name of the job</param>
        /// <param name="initialWait">Initial wait time</param>
        /// <param name="initialTime">Initial time to run</param>
        /// <param name="initialise">Initialise schedule action</param>
        /// <param name="cleanup">Cleanup schedule action</param>
        protected void MasterJobRegister(TimeSpan? frequency, Func<Schedule, Task> action
            , string name = null, TimeSpan? initialWait = null, DateTime? initialTime = null
            , Action<Schedule> initialise = null
            , Action<Schedule> cleanup = null)
        {
            var schedule = new Schedule(MasterJobExecute, name ?? $"MasterJob{GetType().Name}")
            {
                InitialWait = initialWait,
                Frequency = frequency,
                InitialTime = initialTime
            };

            mMasterJobContext.Jobs.Add(schedule.Id, new MasterJobHolder(schedule.Name, schedule, action, initialise, cleanup));
        }
        #endregion
    }
}
