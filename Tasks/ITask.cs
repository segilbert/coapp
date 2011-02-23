namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITask {
        ITask ParentTask { get; }
        CancellationToken CancellationToken { get; }

        List<MessageHandlers> MessageHandlerList { get; }
        MessageHandlers GetMessageHandler(Type t);
        MessageHandlers AddMessageHandler(MessageHandlers handler);
        IEnumerable<Task> ChildTasks { get; }
    }

    public static class ITaskExtensions {
        /* public static CoTask ContinueWith( this ITask task, Action<CoTask> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler, IEnumerable<MessageHandlers> messageHandlers) {
            var cTask = task as CoTask;
            if( cTask != null ) {
                return cTask.ContinueWith(continuationAction, cancellationToken, continuationOptions, scheduler, messageHandlers);
            }

            return null;
        } */
    }
}