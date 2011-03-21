namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class CoTaskFactory : TaskFactory {
        // Methods
        public CoTaskFactory() : this (CancellationToken.None ,TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default ) { }
        public CoTaskFactory(CancellationToken cancellationToken) : this(cancellationToken, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default) { }
        public CoTaskFactory(TaskScheduler scheduler) : this(CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, scheduler) { }
        public CoTaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions) : this(CancellationToken.None, creationOptions, continuationOptions, TaskScheduler.Default) { }
        public CoTaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) : base(cancellationToken, creationOptions, continuationOptions, scheduler) { }

        internal static TaskCreationOptions CreationOptionsFromContinuationOptions(TaskContinuationOptions continuationOptions) {
            const TaskContinuationOptions notOnAnything = TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnRanToCompletion;
            const TaskContinuationOptions creationOptionsMask = TaskContinuationOptions.PreferFairness | TaskContinuationOptions.LongRunning | TaskContinuationOptions.AttachedToParent;
            const TaskContinuationOptions illegalMask = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.LongRunning;

            // Check that LongRunning and ExecuteSynchronously are not specified together 
            if ((continuationOptions & illegalMask) == illegalMask) {
                throw new ArgumentOutOfRangeException("continuationOptions", "Task_ContinueWith_ESandLR");
            }

            // Check that no illegal options were specified 
            if ((continuationOptions & ~(creationOptionsMask | notOnAnything | TaskContinuationOptions.ExecuteSynchronously)) != 0) {
                throw new ArgumentOutOfRangeException("continuationOptions");
            }

            // Check that we didn't specify "not on anything"
            if ((continuationOptions & notOnAnything) == notOnAnything) {
                throw new ArgumentOutOfRangeException("continuationOptions", "Task_ContinueWith_NotOnAnything");
            }

            return (TaskCreationOptions)(continuationOptions & creationOptionsMask);
        }

        public new Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction) { return ContinueWhenAll(tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] {} ); }
        public new Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAll(Task[] Tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task[]>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => continuationAction(antecedent.Result), cancellationToken, continuationOptions , scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAll(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task<TAntecedentResult>[]>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => continuationAction(antecedent.Result), cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAll(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }


        public new Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TResult>(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task[]>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); }, cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAll(Tasks, tcs.SetResult, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task<TAntecedentResult>[]>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); }, cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAll(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        

        public new Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny(Task[] Tasks, Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => continuationAction(antecedent.Result), cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAny(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        
        
       
        public new Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny<TResult>(Task[] Tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); }, cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAny(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction) { return ContinueWhenAny(tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }
        
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }
        
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task<TAntecedentResult>>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); }, cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAny(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }




        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }
        
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }
        
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationAction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task<TAntecedentResult>>(CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => continuationAction(antecedent.Result), cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            base.ContinueWhenAny(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public new Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, messageHandlers); }
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, messageHandlers); }
        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => {  endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(asyncResult, antecedent => { tcs.SetResult(antecedent); }, creationOptions, scheduler);
            return actualTask;
        }

        public new Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state) { return FromAsync(beginMethod, endMethod, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, state, creationOptions, new MessageHandlers[] { }); }
        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state,CreationOptions, new [] { messageHandlers }); }
        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state, creationOptions, new [] { messageHandlers }); }
        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, state, CreationOptions, messageHandlers); }
        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => {  endMethod(antecedent.Result); });
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, state, creationOptions);
            return actualTask;
        }


        public new Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state) { return FromAsync(beginMethod, endMethod, arg1, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new MessageHandlers[] { }); }
        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, CreationOptions, new[] { messageHandlers }); }
        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new[] { messageHandlers }); }
        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, CreationOptions, messageHandlers); }
        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => {  endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, state, creationOptions);
            return actualTask;
        }

        public new Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new MessageHandlers[] { }); }
        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state,CreationOptions, new[] { messageHandlers }); }
        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new[] { messageHandlers }); }
        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, CreationOptions, messageHandlers); }
        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, arg2, state, creationOptions);
            return actualTask;
        }

        public new Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state,CreationOptions, new MessageHandlers[] { }); }
        public new Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new MessageHandlers[] { }); }
        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state,CreationOptions, new[] { messageHandlers }); }
        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new[] { messageHandlers }); }
        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state,CreationOptions, messageHandlers); }
        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, arg2, arg3, state, creationOptions);
            return actualTask;
        }

        public new Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod) { return FromAsync<TResult>(asyncResult, endMethod, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, MessageHandlers messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, Scheduler, messageHandlers); }
        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(asyncResult, antecedent => { tcs.SetResult(antecedent); }, creationOptions, scheduler);
            return actualTask;
        }
                                                
        public new Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state) { return FromAsync<TResult>(beginMethod, endMethod, state,CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions) { return FromAsync<TResult>(beginMethod, endMethod, state, creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, MessageHandlers messageHandlers) { return FromAsync<TResult>(beginMethod, endMethod, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TResult>(beginMethod, endMethod, state, creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TResult>(beginMethod, endMethod, state,CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, state, creationOptions);
            return actualTask;
        }

                                                       
        public new Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state,CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, MessageHandlers messageHandlers) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state,CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, state, creationOptions);
            return actualTask;
        }

        public new Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, arg2, state, creationOptions);
            return actualTask;
        }

        public new Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            base.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, arg2, arg3, state, creationOptions);
            return actualTask;
        }

        public new Task StartNew(Action action) { return StartNew(action, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task StartNew(Action action, TaskCreationOptions creationOptions) { return StartNew(action, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task StartNew(Action action, CancellationToken cancellationToken) { return StartNew(action, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(action, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task StartNew(Action action, MessageHandlers messageHandlers) { return StartNew(action, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new [] { messageHandlers }); }
        public Task StartNew(Action action, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(action, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task StartNew(Action action, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(action, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(action, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task StartNew(Action action, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, messageHandlers) ; }
        public Task StartNew(Action action, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, messageHandlers ); }
        public Task StartNew(Action action, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, cancellationToken, CreationOptions, Scheduler, messageHandlers ); }
        public Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new Task(action, cancellationToken, creationOptions);
            ((Tasklet)newTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)newTask).CancellationToken = cancellationToken;
            newTask.Start(scheduler);
            return newTask;
        }

        public new Task StartNew(Action<object> action, object state) { return StartNew(action, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task StartNew(Action<object> action, object state, TaskCreationOptions creationOptions) { return StartNew(action, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task StartNew(Action<object> action, object state, CancellationToken cancellationToken) { return StartNew(action, state, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task StartNew(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(action, state, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task StartNew(Action<object> action, object state, MessageHandlers messageHandlers) { return StartNew(action, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task StartNew(Action<object> action, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(action, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(action, state, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(action, state, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task StartNew(Action<object> action, object state, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task StartNew(Action<object> action, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, messageHandlers); }
        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, state, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task StartNew(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new Task(action, state, cancellationToken, creationOptions);
            ((Tasklet)newTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)newTask).CancellationToken = cancellationToken;
            newTask.Start(scheduler);
            return newTask;
        }

        public new Task<TResult> StartNew<TResult>(Func<TResult> function) { return StartNew(function, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions) { return StartNew(function, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, MessageHandlers messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new Task<TResult>(function, cancellationToken, creationOptions);
            ((Tasklet)newTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)newTask).CancellationToken = cancellationToken;
            newTask.Start(scheduler);
            return newTask;
        }

        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state) { return StartNew(function, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) { return StartNew(function, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, MessageHandlers messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new Task<TResult>(function, state, cancellationToken, creationOptions);
            ((Tasklet)newTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)newTask).CancellationToken = cancellationToken;
            newTask.Start(scheduler);
            return newTask;
        }

        public static Task CompletedTask {
            get {
                var tcs = new TaskCompletionSource<int>();
                ((Tasklet) tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
                tcs.SetResult(0);
                return tcs.Task;
            }
        }
    }



    public class CoTaskFactory<TResult> : System.Threading.Tasks.TaskFactory<TResult> {
        // Methods
        public CoTaskFactory() : this(CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default) { }
        public CoTaskFactory(CancellationToken cancellationToken) : this(cancellationToken, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default) { }
        public CoTaskFactory(TaskScheduler scheduler) : this(CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, scheduler) { }
        public CoTaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions) : this(CancellationToken.None, creationOptions, continuationOptions, TaskScheduler.Default) { }
        public CoTaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) : base(cancellationToken, creationOptions, continuationOptions, scheduler) { }

        public new Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll(Task[] Tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task[]>(CoTaskFactory.CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); }, cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            Task.Factory.ContinueWhenAll(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public new Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task<TAntecedentResult>[]>(CoTaskFactory.CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); }, cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            Task.Factory.ContinueWhenAll(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }
        
        public new Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }
                                                           
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }
                                                           
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny(Task[] Tasks, Func<Task, TResult>  continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task>(CoTaskFactory.CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); }, cancellationToken, continuationOptions, scheduler);
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            Task.Factory.ContinueWhenAny(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        
        public new Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }
                                                                                                 
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }
                                                                                                 
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(Tasks, continuationFunction, Tasklet.CurrentCancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] Tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            continuationOptions |= TaskContinuationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<Task<TAntecedentResult>>(CoTaskFactory.CreationOptionsFromContinuationOptions(continuationOptions));
            
            ((Tasklet)tcs.Task).CancellationToken = cancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return continuationFunction(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = cancellationToken;
            Task.Factory.ContinueWhenAny(Tasks, antecedent => { tcs.SetResult(antecedent); }, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }


        public new Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new MessageHandlers[] {}); }
        public new Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new [] { messageHandlers }); }
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new [] { messageHandlers }); }
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new [] { messageHandlers }); }
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, messageHandlers); }
        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            Task.Factory.FromAsync(asyncResult, antecedent => { tcs.SetResult(antecedent); }, creationOptions, scheduler);
            return actualTask;
        }


        public new Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state) { return FromAsync(beginMethod, endMethod, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, state, creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state, creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, state, CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            Task.Factory.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, state, creationOptions);
            return actualTask;
        }



        public new Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { return FromAsync(beginMethod, endMethod, arg1, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            Task.Factory.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, state, creationOptions);
            return actualTask;
        }

        public new Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            Task.Factory.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, arg2, state, creationOptions);
            return actualTask;
        }

        public new Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, CreationOptions, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state,  creationOptions, new MessageHandlers[] { }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, CreationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state,  creationOptions, new[] { messageHandlers }); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, CreationOptions, messageHandlers); }
        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            creationOptions |= TaskCreationOptions.AttachedToParent;
            var tcs = new TaskCompletionSource<IAsyncResult>(creationOptions);
            
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            var actualTask = tcs.Task.ContinueWith(antecedent => { return endMethod(antecedent.Result); });
            ((Tasklet)actualTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)actualTask).CancellationToken = Tasklet.CurrentCancellationToken;
            Task.Factory.FromAsync(beginMethod, antecedent => { tcs.SetResult(antecedent); }, arg1, arg2, arg3, state, creationOptions);
            return actualTask;
        }
     
        public new Task<TResult> StartNew(Func<TResult> function) { return StartNew(function, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions) { return StartNew(function, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task<TResult> StartNew(Func<TResult> function, MessageHandlers messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<TResult> function, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new Task<TResult>(function, cancellationToken, creationOptions);
            ((Tasklet)newTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)newTask).CancellationToken = cancellationToken;
            newTask.Start(scheduler);
            return newTask;
        }

        public new Task<TResult> StartNew(Func<object, TResult> function, object state) { return StartNew(function, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) { return StartNew(function, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, MessageHandlers messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, Tasklet.CurrentCancellationToken, creationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public Task<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new Task<TResult>(function, state, cancellationToken, creationOptions);
            ((Tasklet)newTask).AddMessageHandlers(messageHandlers);
            ((Tasklet)newTask).CancellationToken = cancellationToken;
            newTask.Start(scheduler);
            return newTask;
        }

        public static Task<TResult> AsTaskResult(TResult value) {
            var tcs = new TaskCompletionSource<TResult>();
            ((Tasklet)tcs.Task).CancellationToken = Tasklet.CurrentCancellationToken;
            tcs.SetResult(value);
            return tcs.Task;
        }
    }
}