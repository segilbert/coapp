namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class CoTaskFactory : System.Threading.Tasks.TaskFactory {
        // Methods
        public CoTaskFactory() : this (CancellationToken.None ,TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default ) { }
        public CoTaskFactory(CancellationToken cancellationToken) : this(cancellationToken, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default) { }
        public CoTaskFactory(TaskScheduler scheduler) : this(CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, scheduler) { }
        public CoTaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions) : this(CancellationToken.None, creationOptions, continuationOptions, TaskScheduler.Default) { }
        public CoTaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) : base( cancellationToken, creationOptions, continuationOptions,scheduler ) {}

        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] {} ); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new [] { messageHandlers }); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public  CoTask ContinueWhenAll(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask(() => continuationAction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAll(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask(() => continuationAction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAll(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }


        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAll<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAll(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAll<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAll(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAny(CoTask[] coTasks, Action<CoTask[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask(() => continuationAction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAny(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationAction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public CoTask ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Action<CoTask<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask(() => continuationAction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAny(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }


        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAny<TResult>(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAny(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public CoTask<TResult> ContinueWhenAny<TAntecedentResult, TResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAny(coTasks, antecedent => actualTask.RunSynchronously(scheduler), cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public new CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new MessageHandlers[] { }); }
        public CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new[] { messageHandlers }); }
        public CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, messageHandlers); }
        public CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, messageHandlers); }
        public CoTask FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask(() => { },messageHandlers);
            base.FromAsync(asyncResult, endMethod, creationOptions, scheduler).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public new CoTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state) { return FromAsync(beginMethod, endMethod, state, new MessageHandlers[] { }); }
        public new CoTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, state, creationOptions, new MessageHandlers[] { }); }
        public CoTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state, new [] { messageHandlers }); }
        public CoTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state, creationOptions, new [] { messageHandlers }); }
        public CoTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, state,  messageHandlers ); }
        public CoTask FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask(() => { }, messageHandlers);
            base.FromAsync(beginMethod, endMethod, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }


        public new CoTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state) { return FromAsync(beginMethod, endMethod, arg1, state, new MessageHandlers[] { }); }
        public new CoTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, new[] { messageHandlers }); }
        public  CoTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, messageHandlers); }
        public  CoTask FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask(() => { }, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public new CoTask FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, new MessageHandlers[] { }); }
        public new CoTask FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, new[] { messageHandlers }); }
        public  CoTask FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, messageHandlers); }
        public  CoTask FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask(() => { }, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public new CoTask FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, new MessageHandlers[] { }); }
        public new CoTask FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, new[] { messageHandlers }); }
        public  CoTask FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, messageHandlers); }
        public  CoTask FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask(() => { }, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod) { return FromAsync<TResult>(asyncResult, endMethod, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, MessageHandlers messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TResult>(asyncResult, endMethod, creationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(asyncResult, endMethod, creationOptions, scheduler).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public  CoTask<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state) { return FromAsync<TResult>(beginMethod, endMethod, state, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions) { return FromAsync<TResult>(beginMethod, endMethod, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, MessageHandlers messageHandlers) { return FromAsync<TResult>(beginMethod, endMethod, state, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TResult>(beginMethod, endMethod, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TResult>(beginMethod, endMethod, state, messageHandlers); }
        public  CoTask<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }


        public  CoTask<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, MessageHandlers messageHandlers) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TArg1, TResult>(beginMethod, endMethod, arg1, state, messageHandlers); }
        public  CoTask<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public  CoTask<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TArg1, TArg2, TResult>(beginMethod, endMethod, arg1, arg2, state, messageHandlers); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public  CoTask<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync<TArg1, TArg2, TArg3, TResult>(beginMethod, endMethod, arg1, arg2, arg3, state, messageHandlers); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public new CoTask StartNew(Action action) { return StartNew(action, CancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask StartNew(Action action, TaskCreationOptions creationOptions) { return StartNew(action, CancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask StartNew(Action action, CancellationToken cancellationToken) { return StartNew(action, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(action, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public  CoTask StartNew(Action action, MessageHandlers messageHandlers) { return StartNew(action, CancellationToken, CreationOptions, Scheduler, new [] { messageHandlers }); }
        public  CoTask StartNew(Action action, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(action, CancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask StartNew(Action action, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(action, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(action, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public  CoTask StartNew(Action action, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, CancellationToken, CreationOptions, Scheduler, messageHandlers) ; }
        public  CoTask StartNew(Action action, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, CancellationToken, creationOptions, Scheduler, messageHandlers ); }
        public  CoTask StartNew(Action action, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, cancellationToken, CreationOptions, Scheduler, messageHandlers ); }
        public  CoTask StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new CoTask(action, cancellationToken, creationOptions, messageHandlers);
            newTask.Start();
            return newTask;
        }

        public new CoTask StartNew(Action<object> action, object state) { return StartNew(action, state, CancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask StartNew(Action<object> action, object state, TaskCreationOptions creationOptions) { return StartNew(action, state, CancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask StartNew(Action<object> action, object state, CancellationToken cancellationToken) { return StartNew(action, state, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask StartNew(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(action, state, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public CoTask StartNew(Action<object> action, object state, MessageHandlers messageHandlers) { return StartNew(action, state, CancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask StartNew(Action<object> action, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(action, state, CancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask StartNew(Action<object> action, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(action, state, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public CoTask StartNew(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(action, state, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public CoTask StartNew(Action<object> action, object state, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, state, CancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public CoTask StartNew(Action<object> action, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, state, CancellationToken, creationOptions, Scheduler, messageHandlers); }
        public CoTask StartNew(Action<object> action, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(action, state, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public CoTask StartNew(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new CoTask(action, state, cancellationToken, creationOptions, messageHandlers);
            newTask.Start();
            return newTask;
        }

        public new CoTask<TResult> StartNew<TResult>(Func<TResult> function) { return StartNew(function, CancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions) { return StartNew(function, CancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, MessageHandlers messageHandlers) { return StartNew(function, CancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, CancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, CancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, CancellationToken, creationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new CoTask<TResult>(function, cancellationToken, creationOptions, messageHandlers);
            newTask.Start();
            return newTask;
        }

        public new CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state) { return StartNew(function, state, CancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) { return StartNew(function, state, CancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, MessageHandlers messageHandlers) { return StartNew(function, state, CancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, state, CancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, CancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, CancellationToken, creationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew<TResult>(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new CoTask<TResult>(function, state, cancellationToken, creationOptions, messageHandlers);
            newTask.Start();
            return newTask;
        }


        /*
        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>() {
            var result = new TaskCompletionSource<TResult>(TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state) {
            var result = new TaskCompletionSource<TResult>(state, TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state, TaskCreationOptions options) {
            var result = new TaskCompletionSource<TResult>(state, options | TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(TaskCreationOptions options) {
            var result = new TaskCompletionSource<TResult>(options | TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state, MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(state, TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(object state, TaskCreationOptions options, MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(state, options | TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }

        public TaskCompletionSource<TResult> CreateTaskCompletionSource<TResult>(TaskCreationOptions options, MessageHandlers messageHandlers) {
            var result = new TaskCompletionSource<TResult>(options | TaskCreationOptions.AttachedToParent);
            // Task.AddTask(result.Task);
            return result;
        }
         */
    }



    public class CoTaskFactory<TResult> : System.Threading.Tasks.TaskFactory<TResult> {
        // Methods
        public CoTaskFactory() : this(CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default) { }
        public CoTaskFactory(CancellationToken cancellationToken) : this(cancellationToken, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, TaskScheduler.Default) { }
        public CoTaskFactory(TaskScheduler scheduler) : this(CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskContinuationOptions.AttachedToParent, scheduler) { }
        public CoTaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions) : this(CancellationToken.None, creationOptions, continuationOptions, TaskScheduler.Default) { }
        public CoTaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) : base(cancellationToken, creationOptions, continuationOptions, scheduler) { }

        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAll(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAll(coTasks, antecedent => actualTask.Result, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAll(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAll<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAll(coTasks, antecedent => actualTask.Result, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAny(CoTask[] coTasks, Func<CoTask[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAny(coTasks, antecedent => actualTask.Result, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }

        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new MessageHandlers[] { }); }

        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, continuationOptions, scheduler, new[] { messageHandlers }); }

        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, cancellationToken, ContinuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions, IEnumerable<MessageHandlers> messageHandlers) { return ContinueWhenAny(coTasks, continuationFunction, CancellationToken, continuationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> ContinueWhenAny<TAntecedentResult>(CoTask<TAntecedentResult>[] coTasks, Func<CoTask<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var actualTask = new CoTask<TResult>(() => continuationFunction(coTasks), cancellationToken, CoTask.CreationOptionsFromContinuationOptions(continuationOptions), messageHandlers);
            base.ContinueWhenAny(coTasks, antecedent => actualTask.Result, cancellationToken, continuationOptions, scheduler);
            return actualTask;
        }


        public new Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new MessageHandlers[] {}); }
        public new Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new MessageHandlers[] { }); }
        public  Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, new [] { messageHandlers }); }
        public  Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, new [] { messageHandlers }); }
        public  Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, scheduler, new [] { messageHandlers }); }
        public  Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, CreationOptions, Scheduler, messageHandlers); }
        public  Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(asyncResult, endMethod, creationOptions, Scheduler, messageHandlers); }
        public  Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(asyncResult, endMethod, creationOptions, scheduler).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        
        public new CoTask<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state) { return FromAsync(beginMethod, endMethod, state, new MessageHandlers[] { }); }
        public new CoTask<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, state, messageHandlers); }
        public  CoTask<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }



        public new CoTask<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state) { return FromAsync(beginMethod, endMethod, arg1, state, new MessageHandlers[] { }); }
        public new CoTask<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, state, messageHandlers); }
        public  CoTask<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public new CoTask<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, new MessageHandlers[] { }); }
        public new CoTask<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new MessageHandlers[] { }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions, new[] { messageHandlers }); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, state, messageHandlers); }
        public  CoTask<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, arg2, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }

        public new CoTask<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, new MessageHandlers[] { }); }
        public new CoTask<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new MessageHandlers[] { }); }
        public CoTask<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, new[] { messageHandlers }); }
        public CoTask<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions, new[] { messageHandlers }); }
        public CoTask<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, IEnumerable<MessageHandlers> messageHandlers) { return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, messageHandlers); }
        public CoTask<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) {
            var result = new CoTask<TResult>(antecedent => ((CoTask<TResult>)antecedent).Result, messageHandlers);
            base.FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, creationOptions).ContinueWith(antecedent => result.RunSynchronously());
            return result;
        }
     
        public new CoTask<TResult> StartNew(Func<TResult> function) { return StartNew(function, CancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions) { return StartNew(function, CancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> StartNew(Func<TResult> function, MessageHandlers messageHandlers) { return StartNew(function, CancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, CancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<TResult> function, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, CancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, CancellationToken, creationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new CoTask<TResult>(function, cancellationToken, creationOptions, messageHandlers);
            newTask.Start();
            return newTask;
        }

        public new CoTask<TResult> StartNew(Func<object, TResult> function, object state) { return StartNew(function, state, CancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions) { return StartNew(function, state, CancellationToken, creationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new MessageHandlers[] { }); }
        public new CoTask<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new MessageHandlers[] { }); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, MessageHandlers messageHandlers) { return StartNew(function, state, CancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, MessageHandlers messageHandlers) { return StartNew(function, state, CancellationToken, creationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, MessageHandlers messageHandlers) { return StartNew(function, state, cancellationToken, creationOptions, scheduler, new[] { messageHandlers }); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, CancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, TaskCreationOptions creationOptions, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, CancellationToken, creationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, IEnumerable<MessageHandlers> messageHandlers) { return StartNew(function, state, cancellationToken, CreationOptions, Scheduler, messageHandlers); }
        public  CoTask<TResult> StartNew(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var newTask = new CoTask<TResult>(function, state, cancellationToken, creationOptions, messageHandlers);
            newTask.Start();
            return newTask;
        }

    }
}