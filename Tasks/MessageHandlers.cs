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
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;

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
    }

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
}