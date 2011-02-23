//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class TaskExtensions {

        //public static CancellationToken GetCancellationToken( this Task thisTask ) {
        //    return CoAppTask.Tasks[thisTask.Id].CancellationToken;
        //}

        //public static Task GetTopParentTask(this Task thisTask) {
        //    return CoAppTask.Tasks[thisTask.Id].TopParentTask;
        //}

        //public static Task<TResult> GetTopParentTask<TResult>(this Task thisTask) {
        //    return CoAppTask.Tasks[thisTask.Id].TopParentTask as Task<TResult>;
        //}

        //public static Task GetParentTask(this Task thisTask) {
        //    return CoAppTask.Tasks[thisTask.Id].ParentTask;
        //}

        //public static Task<TResult> GetParentTask<TResult>(this Task thisTask) {
        //    return CoAppTask.Tasks[thisTask.Id].ParentTask as Task<TResult>;
        //}
        /*
        public static Task ContinueWithParent(this Task thisTask, Action<Task> continuationAction) {
            return thisTask.ContinueWith(continuationAction, TaskContinuationOptions.AttachedToParent);
        }

        public static Task<TResult> ContinueWithParent<TResult>(this Task thisTask, Func<Task, TResult> continuationFunction) {
            return thisTask.ContinueWith<TResult>(continuationFunction, TaskContinuationOptions.AttachedToParent );
        }

        public static Task ContinueWithParent(this Task thisTask, Action<Task> continuationAction, CancellationToken cancellationToken) {
            return thisTask.ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }

        public static Task ContinueWithParent(this Task thisTask, Action<Task> continuationAction, TaskContinuationOptions continuationOptions) {
            return thisTask.ContinueWith(continuationAction, Task.CurrentCancellationToken  , continuationOptions | TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }

        public static Task ContinueWithParent(this Task thisTask, Action<Task> continuationAction, TaskScheduler scheduler) {
            return thisTask.ContinueWith(continuationAction, Task.CurrentCancellationToken  , TaskContinuationOptions.AttachedToParent,scheduler);
        }

        public static Task<TResult> ContinueWithParent<TResult>(this Task thisTask, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken) {
            return thisTask.ContinueWith<TResult>(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }

        public static Task<TResult> ContinueWithParent<TResult>(this Task thisTask, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions) {
            return thisTask.ContinueWith<TResult>(continuationFunction, Task.CurrentCancellationToken  , continuationOptions | TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }

        public static Task<TResult> ContinueWithParent<TResult>(this Task thisTask, Func<Task, TResult> continuationFunction, TaskScheduler scheduler) {
            return thisTask.ContinueWith<TResult>(continuationFunction,  Task.CurrentCancellationToken  , TaskContinuationOptions.AttachedToParent,scheduler);
        }

        public static Task ContinueWithParent(this Task thisTask, Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return thisTask.ContinueWith(continuationAction,  cancellationToken, continuationOptions | TaskContinuationOptions.AttachedToParent, scheduler);
        }

        public static Task<TResult> ContinueWithParent<TResult>(this Task thisTask, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return thisTask.ContinueWith<TResult>(continuationFunction,  cancellationToken, continuationOptions| TaskContinuationOptions.AttachedToParent,scheduler);
        }

        public static Task ContinueWithParent<TResult>(this Task<TResult> thisTask, Action<Task<TResult>> continuationAction) {
            return thisTask.ContinueWith(continuationAction, Task.CurrentCancellationToken , TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }


        public static Task<TNewResult> ContinueWithParent<TResult, TNewResult>(this Task<TResult> thisTask, Func<Task<TResult>, TNewResult> continuationFunction) {
            return thisTask.ContinueWith<TNewResult>(continuationFunction, Task.CurrentCancellationToken , TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }


        public static Task ContinueWithParent<TResult>(this Task<TResult> thisTask, Action<Task<TResult>> continuationAction, CancellationToken cancellationToken) {
            return thisTask.ContinueWith(continuationAction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }


        public static Task ContinueWithParent<TResult>(this Task<TResult> thisTask, Action<Task<TResult>> continuationAction, TaskContinuationOptions continuationOptions) {
            return thisTask.ContinueWith(continuationAction, Task.CurrentCancellationToken , continuationOptions | TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }


        public static Task ContinueWithParent<TResult>(this Task<TResult> thisTask, Action<Task<TResult>> continuationAction, TaskScheduler scheduler) {
            return thisTask.ContinueWith(continuationAction, Task.CurrentCancellationToken , TaskContinuationOptions.AttachedToParent,scheduler);
        }


        public static Task<TNewResult> ContinueWithParent<TResult, TNewResult>(this Task<TResult> thisTask, Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken) {
            return thisTask.ContinueWith<TNewResult>(continuationFunction, cancellationToken, TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }


        public static Task<TNewResult> ContinueWithParent<TResult, TNewResult>(this Task<TResult> thisTask, Func<Task<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions) {
            return thisTask.ContinueWith<TNewResult>(continuationFunction, Task.CurrentCancellationToken , continuationOptions | TaskContinuationOptions.AttachedToParent, TaskScheduler.Current);
        }


        public static Task<TNewResult> ContinueWithParent<TResult, TNewResult>(this Task<TResult> thisTask, Func<Task<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler) {
            return thisTask.ContinueWith<TNewResult>(continuationFunction, Task.CurrentCancellationToken , TaskContinuationOptions.AttachedToParent,scheduler);
        }


        public static Task ContinueWithParent<TResult>(this Task<TResult> thisTask, Action<Task<TResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return thisTask.ContinueWith(continuationAction, cancellationToken, continuationOptions | TaskContinuationOptions.AttachedToParent,scheduler);
        }


        public static Task<TNewResult> ContinueWithParent<TResult, TNewResult>(this Task<TResult> thisTask, Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler) {
            return thisTask.ContinueWith<TNewResult>(continuationFunction, cancellationToken, continuationOptions | TaskContinuationOptions.AttachedToParent,scheduler);
        }

         * */
        public static void Iterate<TResult>(this TaskCompletionSource<TResult> tcs, IEnumerable<CoTask> asyncIterator) {
            var enumerator = asyncIterator.GetEnumerator();
            Action<CoTask> recursiveBody = null;
            recursiveBody = completedTask => {
                if (completedTask != null && completedTask.IsFaulted) {
                    tcs.TrySetException(completedTask.Exception.InnerExceptions);
                    enumerator.Dispose();
                }
                else if (enumerator.MoveNext()) {
                    enumerator.Current.ContinueWith(recursiveBody, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.AttachedToParent);
                }
                else {
                    enumerator.Dispose();
                }
            };
            recursiveBody(null);
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
                    enumerator.Current.ContinueWith(recursiveBody, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.AttachedToParent);
                }
                else {
                    enumerator.Dispose();
                }
            };
            recursiveBody(null);
        }

    }
}