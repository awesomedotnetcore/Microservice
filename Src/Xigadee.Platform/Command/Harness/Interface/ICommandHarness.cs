﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Xigadee
{
    /// <summary>
    /// This interface is the outwards facing interface for the command harness.
    /// </summary>
    public interface ICommandHarness
    {
        /// <summary>
        /// This is the dependencies class. This class can be shared with another harness.
        /// </summary>
        CommandHarnessDependencies Dependencies { get; }

        /// <summary>
        /// Gets the command as its base interface..
        /// </summary>
        ICommand DefaultCommand();
        /// <summary>
        /// Gets the default command policy.
        /// </summary>
        /// <returns>Returns the command policy.</returns>
        CommandPolicy DefaultPolicy();
        /// <summary>
        /// Gets the command root statistics.
        /// </summary>
        CommandStatistics DefaultStatistics();

        /// <summary>
        /// Occurs when a CommandHarnessRequest object is created.
        /// </summary>
        event EventHandler<CommandHarnessEventArgs> OnEvent;
        /// <summary>
        /// Occurs when a request CommandHarnessRequest object is created.
        /// </summary>
        event EventHandler<CommandHarnessEventArgs> OnEventRequest;
        /// <summary>
        /// Occurs when a response CommandHarnessRequest object is created.
        /// </summary>
        event EventHandler<CommandHarnessEventArgs> OnEventResponse;
        /// <summary>
        /// Occurs when an outgoing CommandHarnessRequest object is created.
        /// </summary>
        event EventHandler<CommandHarnessEventArgs> OnEventOutgoing;

        /// <summary>
        /// This is the collection of the traffic in and out of the command harness
        /// </summary>
        ConcurrentDictionary<long, CommandHarnessTraffic> Traffic { get; }
        /// <summary>
        /// A list containing the failed traffic.
        /// </summary>
        List<KeyValuePair<long, CommandHarnessTraffic>> TrafficFailed { get; }
        /// <summary>
        /// Contains the outgoing messages generated by the command in the order that they were generated.
        /// </summary>
        List<KeyValuePair<long, CommandHarnessTraffic>> TrafficPayloadOutgoing { get; }
        /// <summary>
        /// Contains the request messages generated by the command in the order that they were generated.
        /// </summary>
        List<KeyValuePair<long, CommandHarnessTraffic>> TrafficPayloadRequests { get; }
        /// <summary>
        /// Contains the response messages generated by the command in the order that they were generated.
        /// </summary>
        List<KeyValuePair<long, CommandHarnessTraffic>> TrafficPayloadResponses { get; }

        /// <summary>
        /// Gets the dispatcher, which can be used to send requests to the command.
        /// </summary>
        ICommandHarnessDispath Dispatcher { get; }

        /// <summary>
        /// Gets the registered schedules.
        /// </summary>
        Dictionary<CommandJobSchedule, bool> RegisteredSchedules { get; }
        /// <summary>
        /// Executes the schedule with the unique identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="allSchedules">This optional parameter can be used to match against just the registered schedules (false) or all schedules registered with the scheduler (true)</param>
        /// <param name="scheduleType">This optional parameter can be used to match to a specific type of schedule, by matching a string on the Schedule.ScheduleType parameter.</param>
        /// <returns>Returns true if the schedule was resolved and executed.</returns>
        bool ScheduleExecute(Guid id, bool allSchedules = false, string scheduleType = null);
        /// <summary>
        /// Executes the schedule with the name specified.
        /// </summary>
        /// <param name="name">The name of the schedule.</param>
        /// <param name="allSchedules">This optional parameter can be used to match against just the registered schedules (false) or all schedules registered with the scheduler (true)</param>
        /// <param name="scheduleType">This optional parameter can be used to match to a specific type of schedule, by matching a string on the Schedule.ScheduleType parameter.</param>
        /// <returns>Returns true if the schedule was resolved and executed.</returns>
        bool ScheduleExecute(string name, bool allSchedules = false, string scheduleType = null);
        /// <summary>
        /// Determines whether the collection has the specified schedule.
        /// </summary>
        /// <param name="name">The schedule name.</param>
        /// <param name="allSchedules">This optional parameter can be used to match against just the registered schedules (false) or all schedules registered with the scheduler (true)</param>
        /// <param name="scheduleType">This optional parameter can be used to match to a specific type of schedule, by matching a string on the Schedule.ScheduleType parameter.</param>
        /// <returns>Returns true if the schedule exists</returns>
        bool HasSchedule(string name, bool allSchedules = false, string scheduleType = null);
        /// <summary>
        /// Determines whether the collection has the specified schedule.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="allSchedules">This optional parameter can be used to match against just the registered schedules (false) or all schedules registered with the scheduler (true)</param>
        /// <param name="scheduleType">This optional parameter can be used to match to a specific type of schedule, by matching a string on the Schedule.ScheduleType parameter.</param>
        /// <returns>Returns true if the schedule exists</returns>
        bool HasSchedule(Guid id, bool allSchedules = false, string scheduleType = null);

        /// <summary>
        /// Gets the registered command methods.
        /// </summary>
        Dictionary<MessageFilterWrapper, bool> RegisteredCommandMethods { get; }
        /// <summary>
        /// Determines whether a registered command exists based on a match to the service message header. 
        /// </summary>
        /// <param name="header">The header to compare.</param>
        /// <param name="useMatch">if set to true us a match, otherwise compare exactly. The default is false.</param>
        /// <returns>
        ///   <c>true</c> if the specified header has matched or is equal; otherwise, <c>false</c>.
        /// </returns>
        bool HasCommand(ServiceMessageHeader header, bool useMatch = false);
        /// <summary>
        /// Determines whether a registered command exists based on a match to the service message header fragment. 
        /// The ChannelId will be set based on the ChannelId set in the command policy.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="useMatch">if set to true us a match, otherwise compare exact. The default is false.</param>
        /// <returns>
        ///   <c>true</c> if the specified fragment has matched or is equal; otherwise, <c>false</c>.
        /// </returns>
        bool HasCommand(ServiceMessageHeaderFragment fragment, bool useMatch = false);
    }
}