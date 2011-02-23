using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.Toolkit.Tasks {
    using System.Threading;
    using Extensions;
    using System.Threading.Tasks;
    using Console = System.Console;

    public class CoTask : System.Threading.Tasks.Task, ITask {
        internal static Dictionary<int, ITask> Tasks = new Dictionary<int, ITask>();
        private static readonly CoTaskFactory _factory = new CoTaskFactory();
        private readonly ITask _parentTask;
        private CancellationToken _cancellationToken;
        private List<MessageHandlers> _messageHandlerList = new List<MessageHandlers>();
        public List<MessageHandlers> MessageHandlerList{ get { return _messageHandlerList;} }


        public static ITask CurrentTask { get { return Tasks.GetOrDefault(CurrentId ?? -1); } }
        public static CancellationToken CurrentCancellationToken { get { return CurrentTask.CancellationToken;  } }
        public static ITask GetTaskById(int id) { return Tasks.GetOrDefault(id); }
        public new static CoTaskFactory Factory { get { return _factory; } }

        internal static void AddNewTask(ITask newTask ) {
            lock (Tasks) {
                Tasks.Add(((System.Threading.Tasks.Task)newTask).Id, newTask);
            }
        }

        internal static TaskCreationOptions CreationOptionsFromContinuationOptions(TaskContinuationOptions continuationOptions) {
            var NotOnAnything = TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnRanToCompletion;
            var creationOptionsMask = TaskContinuationOptions.PreferFairness | TaskContinuationOptions.LongRunning | TaskContinuationOptions.AttachedToParent;

            // Check that LongRunning and ExecuteSynchronously are not specified together 
            var illegalMask = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.LongRunning;
            if ((continuationOptions & illegalMask) == illegalMask) {
                throw new ArgumentOutOfRangeException("continuationOptions", "Task_ContinueWith_ESandLR");
            }

            // Check that no illegal options were specified 
            if ((continuationOptions & ~(creationOptionsMask | NotOnAnything | TaskContinuationOptions.ExecuteSynchronously)) != 0) {
                throw new ArgumentOutOfRangeException("continuationOptions");
            }

            // Check that we didn't specify "not on anything"
            if ((continuationOptions & NotOnAnything) == NotOnAnything) {
                throw new ArgumentOutOfRangeException("continuationOptions", "Task_ContinueWith_NotOnAnything");
            }

            return (TaskCreationOptions)(continuationOptions & creationOptionsMask);
        }

        public CoTask(Action action) : this(action,CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] {} ) {  }
        public CoTask(Action action, CancellationToken cancellationToken) : this(action, cancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] { }) { }
        public CoTask(Action action, TaskCreationOptions creationOptions) : this(action, CurrentTask.CancellationToken, creationOptions, new MessageHandlers[] { }) { }
        public CoTask(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : this(action, cancellationToken, creationOptions, new MessageHandlers[] { }) { }

        public CoTask(Action action, MessageHandlers messageHandlers) : this(action, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new [] { messageHandlers }) { }
        public CoTask(Action action, CancellationToken cancellationToken, MessageHandlers messageHandlers) : this(action, cancellationToken, TaskCreationOptions.AttachedToParent, new [] { messageHandlers }) { }
        public CoTask(Action action, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(action, CurrentTask.CancellationToken, creationOptions, new [] { messageHandlers }) { }
        public CoTask(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(action, cancellationToken, creationOptions, new[] { messageHandlers }) { }
        
        public CoTask(Action action, IEnumerable<MessageHandlers> messageHandlers) : this(action, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers ) { }
        public CoTask(Action action, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) : this(action, cancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers ) { }
        public CoTask(Action action, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) : this(action, CurrentTask.CancellationToken, creationOptions, messageHandlers ) { }
        public CoTask(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) : base(action, cancellationToken, creationOptions) {
            _parentTask = CurrentTask;
            _cancellationToken = cancellationToken;
            foreach (var handler in messageHandlers)
                AddMessageHandler(handler);
            AddNewTask(this);
        }

        public CoTask(Action<object> action, object state) : this(action, state, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] { }) { }
        public CoTask(Action<object> action, object state, CancellationToken cancellationToken) : this(action, state, cancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] { }) { }
        public CoTask(Action<object> action, object state, TaskCreationOptions creationOptions) : this(action, state, CurrentTask.CancellationToken, creationOptions, new MessageHandlers[] { }) { }
        public CoTask(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : this(action, state, cancellationToken, creationOptions, new MessageHandlers[] { }) { }

        public CoTask(Action<object> action, object state, MessageHandlers messageHandlers) : this(action, state, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new[] { messageHandlers }) { }
        public CoTask(Action<object> action, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) : this(action, state, cancellationToken, TaskCreationOptions.AttachedToParent, new[] { messageHandlers }) { }
        public CoTask(Action<object> action, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(action, state, CurrentTask.CancellationToken, creationOptions, new[] { messageHandlers }) { }
        public CoTask(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(action, cancellationToken, creationOptions, new[] { messageHandlers }) { }

        public CoTask(Action<object> action, object state, IEnumerable<MessageHandlers> messageHandlers) : this(action, state, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers) { }
        public CoTask(Action<object> action, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) : this(action, state, cancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers) { }
        public CoTask(Action<object> action, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) : this(action, state, CurrentTask.CancellationToken, creationOptions, messageHandlers) { }
        public CoTask(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers)
            : base(action, state, cancellationToken, creationOptions) {
            _parentTask = CurrentTask;
            _cancellationToken = cancellationToken;
            foreach (var handler in messageHandlers)
                AddMessageHandler(handler);
            AddNewTask(this);
        }

        public CoTask ContinueWith(Action<CoTask> continuationAction) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, CancellationToken cancellationToken) { return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWith(continuationAction, CancellationToken, continuationOptions, TaskScheduler.Current, new MessageHandlers[] { }); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, TaskScheduler scheduler) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new MessageHandlers[] { }); }
               
        public CoTask ContinueWith(Action<CoTask> continuationAction, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new [] { messageHandlers }); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new [] { messageHandlers }); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, CancellationToken, continuationOptions, TaskScheduler.Current, new [] { messageHandlers }); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new [] { messageHandlers }); }
               
        public CoTask ContinueWith(Action<CoTask> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public CoTask ContinueWith(Action<CoTask> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationAction, CancellationToken, continuationOptions, TaskScheduler.Current, messageHandlers); }
               
        public CoTask ContinueWith(Action<CoTask> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask(() => continuationAction(this), cancellationToken, CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWith(antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWith(continuationFunction, CancellationToken, continuationOptions, TaskScheduler.Current, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, TaskScheduler scheduler) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new MessageHandlers[] { }); }
                
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, continuationOptions, TaskScheduler.Current, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new[] { messageHandlers }); }
                
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, continuationOptions, TaskScheduler.Current, messageHandlers); }
        
        public  CoTask<TResult> ContinueWith<TResult>(Func<CoTask, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(this), cancellationToken, CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWith(antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public MessageHandlers GetMessageHandler(Type t) {
            return (from handler in MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault() ??
                (ParentTask != null
                    ? (from handler in ParentTask.MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault()
                    : null);
        }

        public MessageHandlers AddMessageHandler(MessageHandlers handler) {
            if (handler != null) {
                handler.SetMissingDelegates();
                MessageHandlerList.Add(handler);
            }
            return handler;
        }

        public ITask ParentTask {
            get { return _parentTask; }
        }

        public CancellationToken CancellationToken {
            get { return _cancellationToken; }
        }

        public IEnumerable<Task> ChildTasks { get {
            return from t in Tasks.Values where t.ParentTask == this select (Task) t;
        } }
    }

    public class CoTask<TResult> : System.Threading.Tasks.Task<TResult>, ITask {
        private static readonly CoTaskFactory<TResult> _factory = new CoTaskFactory<TResult>();
        private readonly ITask _parentTask;
        private CancellationToken _cancellationToken;
        private List<MessageHandlers> _messageHandlerList = new List<MessageHandlers>();
        public List<MessageHandlers> MessageHandlerList { get { return _messageHandlerList; } }

        public static ITask CurrentTask { get { return CoTask.Tasks.GetOrDefault(CurrentId ?? -1); }}
        public static CancellationToken CurrentCancellationToken { get { return CurrentTask.CancellationToken; } }
        public new static CoTaskFactory<TResult> Factory { get { return _factory;  } }


        public CoTask(Func<TResult> function) : this(function, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] { }) { }
        public CoTask(Func<TResult> function, CancellationToken cancellationToken) : this(function, cancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] { }) { }
        public CoTask(Func<TResult> function, TaskCreationOptions creationOptions) : this(function, CurrentTask.CancellationToken, creationOptions, new MessageHandlers[] { }) { }
        public CoTask(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : this(function, cancellationToken, creationOptions, new MessageHandlers[] { }) { }

        public CoTask(Func<TResult> function, MessageHandlers messageHandlers) : this(function, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new[] { messageHandlers }) { }
        public CoTask(Func<TResult> function, CancellationToken cancellationToken, MessageHandlers messageHandlers) : this(function, cancellationToken, TaskCreationOptions.AttachedToParent, new[] { messageHandlers }) { }
        public CoTask(Func<TResult> function, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(function, CurrentTask.CancellationToken, creationOptions, new[] { messageHandlers }) { }
        public CoTask(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(function, cancellationToken, creationOptions, new[] { messageHandlers }) { }

        public CoTask(Func<TResult> function, IEnumerable<MessageHandlers> messageHandlers) : this(function, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers) { }
        public CoTask(Func<TResult> function, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) : this(function, cancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers) { }
        public CoTask(Func<TResult> function, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) : this(function, CurrentTask.CancellationToken, creationOptions, messageHandlers) { }
        public CoTask(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers)
            : base(function, cancellationToken, creationOptions) {
            _parentTask = CurrentTask;
            _cancellationToken = cancellationToken;
            foreach (var handler in messageHandlers)
                AddMessageHandler(handler);
            CoTask.AddNewTask(this);
        }

        public CoTask(Func<object, TResult> function, object state) : this(function, state, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] { }) { }
        public CoTask(Func<object, TResult> function, object state, CancellationToken cancellationToken) : this(function, state, cancellationToken, TaskCreationOptions.AttachedToParent, new MessageHandlers[] { }) { }
        public CoTask(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) : this(function, state, CurrentTask.CancellationToken, creationOptions, new MessageHandlers[] { }) { }
        public CoTask(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : this(function, state, cancellationToken, creationOptions, new MessageHandlers[] { }) { }

        public CoTask(Func<object, TResult> function, object state, MessageHandlers messageHandlers) : this(function, state, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, new[] { messageHandlers }) { }
        public CoTask(Func<object, TResult> function, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) : this(function, state, cancellationToken, TaskCreationOptions.AttachedToParent, new[] { messageHandlers }) { }
        public CoTask(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(function, state, CurrentTask.CancellationToken, creationOptions, new[] { messageHandlers }) { }
        public CoTask(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) : this(function, cancellationToken, creationOptions, new[] { messageHandlers }) { }

        public CoTask(Func<object, TResult> function, object state, IEnumerable<MessageHandlers> messageHandlers) : this(function, state, CurrentTask.CancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers) { }
        public CoTask(Func<object, TResult> function, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) : this(function, state, cancellationToken, TaskCreationOptions.AttachedToParent, messageHandlers) { }
        public CoTask(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) : this(function, state, CurrentTask.CancellationToken, creationOptions, messageHandlers) { }
        public CoTask(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers)
            : base(function, state, cancellationToken, creationOptions) {
            _parentTask = CurrentTask;
            _cancellationToken = cancellationToken;
            foreach (var handler in messageHandlers)
                AddMessageHandler(handler);
            CoTask.AddNewTask(this);
        }

        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, CancellationToken cancellationToken) { return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWith(continuationAction, CancellationToken, continuationOptions, TaskScheduler.Current, new MessageHandlers[] { }); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, TaskScheduler scheduler) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new MessageHandlers[] { }); }

        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new[] { messageHandlers }); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new[] { messageHandlers }); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, CancellationToken, continuationOptions, TaskScheduler.Current, new[] { messageHandlers }); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new[] { messageHandlers }); }

        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationAction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationAction, CancellationToken, continuationOptions, TaskScheduler.Current, messageHandlers); }

        public CoTask ContinueWith(Action<CoTask<TResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            /*
            var result = new CoTask(() => continuationAction(this), messageHandlers);

            if( IsCompleted ) {
                result.RunSynchronously();
                return result;
            }
            
            //var actualTask = new CoTask(() => continuationAction(this), messageHandlers);
            //base.ContinueWith(antecedent =>
            //{
            //    Console.WriteLine("Here I am...");
            //    actualTask.RunSynchronously();
            //    Console.WriteLine("But Not Here..");

            //} , cancellationToken, continuationOptions, scheduler);

            base.ContinueWith((antecedent) => { result.Start();  } , cancellationToken, continuationOptions, scheduler);
            return result;
            */

            var actualTask = new CoTask(() => continuationAction(this), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWith(antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;

        }

        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new MessageHandlers[] { }); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWith(continuationFunction, CancellationToken, continuationOptions, TaskScheduler.Current, new MessageHandlers[] { }); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new MessageHandlers[] { }); }

        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new[] { messageHandlers }); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, new[] { messageHandlers }); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, continuationOptions, TaskScheduler.Current, new[] { messageHandlers }); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, scheduler, new[] { messageHandlers }); }

        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current, messageHandlers); }
        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWith(continuationFunction, CancellationToken, continuationOptions, TaskScheduler.Current, messageHandlers); }

        public  CoTask<TNewResult> ContinueWith<TNewResult>(Func<CoTask<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TNewResult>(() => continuationFunction(this), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWith(antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public MessageHandlers GetMessageHandler(Type t) {
            return (from handler in MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault() ??
                (ParentTask != null
                    ? (from handler in ParentTask.MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault()
                    : null);
        }

        public MessageHandlers AddMessageHandler(MessageHandlers handler) {
            if (handler != null) {
                handler.SetMissingDelegates();
                MessageHandlerList.Add(handler);
            }
            return handler;
        }

        public ITask ParentTask {
            get { return _parentTask; }
        }

        public CancellationToken CancellationToken {
            get { return _cancellationToken; }
        }

        public IEnumerable<Task> ChildTasks {
            get {
                return from t in CoTask.Tasks.Values where t.ParentTask == this select (Task) t;
            }
        }
    }
    /*
    public class CoTaskCompletionSource<TResult> : System.Threading.Tasks.TaskCompletionSource<TResult>, ITask {
        private readonly ITask _parentTask;
        private CancellationToken _cancellationToken;
        private List<MessageHandlers> _messageHandlerList = new List<MessageHandlers>();
        public List<MessageHandlers> MessageHandlerList { get { return _messageHandlerList; } }

        public static implicit operator Task<TResult>( CoTaskCompletionSource<TResult> coTaskCompletionSource ) {
            return coTaskCompletionSource.Task;
        }

        public ITask ParentTask {
            get { return _parentTask; }
        }

        public CancellationToken CancellationToken {
            get { return _cancellationToken; }
        }

        public MessageHandlers GetMessageHandler(Type t) {
            return (from handler in MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault() ??
                (ParentTask != null
                    ? (from handler in ParentTask.MessageHandlerList where handler.GetType() == t select handler).FirstOrDefault()
                    : null);
        }

        public MessageHandlers AddMessageHandler(MessageHandlers handler) {
            if (handler != null) {
                handler.SetMissingDelegates();
                MessageHandlerList.Add(handler);
            }
            return handler;
        }

        public CoTaskCompletionSource() : this(null, TaskCreationOptions.AttachedToParent) {
        }

        public CoTaskCompletionSource(object state) : this(state, TaskCreationOptions.AttachedToParent) {
        }

        public CoTaskCompletionSource(TaskCreationOptions creationOptions) : this(null, creationOptions) {
        }

        public CoTaskCompletionSource(object state, TaskCreationOptions creationOptions) : base(state, creationOptions) {
            CoApp.Toolkit.Tasks.CoTask.AddNewTask(this);
            _parentTask = CoTask.CurrentTask;
        }

        public CoTaskCompletionSource(MessageHandlers messageHandlers)
            : this(null, TaskCreationOptions.AttachedToParent, messageHandlers) {
        }

        public CoTaskCompletionSource(object state, MessageHandlers messageHandlers)
            : this(state, TaskCreationOptions.AttachedToParent, messageHandlers) {
        }

        public CoTaskCompletionSource(TaskCreationOptions creationOptions, MessageHandlers messageHandlers)
            : this(null, creationOptions, messageHandlers) {
        }

        public CoTaskCompletionSource(object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers)
            : base(state, creationOptions) {
            AddMessageHandler(messageHandlers);
            CoApp.Toolkit.Tasks.CoTask.AddNewTask(this);
            _parentTask = CoTask.CurrentTask;
        }

        public IEnumerable<ITask> ChildTasks {
            get {
                return from t in CoTask.Tasks.Values where t.ParentTask == this select t;
            }
        }
    }
     * */

}
