//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Exceptions;

    public static class CoTask {
        private class InternalTaskData {
            /// <summary>
            /// The collection of message handler objects listening to thie task
            /// </summary>
            private readonly Lazy<List<MessageHandlers>> _messageHandlerList = new Lazy<List<MessageHandlers>>(() => new List<MessageHandlers>());

            internal int? ParentTaskId;
            internal Task TaskInstance;
            internal ManualResetEvent AutoManaged = new ManualResetEvent(false);

            internal bool IsStillActive {
                get { return  (! (TaskInstance != null && TaskInstance.IsCompleted)) | (ActiveChildren.Any()); }
            }

            internal IEnumerable<InternalTaskData> ActiveChildren {
                get { return from child in Children where child.IsStillActive select child; }
            }

            internal IEnumerable<InternalTaskData> Children { get {
                return from tsk in _tasks.Values where tsk.ParentTaskId == TaskInstance.Id select tsk; }
            }
           
            /// <summary>
            /// Gets the message handler list.
            /// </summary>
            /// <remarks></remarks>
            internal List<MessageHandlers> MessageHandlerList { get { return _messageHandlerList.Value; } }
        }

        private static readonly Dictionary<int,InternalTaskData> _tasks  = new Dictionary<int, InternalTaskData>();

        private static void OnTaskComplete(Task task) {
            var tid = Task.CurrentId ?? 0;

            if( _tasks.ContainsKey(tid)) {
                var tsk = _tasks[tid];
                // see if there are any outstanding child tasks
                foreach (var child in tsk.Children.ToList().Where(child => !child.IsStillActive)) {
                    OnTaskComplete(child.TaskInstance);
                }

                lock (_tasks) {
                    if (!tsk.IsStillActive) {
                        tsk.MessageHandlerList.Clear();
                        _tasks.Remove(tid);
                    }
                }
            }
        }

        public static Task<T> AsResultTask<T>(this T result) {
            var x = new TaskCompletionSource<T>(TaskCreationOptions.AttachedToParent);
            x.SetResult(result);
            return x.Task.AutoManage();
        }

        public static Task AutoManage(this Task task) {
            lock (_tasks) {
                if (!_tasks.ContainsKey(task.Id)) {
                    _tasks.Add(task.Id, new InternalTaskData {
                        TaskInstance = task,
                        ParentTaskId = Task.CurrentId
                    });
                }
                else if (!_tasks[task.Id].AutoManaged.WaitOne(0)) {
                    _tasks[task.Id].ParentTaskId = Task.CurrentId;
                    _tasks[task.Id].TaskInstance = task;
                    task.ContinueWith(OnTaskComplete, TaskContinuationOptions.AttachedToParent);
                    _tasks[task.Id].AutoManaged.Set();
                }
            }
            return task;
        }

        public static Task<T> AutoManage<T>(this Task<T> task) {
            lock (_tasks) {
                if (!_tasks.ContainsKey(task.Id)) {
                    _tasks.Add(task.Id, new InternalTaskData {
                        TaskInstance = task,
                        ParentTaskId = Task.CurrentId
                    });
                }
                else if (!_tasks[task.Id].AutoManaged.WaitOne(0)) {
                    _tasks[task.Id].ParentTaskId = Task.CurrentId;
                    _tasks[task.Id].TaskInstance = task;
                    task.ContinueWith(OnTaskComplete, TaskContinuationOptions.AttachedToParent);
                    _tasks[task.Id].AutoManaged.Set();
                }
            }
            return task;
        }

        private static InternalTaskData Instance(this Task task) {
            return _tasks.ContainsKey(task.Id) ? _tasks[task.Id] : null;
        }

        internal static Task CurrentTask { get {
            var tid = Task.CurrentId ?? 0;
            return _tasks.ContainsKey(tid) ? _tasks[tid].TaskInstance : null;
        }}


        public static Task ParentTask {
            get {
            var tid = Task.CurrentId ?? 0;
            if( _tasks.ContainsKey(tid) ) {
                var ptid = _tasks[tid].ParentTaskId ?? 0;
                return _tasks.ContainsKey(ptid) ? _tasks[ptid].TaskInstance : null;
            }
            return null;
        }}

        /// <summary>
        /// Gets the message handler.
        /// </summary>
        /// <param name="task">The task to get the message handler for.</param>
        /// <param name="messageHandlerType">the message handler class</param>
        /// <returns>A message handler; null if there isn't one. </returns>
        /// <remarks></remarks>
        internal static MessageHandlers GetMessageHandler(this Task task, Type messageHandlerType) {
            return GetMessageHandler(task.Id, messageHandlerType);
        }      
  
        /// <summary>
        /// Gets the message handler.
        /// </summary>
        /// <param name="task">The task to get the message handler for.</param>
        /// <param name="messageHandlerType">the message handler class</param>
        /// <returns>A message handler; null if there isn't one. </returns>
        /// <remarks></remarks>
        internal static MessageHandlers GetMessageHandler(int? taskId, Type messageHandlerType) {
            var tid = taskId ?? 0;

            if( _tasks.ContainsKey(tid)) {
                var inst = _tasks[tid];
                return (from handler in inst.MessageHandlerList where handler.GetType() == messageHandlerType select handler).FirstOrDefault() ??
                    GetMessageHandler(inst.ParentTaskId ?? 0, messageHandlerType);
            }
            
            return null;
        }  


        /// <summary>
        /// Adds a collection of message handlers to a task.
        /// </summary>
        /// <param name="handlers">The handlers.</param>
        /// <remarks></remarks>
        internal static void AddMessageHandlers(this Task task, IEnumerable<MessageHandlers> handlers) {
            foreach (var handler in handlers)
                AddMessageHandler(task.Id, handler);
        }

        /// <summary>
        /// Adds a collection of message handlers to a task.
        /// </summary>
        /// <param name="handlers">The handlers.</param>
        /// <remarks></remarks>
        internal  static void AddMessageHandlers(int? taskId, IEnumerable<MessageHandlers> handlers) {
            foreach (var handler in handlers)
                AddMessageHandler(taskId, handler);
        }

        /// <summary>
        /// Adds the message handler to the given task.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static MessageHandlers AddMessageHandler(this Task task, MessageHandlers handler) {
            return AddMessageHandler(task.Id, handler);
        }

        /// <summary>
        /// Adds the message handler to the given task.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal static MessageHandlers AddMessageHandler(int? taskId, MessageHandlers handler) {
            if (handler != null) {
                handler.SetMissingDelegates();

                var tid = taskId ?? 0;

                lock (_tasks) {
                    if (!_tasks.ContainsKey(tid)) {
                        _tasks.Add(tid, new InternalTaskData());
                    }
                }
                // wait until the task is AutoManage'd 
                int count = 0;
                while(! _tasks[tid].AutoManaged.WaitOne(1000) ) {
                    Console.WriteLine("Waiting for task '{0}' to get AutoManage'd for MessageHandler Registration on type '{1}'", tid, handler.GetType());
                    if( count++ > 15 ) {
                        throw new Exception("I WAITED LIKE, FOREVER, AND YOU NEVER AUTOMANAGED THE TASK MAN!");
                    }
                }
                _tasks[tid].MessageHandlerList.Add(handler);
            }
            return handler;
        }

        public static void Iterate<TResult>(this TaskCompletionSource<TResult> tcs, IEnumerable<System.Threading.Tasks.Task> asyncIterator) {
            var enumerator = asyncIterator.GetEnumerator();
            Action<System.Threading.Tasks.Task> recursiveBody = null;
            recursiveBody = completedTask => {
                if (completedTask != null && completedTask.IsFaulted) {
                    tcs.TrySetException(completedTask.Exception.InnerExceptions);
                    enumerator.Dispose();
                }
                else if (enumerator.MoveNext()) {
                    enumerator.Current.ContinueWith(recursiveBody, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
                }
                else {
                    enumerator.Dispose();
                }
            };
            recursiveBody(null);
        }
        
        public static void Ignore( this AggregateException aggregateException, Type type, Action saySomething = null) {
            foreach (var exception in aggregateException.Flatten().InnerExceptions) {
                if (exception.GetType() == type) {
                    if (saySomething != null)
                        saySomething();
                    continue;
                }
                throw new ConsoleException("Exception Caught: {0}\r\n    {1}", exception.Message, exception.StackTrace);
            }
        }
    }
}