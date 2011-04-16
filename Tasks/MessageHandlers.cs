namespace CoApp.Toolkit.Tasks {
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;

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

        public static T Invoke {
            get {
                var cct = Tasklet.CurrentTasklet;
                return cct == null ? _none : (cct.GetMessageHandler(typeof(T)) as T) ?? _none;
            }
        }

        public static implicit operator MessageHandlers<T>(Tasklet coTask) {
            return (coTask.GetMessageHandler(typeof(T)) ?? (coTask.AddMessageHandler(new T()))) as MessageHandlers<T>;
        }

        public static implicit operator MessageHandlers<T>(Task coTask) {
            Tasklet tsklet = coTask;
            return (tsklet.GetMessageHandler(typeof(T)) ?? (tsklet.AddMessageHandler(new T()))) as MessageHandlers<T>;
        }

        public static implicit operator T(MessageHandlers<T> handlers) {
            return (T)handlers;
        }
    }
}