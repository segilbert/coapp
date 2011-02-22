//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Extensions {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class TaskExtensions {
        public static Task ContinueWithParent(this Task thisTask, Action<Task> continuationAction) {
            return thisTask.ContinueWith(continuationAction, TaskContinuationOptions.AttachedToParent);
        }

        public static Task ContinueWithParent(this Task thisTask, Action<Task> continuationAction, CancellationToken cancellationToken) {
            return thisTask.ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent,
                TaskScheduler.Default);
        }

        public static Task<TResult> ContinueWithParent<TResult>(this Task<TResult> thisTask, Func<Task, TResult> continuationFunction) {
            return thisTask.ContinueWith(continuationFunction, TaskContinuationOptions.AttachedToParent);
        }

        public static Task<TResult> ContinueWithParent<TResult>(this Task<TResult> thisTask, Func<Task, TResult> continuationFunction,
                                                                CancellationToken cancellationToken) {
            return thisTask.ContinueWith(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent,
                TaskScheduler.Default);
        }

        public static void Iterate<TResult>(this TaskCompletionSource<TResult> tcs, IEnumerable<Task> asyncIterator) {
            var enumerator = asyncIterator.GetEnumerator();
            Action<Task> recursiveBody = null;
            recursiveBody = completedTask => {
                if (completedTask != null && completedTask.IsFaulted) {
                    tcs.TrySetException(completedTask.Exception.InnerExceptions);
                    enumerator.Dispose();
                }
                else if (enumerator.MoveNext()) {
                    enumerator.Current.ContinueWith(recursiveBody, TaskContinuationOptions.ExecuteSynchronously);
                }
                else {
                    enumerator.Dispose();
                }
            };
            recursiveBody(null);
        }

        /*
        public static void OnProgressChanged(this Task task, Action<long> progressAction) {

            var ipc = task.AsyncState as RemoteFile;
            if( ipc != null ) {
                if( ipc.ProgressMonitors.ContainsKey(task) ) {
                    
                }
            }
        }
        */
    }
}