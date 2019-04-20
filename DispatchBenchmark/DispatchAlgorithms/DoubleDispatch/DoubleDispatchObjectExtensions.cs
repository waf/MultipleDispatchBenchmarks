using System;

namespace YSharp.Design.DoubleDispatch.Extensions
{
    public static class DoubleDispatchObjectExtensions
    {
        public static DoubleDispatchObject ThreadSafe<T>(this T target, ref DoubleDispatchObject site)
            where T : class
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            var dispatch = site;
            if (dispatch == null)
            {
                System.Threading.Interlocked.CompareExchange(ref site, dispatch = new DoubleDispatchObject(target), null);
            }
            return dispatch;
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
            if (!ReferenceEquals(target, method.Target))
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
            if (!ReferenceEquals(target, method.Target))
            {
                throw new InvalidOperationException($"{nameof(method)} must be bound to {nameof(target)}");
            }
            return DoubleDispatchObject.CreateSurrogate(method, default(T), orElse, defaultResult).Invoke(arg);
        }
    }
}