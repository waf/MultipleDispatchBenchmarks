using System;

namespace YSharp.Design.DoubleDispatch.Extensions
{
    public static class DoubleDispatchObjectExtensions
    {
        public static DoubleDispatchObject EnsureThreadSafe(this object target, ref DoubleDispatchObject dispatchObject) =>
            EnsureThreadSafe(target, ref dispatchObject, obj => new DoubleDispatchObject(obj));

        public static DoubleDispatchObject EnsureThreadSafe<TDispatch>(this object target, ref DoubleDispatchObject dispatchObject, Func<object, TDispatch> createDispatchObject)
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
        /// Invokes the method bound to target, with double dispatch
        /// </summary>
        public static void SurrogateInvoke<T>(this object target, Action<T> method, T arg) =>
            SurrogateInvoke(target, method, arg, null);

        /// <summary>
        /// Invokes the method bound to target, with double dispatch
        /// </summary>
        public static void SurrogateInvoke<T>(this object target, Action<T> method, T arg, Action orElse)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            method = method ?? throw new ArgumentNullException(nameof(method));
            if (!ReferenceEquals(target, method.Target) && !target.GetType().IsValueType)
            {
                throw new InvalidOperationException($"{nameof(method)} must be bound to {nameof(target)}");
            }
            DoubleDispatchObject.CreateSurrogate(method, default(T), orElse).Invoke(arg);
        }

        /// <summary>
        /// Invokes the method bound to target, with double dispatch
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg) =>
            SurrogateInvoke(target, method, arg, null, default(TResult));

        /// <summary>
        /// Invokes the method bound to target, with double dispatch
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse) =>
            SurrogateInvoke(target, method, arg, orElse, default(TResult));

        /// <summary>
        /// Invokes the method bound to target, with double dispatch
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, TResult defaultResult) =>
            SurrogateInvoke(target, method, arg, null, defaultResult);

        /// <summary>
        /// Invokes the method bound to target, with double dispatch
        /// </summary>
        public static TResult SurrogateInvoke<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse, TResult defaultResult)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            method = method ?? throw new ArgumentNullException(nameof(method));
            if (!ReferenceEquals(target, method.Target) && !target.GetType().IsValueType)
            {
                throw new InvalidOperationException($"{nameof(method)} must be bound to {nameof(target)}");
            }
            return DoubleDispatchObject.CreateSurrogate(method, default(T), orElse, defaultResult).Invoke(arg);
        }
    }
}