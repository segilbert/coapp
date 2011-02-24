namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;

    public class MessageHandlers {
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

        private static Type GetDelegateReturnType(Type d) {
            if (d.BaseType != typeof(MulticastDelegate))
                throw new ApplicationException("Not a delegate.");

            MethodInfo invoke = d.GetMethod("Invoke");
            if (invoke == null)
                throw new ApplicationException("Not a delegate.");

            return invoke.ReturnType;
        }

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
    }

    public class MessageHandlers<T> : MessageHandlers where T : MessageHandlers, new() {
        private static readonly T _none = new T();
        static MessageHandlers() {
            _none.SetMissingDelegates();
        }

        public static T Invoke { get {
            var cct = CoAppTask.CurrentCoTask;
            return cct == null ? _none : cct.GetMessageHandler(typeof(T)) as T;
        } }

        public static implicit operator MessageHandlers<T>(Task task) {
            return ((CoAppTask)task);
        }

        public static implicit operator MessageHandlers<T>(CoAppTask task) {
            return (task.GetMessageHandler(typeof(T)) ?? (task.MessageHandlers = new T())) as MessageHandlers<T>;
        }

        public static implicit operator T(MessageHandlers<T> handlers) {
            return (T)handlers;
        }
    }

    public class CoAppTask {
        internal static Dictionary<int, CoAppTask> Tasks = new Dictionary<int, CoAppTask>();

        private static readonly CoAppTaskFactory _factory = new CoAppTaskFactory();
        public CancellationToken CancellationToken;
        internal List<MessageHandlers> MessageHandlerList = new List<MessageHandlers>();
        public Task ParentTask;
        public Task Task;
        public Task TopParentTask;

        public static CoAppTaskFactory Factory {
            get { return _factory; }
        }

        public static CoAppTask CurrentCoTask {
            get { return Tasks.GetOrDefault(Task.CurrentId ?? -1); }
        }

        public static Task CurrentTask {
            get { 
                var cct = CurrentCoTask;
                return cct == null ? null : cct.Task;
            }
        }

        public static Task CurrentParentTask {
            get {
                var cct = CurrentCoTask;
                return cct == null ? null : cct.ParentTask;
            }
        }

        public static Task CurrentTopParentTask {
            get {
                var cct = CurrentCoTask;
                return cct == null ? null : cct.TopParentTask;
            }
        }

        public static CancellationToken CurrentCancellationToken {
            get {
                var cct = CurrentCoTask;
                return cct == null ? CancellationToken.None : cct.CancellationToken;
            }
        }

        public MessageHandlers MessageHandlers {
            set {
                if (value != null) {
                    value.SetMissingDelegates();
                    MessageHandlerList.Add(value);
                }
            }
        }

        public MessageHandlers GetMessageHandler(Type t) {
            return (from handler in MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault() ??
                (ParentTask != null
                    ? (from handler in ((CoAppTask)(ParentTask)).MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault()
                    : null);
        }

        internal static Task AddTask(Task task, MessageHandlers messageHandlers = null) {
            lock (Tasks)
                Tasks.Add(task.Id,
                    new CoAppTask {
                        Task = task,
                        ParentTask = CurrentTask,
                        TopParentTask = CurrentTopParentTask,
                        CancellationToken = CurrentCancellationToken,
                        MessageHandlers = messageHandlers
                    });
            return task;
        }

        internal static Task AddTask(Task task, CancellationToken cancellationToken, MessageHandlers messageHandlers = null) {
            lock (Tasks)
                Tasks.Add(task.Id,
                    new CoAppTask {
                        Task = task,
                        ParentTask = CurrentTask,
                        TopParentTask = CurrentTopParentTask,
                        CancellationToken = cancellationToken,
                        MessageHandlers = messageHandlers
                    });
            return task;
        }

        internal static Task<TResult> AddTask<TResult>(Task<TResult> task, MessageHandlers messageHandlers = null) {
            lock (Tasks)
                Tasks.Add(task.Id,
                    new CoAppTask {
                        Task = task,
                        ParentTask = CurrentTask,
                        TopParentTask = CurrentTopParentTask,
                        CancellationToken = CurrentCancellationToken,
                        MessageHandlers = messageHandlers
                    });
            return task;
        }

        internal static Task<TResult> AddTask<TResult>(Task<TResult> task, CancellationToken cancellationToken,
                                                       MessageHandlers messageHandlers = null) {
            lock (Tasks)
                Tasks.Add(task.Id,
                    new CoAppTask {
                        Task = task,
                        ParentTask = CurrentTask,
                        TopParentTask = CurrentTopParentTask,
                        CancellationToken = cancellationToken,
                        MessageHandlers = messageHandlers
                    });
            return task;
        }

        public static implicit operator CoAppTask(Task t) {
            return Tasks.GetOrDefault(t.Id);
        }

        public static implicit operator Task(CoAppTask t) {
            return t.Task;
        }
    }

    public class CoAppTask<TResult> : CoAppTask {
        public new static Task<TResult> CurrentTask {
            get { return CurrentCoTask.Task as Task<TResult>; }
        }

        public new static Task<TResult> CurrentTopParentTask {
            get { return CurrentCoTask.TopParentTask as Task<TResult>; }
        }

        internal static Task<TResult> AddTask(Task<TResult> task, MessageHandlers messageHandlers = null) {
            return AddTask<TResult>(task, messageHandlers);
        }

        internal static Task<TResult> AddTask(Task<TResult> task, CancellationToken cancellationToken,
                                              MessageHandlers messageHandlers = null) {
            return AddTask<TResult>(task, cancellationToken, messageHandlers);
        }
    }
}