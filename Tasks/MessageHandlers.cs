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
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using Exceptions;


    /// <summary>
    /// Base class for all CoApp Task-based quasi-event classes
    /// </summary>
    /// <remarks></remarks>
    public class MessageHandlers {
        /// <summary>
        /// Gets the parameter types of a Delegate
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static Type[] GetDelegateParameterTypes(Type d) {
            if (d.BaseType != typeof(MulticastDelegate))
                throw new ApplicationException("Not a delegate.");

            MethodInfo invoke = d.GetMethod("Invoke");
            if (invoke == null)
                throw new ApplicationException("Not a delegate.");

            ParameterInfo[] parameters = invoke.GetParameters();
            Type[] typeParameters = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                typeParameters[i] = parameters[i].ParameterType;
            }
            return typeParameters;
        }

        /// <summary>
        /// Gets the Return type of a delegate
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static Type GetDelegateReturnType(Type d) {
            if (d.BaseType != typeof(MulticastDelegate))
                throw new ApplicationException("Not a delegate.");

            MethodInfo invoke = d.GetMethod("Invoke");
            if (invoke == null)
                throw new ApplicationException("Not a delegate.");

            return invoke.ReturnType;
        }

        /// <summary>
        /// Creates do-nothing delegates for events not listened to.
        /// </summary>
        /// <remarks></remarks>
        public void SetMissingDelegates() {
            foreach (var field in GetType().GetFields().Where(f => f.FieldType.BaseType == typeof(MulticastDelegate))) {
                if (field.GetValue(this) != null)
                    continue;

                Type delegateReturnType = GetDelegateReturnType(field.FieldType);
                Type[] delegateParameterTypes = GetDelegateParameterTypes(field.FieldType);

                var dynamicMethod = new DynamicMethod(string.Empty, delegateReturnType, delegateParameterTypes);
                ILGenerator il = dynamicMethod.GetILGenerator();
                if (delegateReturnType.FullName != "System.Void") {
                    if (delegateReturnType.IsValueType) {
                        il.Emit(OpCodes.Ldc_I4, 0);
                    }
                    else {
                        il.Emit(OpCodes.Ldnull);
                    }
                }
                il.Emit(OpCodes.Ret);

                field.SetValue(this, dynamicMethod.CreateDelegate(field.FieldType));
            }
        }

        public void Register() {
            NewCoTask.AddMessageHandler(Task.CurrentId, this);
        }
    }

    /*
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks></remarks>
    public class MessageHandlers<T> : MessageHandlers where T : MessageHandlers, new() {
        /// <summary>
        /// 
        /// </summary>
        private static readonly T _none = new T();
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        /// <remarks></remarks>
        static MessageHandlers() {
            _none.SetMissingDelegates();
        }

        /// <summary>
        /// Gets the delegate to invoke against.
        /// </summary>
        /// <remarks></remarks>
        public static T Invoke {
            get {
                var cct = Tasklet.CurrentTasklet;
                return cct == null ? _none : (cct.GetMessageHandler(typeof(T)) as T) ?? _none;
            }
        }

        public void Register() {
            
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="CoApp.Toolkit.Tasks.Tasklet"/> to <see cref="CoApp.Toolkit.Tasks.MessageHandlers&lt;T&gt;"/>.
        /// </summary>
        /// <param name="coTask">The co task.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks></remarks>
        public static implicit operator MessageHandlers<T>(Tasklet coTask) {
            return (coTask.GetMessageHandler(typeof(T)) ?? (coTask.AddMessageHandler(new T()))) as MessageHandlers<T>;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Threading.Tasks.Task"/> to <see cref="CoApp.Toolkit.Tasks.MessageHandlers&lt;T&gt;"/>.
        /// </summary>
        /// <param name="coTask">The co task.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks></remarks>
        public static implicit operator MessageHandlers<T>(Task coTask) {
            Tasklet tsklet = coTask;
            return (tsklet.GetMessageHandler(typeof(T)) ?? (tsklet.AddMessageHandler(new T()))) as MessageHandlers<T>;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="CoApp.Toolkit.Tasks.MessageHandlers&lt;T&gt;"/> to <see cref="T"/>.
        /// </summary>
        /// <param name="handlers">The handlers.</param>
        /// <returns>The result of the conversion.</returns>
        /// <remarks></remarks>
        public static implicit operator T(MessageHandlers<T> handlers) {
            return (T)handlers;
        }

    }
    */
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks></remarks>
    public class MessageHandlers<T> : MessageHandlers where T : MessageHandlers, new() {
        /// <summary>
        /// 
        /// </summary>
        private static readonly T _none = new T();
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        /// <remarks></remarks>
        static MessageHandlers() {
            _none.SetMissingDelegates();
        }

        /// <summary>
        /// Gets the delegate to invoke against.
        /// </summary>
        /// <remarks></remarks>
        public static T Invoke {
            get {
                var currentTask = NewCoTask.CurrentTask;
                return currentTask == null ? _none : (currentTask.GetMessageHandler(typeof(T)) as T) ?? _none;
            }
        }
    }

    public static class NewCoTask {
        private class InternalTaskData {
            /// <summary>
            /// The collection of message handler objects listening to thie task
            /// </summary>
            private readonly Lazy<List<MessageHandlers>> _messageHandlerList = new Lazy<List<MessageHandlers>>(() => new List<MessageHandlers>());

            internal int? ParentTaskId;
            internal Task TaskInstance;

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
                
                if (!tsk.IsStillActive) {
                    tsk.MessageHandlerList.Clear();
                    lock (_tasks) {
                        _tasks.Remove(tid);
                    }
                }
            }
        }

        internal static Task<T> AsResultTask<T>( this T result) {
            var x = new TaskCompletionSource<T>(TaskCreationOptions.AttachedToParent);
            x.SetResult(result);
            return x.Task.AutoManage();
        }

        internal static Task AutoManage(this Task task) {
            if( !_tasks.ContainsKey(task.Id)) {
                lock (_tasks) {
                    _tasks.Add(task.Id, new InternalTaskData {
                        TaskInstance = task,
                        ParentTaskId = Task.CurrentId
                    });
                }
            } else {
                _tasks[task.Id].ParentTaskId = Task.CurrentId;
                _tasks[task.Id].TaskInstance= task;
            }

            task.ContinueWith(OnTaskComplete,TaskContinuationOptions.AttachedToParent);
            return task;
        }

        internal static Task<T> AutoManage<T>(this Task<T> task) {
             if( !_tasks.ContainsKey(task.Id)) {
                lock (_tasks) {
                    _tasks.Add(task.Id, new InternalTaskData {
                        TaskInstance = task,
                        ParentTaskId = Task.CurrentId
                    });
                }
            } else {
                _tasks[task.Id].ParentTaskId = Task.CurrentId;
                _tasks[task.Id].TaskInstance= task;
            }

            task.ContinueWith(OnTaskComplete, TaskContinuationOptions.AttachedToParent);
            return task;
        }

        private static InternalTaskData Instance(this Task task) {
            return _tasks.ContainsKey(task.Id) ? _tasks[task.Id] : null;
        }

        internal static Task CurrentTask { get {
            var tid = Task.CurrentId ?? 0;
            return _tasks.ContainsKey(tid) ? _tasks[tid].TaskInstance : null;
        }}


        internal static Task ParentTask { get {
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

                if (!_tasks.ContainsKey(tid)) {
                    lock (_tasks) {
                        _tasks.Add(tid, new InternalTaskData());
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