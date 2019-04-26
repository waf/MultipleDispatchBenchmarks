using System;

namespace YSharp.Design.DoubleDispatch.Extensions
{
    public static class DoubleDispatchObjectExtensions
    {
        public static DoubleDispatchObject EnsureThreadSafe(this object target, ref DoubleDispatchObject dispatchObject) =>
            EnsureThreadSafe(target, ref dispatchObject, obj => new DoubleDispatchObject(obj));

        public static DoubleDispatchObject EnsureThreadSafe<TDispatch>(this object target, ref TDispatch dispatchObject, Func<object, TDispatch> createDispatchObject)
            where TDispatch : DoubleDispatchObject
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            createDispatchObject = createDispatchObject ?? throw new ArgumentNullException(nameof(createDispatchObject));
            if (dispatchObject == null)
            {
                System.Threading.Interlocked.CompareExchange(ref dispatchObject, createDispatchObject(target) ?? throw new InvalidOperationException(), null);
            }
            return dispatchObject;
        }

        /// <summary>
        /// Creates a surrogate of the type's named (static) function, which enables double dispatch in the same type
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(this TResult resultPrototype, Type type, string functionName, T prototype) =>
            CreateSurrogate(resultPrototype, type, functionName, prototype, null, default(TResult));

        /// <summary>
        /// Creates a surrogate of the type's named (static) function, which enables double dispatch in the same type
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(this TResult resultPrototype, Type type, string functionName, T prototype, Func<TResult> orElse) =>
            CreateSurrogate(resultPrototype, type, functionName, prototype, orElse, default(TResult));

        /// <summary>
        /// Creates a surrogate of the type's named (static) function, which enables double dispatch in the same type
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(this TResult resultPrototype, Type type, string functionName, T prototype, TResult defaultResult) =>
            CreateSurrogate(resultPrototype, type, functionName, prototype, null, defaultResult);

        /// <summary>
        /// Creates a surrogate of the type's named (static) function, which enables double dispatch in the same type
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(this TResult resultPrototype, Type type, string functionName, T prototype, Func<TResult> orElse, TResult defaultResult) =>
            DoubleDispatchObject.CreateSurrogate(type, functionName, prototype, orElse, defaultResult);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static void SurrogateInvoke<T>(this object target, Action<T> method, T arg) =>
            SurrogateInvoke(target, method, arg, null, out var ignored);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static void SurrogateInvoke<T>(this object target, Action<T> method, T arg, out Action<T> surrogate) =>
            SurrogateInvoke(target, method, arg, null, out surrogate);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static void SurrogateInvoke<T>(this object target, Action<T> method, T arg, Action orElse) =>
            SurrogateInvoke(target, method, arg, orElse, out var ignored);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static void SurrogateInvoke<T>(this object target, Action<T> method, T arg, Action orElse, out Action<T> surrogate)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            method = method ?? throw new ArgumentNullException(nameof(method));
            if (!ReferenceEquals(target, method.Target) && !target.GetType().IsValueType)
            {
                throw new InvalidOperationException($"{nameof(method)} must be bound to {nameof(target)}");
            }
            surrogate = DoubleDispatchObject.CreateSurrogate(method, default(T), orElse);
            surrogate.Invoke(arg);
        }

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg) =>
            SurrogateInvoke(resultPrototype, type, functionName, arg, null, default(TResult), out var ignored);

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg, out Func<T, TResult> surrogate) =>
            SurrogateInvoke(resultPrototype, type, functionName, arg, null, default(TResult), out surrogate);

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg, Func<TResult> orElse) =>
            SurrogateInvoke(resultPrototype, type, functionName, arg, orElse, default(TResult), out var ignored);

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg, Func<TResult> orElse, out Func<T, TResult> surrogate) =>
            SurrogateInvoke(resultPrototype, type, functionName, arg, orElse, default(TResult), out surrogate);

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg, TResult defaultResult) =>
            SurrogateInvoke(resultPrototype, type, functionName, arg, null, defaultResult, out var ignored);

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg, TResult defaultResult, out Func<T, TResult> surrogate) =>
            SurrogateInvoke(resultPrototype, type, functionName, arg, null, defaultResult, out surrogate);

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg, Func<TResult> orElse, TResult defaultResult) =>
            SurrogateInvoke(resultPrototype, type, functionName, arg, orElse, defaultResult, out var ignored);

        /// <summary>
        /// Invokes the type's named (static) function, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this TResult resultPrototype, Type type, string functionName, T arg, Func<TResult> orElse, TResult defaultResult, out Func<T, TResult> surrogate) =>
            (surrogate = CreateSurrogate(resultPrototype, type, functionName, arg, orElse, defaultResult)).Invoke(arg);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg) =>
            SurrogateInvoke(target, method, arg, null, default(TResult), out var ignored);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, out Func<T, TResult> surrogate) =>
            SurrogateInvoke(target, method, arg, null, default(TResult), out surrogate);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse) =>
            SurrogateInvoke(target, method, arg, orElse, default(TResult), out var ignored);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse, out Func<T, TResult> surrogate) =>
            SurrogateInvoke(target, method, arg, orElse, default(TResult), out surrogate);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, TResult defaultResult) =>
            SurrogateInvoke(target, method, arg, null, defaultResult, out var ignored);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, TResult defaultResult, out Func<T, TResult> surrogate) =>
            SurrogateInvoke(target, method, arg, null, defaultResult, out surrogate);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse, TResult defaultResult) =>
            SurrogateInvoke(target, method, arg, orElse, defaultResult, out var ignored);

        /// <summary>
        /// Invokes the method on the target, with double dispatch through arg's runtime type
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse, TResult defaultResult, out Func<T, TResult> surrogate)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            method = method ?? throw new ArgumentNullException(nameof(method));
            if (!ReferenceEquals(target, method.Target) && !target.GetType().IsValueType)
            {
                throw new InvalidOperationException($"{nameof(method)} must be bound to {nameof(target)}");
            }
            surrogate = DoubleDispatchObject.CreateSurrogate(method, default(T), orElse, defaultResult);
            return surrogate.Invoke(arg);
        }
    }
}