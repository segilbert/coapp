namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class CoAppTaskFactory : TaskFactory {
        public CoAppTaskFactory() : base(TaskCreationOptions.AttachedToParent,
            TaskContinuationOptions.AttachedToParent) {
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>() {
            var result = new TaskCompletionSource<TResult>(TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state) {
            var result = new TaskCompletionSource<TResult>(state, TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state, TaskCreationOptions options) {
            var result = new TaskCompletionSource<TResult>(state, options | TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(TaskCreationOptions options) {
            var result = new TaskCompletionSource<TResult>(options | TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task, messageHandlers);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state, MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(state, TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task, messageHandlers);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state, TaskCreationOptions options, MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(state, options | TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task, messageHandlers);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(TaskCreationOptions options, MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(options | TaskCreationOptions.AttachedToParent);
            CoAppTask.AddTask(result.Task, messageHandlers);
            return result;
        }


        internal Task StartNewImpl<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskScheduler scheduler, TaskCreationOptions options) {
            var t = new Task<TResult>(function, state, cancellationToken, options);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        internal Task StartNewImpl<TResult>(Func<TResult> function,CancellationToken cancellationToken, TaskScheduler scheduler, TaskCreationOptions options ) {
            var t = new Task<TResult>(function, cancellationToken, options);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        internal Task StartNewImpl(Action<object> action, object state, CancellationToken cancellationToken, TaskScheduler scheduler, TaskCreationOptions options) {
            var t = new Task(action, state, cancellationToken, options);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        internal Task StartNewImpl(Action action, CancellationToken cancellationToken, TaskScheduler scheduler, TaskCreationOptions options) {
            var t = new Task(action, cancellationToken, options);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        /*
        internal Task StartNewImpl(Action action, CancellationToken cancellationToken, TaskScheduler scheduler) {
            var t = new Task(action, cancellationToken);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        internal Task StartNewImpl(Action action, CancellationToken cancellationToken, TaskCreationOptions options) {
            var t = new Task(action, cancellationToken, options);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }

        internal Task StartNewImpl(Action action, CancellationToken cancellationToken) {
            var t = new Task(action, cancellationToken);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }

        internal Task StartNewImpl(Action action, TaskScheduler scheduler, TaskCreationOptions options) {
            var t = new Task(action, options);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }
       
        internal Task StartNewImpl(Action action, TaskScheduler scheduler) {
            var t = new Task(action);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        internal Task StartNewImpl(Action action, TaskCreationOptions options) {
            var t = new Task(action, options);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }

        internal Task StartNewImpl(Action action) {
            var t = new Task(action);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }

        internal Task StartNewImpl(Action<object> action, object state, CancellationToken cancellationToken, TaskScheduler scheduler) {
            var t = new Task(action, state, cancellationToken);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        internal Task StartNewImpl(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions options) {
            var t = new Task(action, state, cancellationToken, options);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }

        internal Task StartNewImpl(Action<object> action, object state, CancellationToken cancellationToken) {
            var t = new Task(action, state, cancellationToken);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }

        internal Task StartNewImpl(Action<object> action, object state, TaskScheduler scheduler, TaskCreationOptions options) {
            var t = new Task(action, state, options);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }


        internal Task StartNewImpl(Action<object> action, object state, TaskScheduler scheduler) {
            var t = new Task(action, state);
            CoAppTask.AddTask(t);
            t.Start(scheduler);
            return t;
        }

        internal Task StartNewImpl(Action<object> action, object state, TaskCreationOptions options) {
            var t = new Task(action, state, options);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }

        internal Task StartNewImpl(Action<object> action, object state) {
            var t = new Task(action, state);
            CoAppTask.AddTask(t);
            t.Start();
            return t;
        }
        */

        public new Task StartNew(Action action) {
            return CoAppTask.AddTask(base.StartNew(action));
        }

        public new Task<TResult> StartNew<TResult>(Func<TResult> function) {
            return CoAppTask.AddTask(base.StartNew(function));
        }

        public new Task StartNew(Action<object> action, object state) {
            return CoAppTask.AddTask(base.StartNew(action, state));
        }

        public new Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken) {
            return CoAppTask.AddTask(base.StartNew(function, cancellationToken), cancellationToken);
        }

        public new Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(base.StartNew(function, creationOptions));
        }

        public new Task StartNew(Action action, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(base.StartNew(action, creationOptions));
        }

        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state) {
            return CoAppTask.AddTask(base.StartNew(function, state));
        }

        public new Task StartNew(Action action, CancellationToken cancellationToken) {
            return CoAppTask.AddTask(base.StartNew(action, cancellationToken), cancellationToken);
        }

        public new Task StartNew(Action<object> action, object state, CancellationToken cancellationToken) {
            return CoAppTask.AddTask(base.StartNew(action, state, cancellationToken), cancellationToken);
        }

        public new Task StartNew(Action<object> action, object state, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(base.StartNew(action, state, creationOptions));
        }

        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken) {
            return CoAppTask.AddTask(base.StartNew(function, state, cancellationToken), cancellationToken);
        }

        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(base.StartNew(function, state, creationOptions));
        }

        public new Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions,
                                 TaskScheduler scheduler) {
            return CoAppTask.AddTask(base.StartNew(action, cancellationToken, creationOptions, scheduler), cancellationToken);
        }

        public new Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken,
                                                   TaskCreationOptions creationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(base.StartNew(function, cancellationToken, creationOptions, scheduler), cancellationToken);
        }

        public new Task StartNew(Action<object> action, object state, CancellationToken cancellationToken,
                                 TaskCreationOptions creationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(base.StartNew(action, state, cancellationToken, creationOptions, scheduler), cancellationToken);
        }

        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken,
                                                   TaskCreationOptions creationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(base.StartNew(function, state, cancellationToken, creationOptions, scheduler), cancellationToken);
        }

        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>[]> continuationAction) {
            return CoAppTask.AddTask(base.ContinueWhenAll(tasks, continuationAction));
        }

        public new Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction));
        }

        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>[], TResult> continuationFunction) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction));
        }

        public new Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction));
        }

        public new Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken));
        }

        public new Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, continuationOptions));
        }

        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>[]> continuationAction,
                                                           CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken));
        }

        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>[]> continuationAction,
                                                           TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, continuationOptions));
        }

        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>[], TResult> continuationFunction,
                                                                             CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken));
        }

        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>[], TResult> continuationFunction,
                                                                             TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, continuationOptions));
        }

        public new Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
                                                          CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken));
        }

        public new Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
                                                          TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, continuationOptions));
        }

        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>[]> continuationAction,
                                                           CancellationToken cancellationToken,
                                                           TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>[], TResult> continuationFunction,
                                                                             CancellationToken cancellationToken,
                                                                             TaskContinuationOptions continuationOptions,
                                                                             TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken,
                                        TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
                                                          CancellationToken cancellationToken, TaskContinuationOptions continuationOptions,
                                                          TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>> continuationAction) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction));
        }

        public new Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction));
        }

        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>, TResult> continuationFunction) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction));
        }

        public new Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction));
        }

        public new Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken));
        }

        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>> continuationAction,
                                                           CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken));
        }

        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>, TResult> continuationFunction,
                                                                             TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, continuationOptions));
        }

        public new Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, continuationOptions));
        }

        public new Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
                                                          CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken));
        }

        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>> continuationAction,
                                                           TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, continuationOptions));
        }

        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>, TResult> continuationFunction,
                                                                             CancellationToken cancellationToken) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken));
        }

        public new Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
                                                          TaskContinuationOptions continuationOptions) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, continuationOptions));
        }

        public new Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
                                                          CancellationToken cancellationToken, TaskContinuationOptions continuationOptions,
                                                          TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                           Action<Task<TAntecedentResult>> continuationAction,
                                                           CancellationToken cancellationToken,
                                                           TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                             Func<Task<TAntecedentResult>, TResult> continuationFunction,
                                                                             CancellationToken cancellationToken,
                                                                             TaskContinuationOptions continuationOptions,
                                                                             TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken,
                                        TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler));
        }

        public new Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod));
        }

        public new Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod));
        }

        public new Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state));
        }

        public new Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
                                                    Func<IAsyncResult, TResult> endMethod, object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state));
        }

        public new Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions));
        }

        public new Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
                                                    TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions));
        }

        public new Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state,
                                  TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state, creationOptions));
        }

        public new Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
                                                    Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state, creationOptions));
        }

        public new Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod,
                                         TArg1 arg1, object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state));
        }

        public new Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
                                                           Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state));
        }

        public new Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions,
                                  TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions, scheduler));
        }

        public new Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
                                                    TaskCreationOptions creationOptions, TaskScheduler scheduler) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions, scheduler));
        }

        public new Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod,
                                         TArg1 arg1, object state, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state, creationOptions));
        }

        public new Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
                                                           Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state,
                                                           TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state, creationOptions));
        }

        public new Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                                Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state));
        }

        public new Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                                                  Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2,
                                                                  object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state));
        }

        public new Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                                Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state,
                                                TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions));
        }

        public new Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                                                  Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2,
                                                                  object state, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions));
        }

        public new Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
                                                       Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state));
        }

        public new Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1,
            TArg2 arg2, TArg3 arg3, object state) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state));
        }

        public new Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
                                                       Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state,
                                                       TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions));
        }

        public new Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1,
            TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions));
        }

        public Task StartNew(Action action, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action), messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function), messageHandlers);
        }

        public Task StartNew(Action<object> action, object state, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action, state), messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function, cancellationToken), cancellationToken, messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function, creationOptions), messageHandlers);
        }

        public Task StartNew(Action action, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action, creationOptions), messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function, state), messageHandlers);
        }

        public Task StartNew(Action action, CancellationToken cancellationToken, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action, cancellationToken), cancellationToken, messageHandlers);
        }

        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action, state, cancellationToken), cancellationToken, messageHandlers);
        }

        public Task StartNew(Action<object> action, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action, state, creationOptions), messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken,
                                               MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function, state, cancellationToken), cancellationToken, messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions,
                                               MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function, state, creationOptions), messageHandlers);
        }

        public Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions,
                             TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action, cancellationToken, creationOptions, scheduler), cancellationToken,
                messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken,
                                               TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function, cancellationToken, creationOptions, scheduler), cancellationToken,
                messageHandlers);
        }

        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken,
                             TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(action, state, cancellationToken, creationOptions, scheduler), cancellationToken,
                messageHandlers);
        }

        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken,
                                               TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.StartNew(function, state, cancellationToken, creationOptions, scheduler), cancellationToken,
                messageHandlers);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>[]> continuationAction, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(base.ContinueWhenAll(tasks, continuationAction), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
                                                      MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>[], TResult> continuationFunction,
                                                                         MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction), messageHandlers);
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction), messageHandlers);
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken,
                                    MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken), messageHandlers);
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions,
                                    MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, continuationOptions), messageHandlers);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>[]> continuationAction,
                                                       CancellationToken cancellationToken, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken), messageHandlers);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>[]> continuationAction,
                                                       TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, continuationOptions), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>[], TResult> continuationFunction,
                                                                         CancellationToken cancellationToken,
                                                                         MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>[], TResult> continuationFunction,
                                                                         TaskContinuationOptions continuationOptions,
                                                                         MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, continuationOptions), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
                                                      CancellationToken cancellationToken, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
                                                      TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, continuationOptions), messageHandlers);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>[]> continuationAction,
                                                       CancellationToken cancellationToken,
                                                       TaskContinuationOptions continuationOptions, TaskScheduler scheduler,
                                                       MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>[], TResult> continuationFunction,
                                                                         CancellationToken cancellationToken,
                                                                         TaskContinuationOptions continuationOptions,
                                                                         TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken,
                                    TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationAction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction,
                                                      CancellationToken cancellationToken, TaskContinuationOptions continuationOptions,
                                                      TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAll(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>> continuationAction, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction), messageHandlers);
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>, TResult> continuationFunction,
                                                                         MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
                                                      MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction), messageHandlers);
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken,
                                    MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken), messageHandlers);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>> continuationAction,
                                                       CancellationToken cancellationToken, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>, TResult> continuationFunction,
                                                                         TaskContinuationOptions continuationOptions,
                                                                         MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, continuationOptions), messageHandlers);
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions,
                                    MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, continuationOptions), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
                                                      CancellationToken cancellationToken, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken), messageHandlers);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>> continuationAction,
                                                       TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, continuationOptions), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>, TResult> continuationFunction,
                                                                         CancellationToken cancellationToken,
                                                                         MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
                                                      TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, continuationOptions), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction,
                                                      CancellationToken cancellationToken, TaskContinuationOptions continuationOptions,
                                                      TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks,
                                                       Action<Task<TAntecedentResult>> continuationAction,
                                                       CancellationToken cancellationToken,
                                                       TaskContinuationOptions continuationOptions, TaskScheduler scheduler,
                                                       MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks,
                                                                         Func<Task<TAntecedentResult>, TResult> continuationFunction,
                                                                         CancellationToken cancellationToken,
                                                                         TaskContinuationOptions continuationOptions,
                                                                         TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken,
                                    TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.ContinueWhenAny(tasks, continuationAction, cancellationToken, continuationOptions, scheduler), messageHandlers);
        }

        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod), messageHandlers);
        }

        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
                                                MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod), messageHandlers);
        }

        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state,
                              MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state), messageHandlers);
        }

        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
                                                Func<IAsyncResult, TResult> endMethod, object state, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state), messageHandlers);
        }

        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions,
                              MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions), messageHandlers);
        }

        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
                                                TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions), messageHandlers);
        }

        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state,
                              TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state, creationOptions), messageHandlers);
        }

        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod,
                                                Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions,
                                                MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, state, creationOptions), messageHandlers);
        }

        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod,
                                     TArg1 arg1, object state, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state), messageHandlers);
        }

        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
                                                       Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state,
                                                       MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state), messageHandlers);
        }

        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions,
                              TaskScheduler scheduler, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions, scheduler), messageHandlers);
        }

        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod,
                                                TaskCreationOptions creationOptions, TaskScheduler scheduler,
                                                MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(asyncResult, endMethod, creationOptions, scheduler), messageHandlers);
        }

        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod,
                                     TArg1 arg1, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state, creationOptions), messageHandlers);
        }

        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod,
                                                       Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state,
                                                       TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, state, creationOptions), messageHandlers);
        }

        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state,
                                            MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state), messageHandlers);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                                              Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state,
                                                              MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state), messageHandlers);
        }

        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                            Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state,
                                            TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions), messageHandlers);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod,
                                                              Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state,
                                                              TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions), messageHandlers);
        }

        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
                                                   Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state,
                                                   MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state), messageHandlers);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1,
            TArg2 arg2, TArg3 arg3, object state, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state), messageHandlers);
        }

        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod,
                                                   Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state,
                                                   TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions), messageHandlers);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(
            Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1,
            TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) {
            return CoAppTask.AddTask(
                base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions), messageHandlers);
        }
    }
}