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

        public static void Surrogate<T>(this object target, Action<T> method, T arg) =>
            Surrogate(target, method, arg, null);

        public static void Surrogate<T>(this object target, Action<T> method, T arg, Action orElse)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            method = method ?? throw new ArgumentNullException(nameof(method));
            if (target != method.Target)
            {
                throw new InvalidOperationException($"{nameof(method)} must be bound to {nameof(target)}");
            }
            DoubleDispatchObject.CreateSurrogate(method, default(T), orElse).Invoke(arg);
        }

        public static TResult Surrogate<T, TResult>(this object target, Func<T, TResult> method, T arg) =>
            Surrogate(target, method, arg, null, default(TResult));

        public static TResult Surrogate<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse) =>
            Surrogate(target, method, arg, orElse, default(TResult));

        public static TResult Surrogate<T, TResult>(this object target, Func<T, TResult> method, T arg, TResult defaultResult) =>
            Surrogate(target, method, arg, null, defaultResult);

        public static TResult Surrogate<T, TResult>(this object target, Func<T, TResult> method, T arg, Func<TResult> orElse, TResult defaultResult)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            method = method ?? throw new ArgumentNullException(nameof(method));
            if (target != method.Target)
            {
                throw new InvalidOperationException($"{nameof(method)} must be bound to {nameof(target)}");
            }
            return DoubleDispatchObject.CreateSurrogate(method, default(T), orElse, defaultResult).Invoke(arg);
        }
    }
}