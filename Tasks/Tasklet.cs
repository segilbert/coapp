//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Tasks {
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;

    /// <summary>
    /// A CoApp representation of a Task
    /// </summary>
    /// <remarks></remarks>
    public class Tasklet {
        /// <summary>
        /// All tasks in the system.
        /// </summary>
        internal static Dictionary<int, Tasklet> AllTasks = new Dictionary<int, Tasklet>();
        /// <summary>
        /// Gets the current tasklet.
        /// </summary>
        /// <remarks></remarks>
        public static Tasklet CurrentTasklet {
            get {
                var tid = Task.CurrentId;
                return tid != null && AllTasks.ContainsKey(tid.Value) ? AllTasks[tid.Value] : null;
            }
        }
        /// <summary>
        /// Gets the current task.
        /// </summary>
        /// <remarks></remarks>
        public static Task CurrentTask { 
            get {
                var current = CurrentTasklet;
                return current != null ? current.Task : null;
            }
        }
        /// <summary>
        /// Gets a value indicating whether this instance is cancellation requested.
        /// </summary>
        /// <remarks></remarks>
        public static bool IsCancellationRequested {
            get { return CurrentCancellationToken.IsCancellationRequested; }
        }

        /// <summary>
        /// Gets the current cancellation token.
        /// </summary>
        /// <remarks></remarks>
        public static CancellationToken CurrentCancellationToken { get {
                var current = CurrentTasklet;
                return current != null ? current.CancellationToken : CancellationToken.None;
            }
        }

        /// <summary>
        /// Waitfors the child tasks.
        /// </summary>
        /// <remarks></remarks>
        public void WaitforChildTasks() {
#if DEBUG
            var milliseconds = DateTime.Now.Ticks / 10000;
            while (true) {
                if (Task.WaitAll((Task[]) ChildTasks, 100, CancellationToken))
                    return;
                var elapsed = (DateTime.Now.Ticks/10000) - milliseconds;
                Debug.WriteLine("Waiting on child tasks id=[{0}], count=[{1}], milliseconds=[{2}]", Task.Id, ChildTasks.Count(),elapsed );
            }
#else 
            Task.WaitAll((Task[]) ChildTasks, CancellationToken);
#endif
        }

        /// <summary>
        /// Waitfors the child tasks.
        /// </summary>
        /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
        /// <remarks></remarks>
        public void WaitforChildTasks(int millisecondsTimeout) {
#if DEBUG
            var milliseconds = DateTime.Now.Ticks / 10000;
            var elapsed = (DateTime.Now.Ticks / 10000) - milliseconds;
            while (elapsed < millisecondsTimeout) {
                if (Task.WaitAll((Task[]) ChildTasks, 100, CancellationToken))
                    return;
                elapsed = (DateTime.Now.Ticks/10000) - milliseconds;
                Debug.WriteLine("Waiting on child tasks id=[{0}], count=[{1}], milliseconds=[{2}]", Task.Id, ChildTasks.Count(),elapsed );
            }
#else 
            Task.WaitAll((Task[])ChildTasks, millisecondsTimeout, CancellationToken);
#endif
        }

        /// <summary>
        /// Waitfors the current child tasks.
        /// </summary>
        /// <remarks></remarks>
        public static void WaitforCurrentChildTasks() {
            var current = CurrentTasklet;
            if( current != null ) {
                current.WaitforChildTasks();
            }
        }
        /// <summary>
        /// Waitfors the current child tasks.
        /// </summary>
        /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
        /// <remarks></remarks>
        public static void WaitforCurrentChildTasks( int millisecondsTimeout) {
            var current = CurrentTasklet;
            if (current != null) {
                current.WaitforChildTasks(millisecondsTimeout);
            }
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Threading.Tasks.Task"/> to <see cref="CoApp.Toolkit.Tasks.Tasklet"/>.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks></remarks>
        public static implicit operator Tasklet(Task t) {
            return AllTasks.ContainsKey(t.Id) ? (Tasklet)AllTasks[t.Id] : new Tasklet(t);
        }
        /// <summary>
        /// Performs an implicit conversion from <see cref="CoApp.Toolkit.Tasks.Tasklet"/> to <see cref="System.Threading.Tasks.Task"/>.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks></remarks>
        public static implicit operator Task(Tasklet t) {
            return t.Task;
        }
        /// <summary>
        /// Ases the task.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <returns></returns>
        /// <remarks></remarks>
        public Task<TResult> AsTask<TResult>() { 
            return Task as Task<TResult>;
        }

        /// <summary>
        /// The collection of message handler objects listening to thie task
        /// </summary>
        private List<MessageHandlers> _messageHandlerList = new List<MessageHandlers>();
        /// <summary>
        /// Gets the message handler list.
        /// </summary>
        /// <remarks></remarks>
        public List<MessageHandlers> MessageHandlerList { get { return _messageHandlerList; } }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        /// <remarks></remarks>
        public CancellationToken CancellationToken { get; internal set; }
        /// <summary>
        /// The current Task
        /// </summary>
        public readonly Task Task;
        /// <summary>
        /// The Parent Tasklet
        /// </summary>
        public readonly Tasklet ParentTasklet;
        /// <summary>
        /// Gets the parent task.
        /// </summary>
        /// <remarks></remarks>
        public Task ParentTask {get { return ParentTasklet; }}

        /// <summary>
        /// Gets the child tasks.
        /// </summary>
        /// <remarks></remarks>
        public IEnumerable<Task> ChildTasks {
            get {
                lock (AllTasks) {
                    return (from t in AllTasks.Values where t.ParentTasklet == this select t.Task).ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the message handler.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public MessageHandlers GetMessageHandler(Type t) {
            return (from handler in MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault() ??
                (ParentTasklet != null ? ParentTasklet.GetMessageHandler(t) : null);
        }

        /// <summary>
        /// Adds the message handlers.
        /// </summary>
        /// <param name="handlers">The handlers.</param>
        /// <remarks></remarks>
        public void AddMessageHandlers(IEnumerable<MessageHandlers> handlers) {
            foreach (var handler in handlers)
                AddMessageHandler(handler);
        }

        /// <summary>
        /// Adds the message handler.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public MessageHandlers AddMessageHandler(MessageHandlers handler) {
            if (handler != null) {
                handler.SetMissingDelegates();
                MessageHandlerList.Add(handler);
            }
            return handler;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tasklet"/> class.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <remarks></remarks>
        public Tasklet(Task task) {
            Task = task;
            ParentTasklet = CurrentTasklet;
            CancellationToken = ParentTasklet != null ? ParentTasklet.CancellationToken : CancellationToken.None;
            lock (AllTasks) {
                AllTasks.Add(task.Id, this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tasklet"/> class.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="parentTask">The parent task.</param>
        /// <remarks></remarks>
        public Tasklet(Task task, Task parentTask) {
            Task = task;
            ParentTasklet = parentTask;
            CancellationToken = ParentTasklet != null ? ParentTasklet.CancellationToken : CancellationToken.None;
            
            lock (AllTasks) {
                AllTasks.Add(task.Id, this);
            }
        }
    }
}
