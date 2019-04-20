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
    }
}