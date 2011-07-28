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

    public class Tasklet {
        internal static Dictionary<int, Tasklet> AllTasks = new Dictionary<int, Tasklet>();
        public static Tasklet CurrentTasklet {
            get {
                var tid = Task.CurrentId;
                return tid != null && AllTasks.ContainsKey(tid.Value) ? AllTasks[tid.Value] : null;
            }
        }
        public static Task CurrentTask { 
            get {
                var current = CurrentTasklet;
                return current != null ? current.Task : null;
            }
        }
        public static bool IsCancellationRequested {
            get { return CurrentCancellationToken.IsCancellationRequested; }
        }

        public static CancellationToken CurrentCancellationToken { get {
                var current = CurrentTasklet;
                return current != null ? current.CancellationToken : CancellationToken.None;
            }
        }

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

        public static void WaitforCurrentChildTasks() {
            var current = CurrentTasklet;
            if( current != null ) {
                current.WaitforChildTasks();
            }
        }
        public static void WaitforCurrentChildTasks( int millisecondsTimeout) {
            var current = CurrentTasklet;
            if (current != null) {
                current.WaitforChildTasks(millisecondsTimeout);
            }
        }

        public static implicit operator Tasklet(Task t) {
            return AllTasks.ContainsKey(t.Id) ? (Tasklet)AllTasks[t.Id] : new Tasklet(t);
        }
        public static implicit operator Task(Tasklet t) {
            return t.Task;
        }
        public Task<TResult> AsTask<TResult>() { 
            return Task as Task<TResult>;
        }

        private List<MessageHandlers> _messageHandlerList = new List<MessageHandlers>();
        public List<MessageHandlers> MessageHandlerList { get { return _messageHandlerList; } }
        
        public CancellationToken CancellationToken { get; internal set; }
        public readonly Task Task;
        public readonly Tasklet ParentTasklet;
        public Task ParentTask {get { return ParentTasklet; }}

        public IEnumerable<Task> ChildTasks {
            get {
                lock (AllTasks) {
                    return (from t in AllTasks.Values where t.ParentTasklet == this select t.Task).ToArray();
                }
            }
        }

        public MessageHandlers GetMessageHandler(Type t) {
            return (from handler in MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault() ??
                (ParentTasklet != null ? ParentTasklet.GetMessageHandler(t) : null);
        }

        public void AddMessageHandlers(IEnumerable<MessageHandlers> handlers) {
            foreach (var handler in handlers)
                AddMessageHandler(handler);
        }

        public MessageHandlers AddMessageHandler(MessageHandlers handler) {
            if (handler != null) {
                handler.SetMissingDelegates();
                MessageHandlerList.Add(handler);
            }
            return handler;
        }

        public Tasklet(Task task) {
            Task = task;
            ParentTasklet = CurrentTasklet;
            CancellationToken = ParentTasklet != null ? ParentTasklet.CancellationToken : CancellationToken.None;
            lock (AllTasks) {
                AllTasks.Add(task.Id, this);
            }
        }

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
