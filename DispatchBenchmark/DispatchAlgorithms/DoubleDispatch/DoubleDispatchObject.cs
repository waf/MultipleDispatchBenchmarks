/*
    Copyright (c) 2018-2019 Cyril Jandia

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
``Software''), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESSED
OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL CYRIL JANDIA BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of Cyril Jandia shall
not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization
from Cyril Jandia.

Inquiries : ysharp {dot} design {at} gmail {dot} com
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YSharp.Design.DoubleDispatch
{
    internal class Binder
    {
        private static readonly IDictionary<Type, Type> _binderTypes =
            new Dictionary<Type, Type>
            {
                [typeof(Action<object>)] = typeof(BindAction<>),
                [typeof(Func<object, object>)] = typeof(BindFunc<,>)
            };

        internal static Binder<TBound> Create<TBound>(object target, MethodInfo method, Type[] parameterTypes, Type returnType) =>
            (Binder<TBound>)
            Activator.CreateInstance
            (
                _binderTypes[typeof(TBound)].MakeGenericType(parameterTypes.Concat(returnType != null ? new[] { returnType } : new Type[0]).ToArray()),
                target,
                method
            );
    }

    internal abstract class Binder<TBound>
    {
        internal abstract TBound Bound { get; }
    }

    internal abstract class BindAction1 : Binder<Action<object>>
    {
    }

    internal class BindAction<T> : BindAction1
    {
        private readonly Action<object> _bound;
        private readonly Action<T> _action;

        public BindAction(object target, MethodInfo method)
        {
            _action = (Action<T>)method.CreateDelegate(typeof(Action<T>), target);
            _bound = arg => _action((T)arg);
        }

        internal override Action<object> Bound => _bound;
    }

    internal abstract class BindFunc1 : Binder<Func<object, object>>
    {
    }

    internal class BindFunc<T, TResult> : BindFunc1
    {
        private readonly Func<object, object> _bound;
        private readonly Func<T, TResult> _func;

        public BindFunc(object target, MethodInfo method)
        {
            _func = (Func<T, TResult>)method.CreateDelegate(typeof(Func<T, TResult>), target);
            _bound = arg => _func((T)arg);
        }

        internal override Func<object, object> Bound => _bound;
    }

    public class DoubleDispatchObject
    {
        private readonly IDictionary<Type, Action<object>> _action1 = new Dictionary<Type, Action<object>>();
        private readonly IDictionary<Type, Func<object, object>> _function1 = new Dictionary<Type, Func<object, object>>();
        private readonly object _target;

        private void PrepareBinder<TBound>(IDictionary<Type, TBound> dispatch, int parameterCount, bool isFunc)
        {
            _target
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where
            (
                m =>
                    (isFunc ? m.ReturnType != typeof(void) : m.ReturnType == typeof(void)) &&
                    m.GetParameters().Length == parameterCount &&
                    !m.GetParameters().Any(p => p.ParameterType.ContainsGenericParameters)
            )
            .Aggregate
            (
                dispatch,
                (map, method) =>
                {
                    var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                    var returnType = method.ReturnType != typeof(void) ? method.ReturnType : null;
                    var binder = Binder.Create<TBound>(_target, method, parameterTypes, returnType);
                    map.Add(parameterTypes[0], binder.Bound);
                    return dispatch;
                }
            );
        }

        //TODO: more unit tests
        public static Action<T> CreateSurrogate<T>(Action<T> action, T prototype) =>
            CreateSurrogate(action, prototype, null);

        public static Action<T> CreateSurrogate<T>(Action<T> action, T prototype, Action orElse)
        {
            action = action ?? throw new ArgumentNullException(nameof(action), "cannot be null");
            var target = action.Target ?? throw new ArgumentException("must be bound", nameof(action));
            var dispatch = new DoubleDispatchObject(target);
            Action<T> surrogate =
                arg =>
                    dispatch.Via(action, arg, orElse);
            return surrogate;
        }

        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype) =>
            CreateSurrogate(function, prototype, null, default(TResult));

        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, Func<TResult> orElse) =>
            CreateSurrogate(function, prototype, orElse, default(TResult));

        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, TResult defaultResult) =>
            CreateSurrogate(function, prototype, null, defaultResult);

        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, Func<TResult> orElse, TResult defaultResult)
        {
            function = function ?? throw new ArgumentNullException(nameof(function), "cannot be null");
            var target = function.Target ?? throw new ArgumentException("must be bound", nameof(function));
            var dispatch = new DoubleDispatchObject(target);
            Func<T, TResult> surrogate =
                arg =>
                    dispatch.Via(function, arg, orElse, defaultResult);
            return surrogate;
        }

        public DoubleDispatchObject() : this(null) { }

        public DoubleDispatchObject(object target)
        {
            _target = target ?? this;
            PrepareBinder(_action1, 1, false);
            PrepareBinder(_function1, 1, true);
        }

        public void Via<T>(Action<T> action, T arg) =>
            Via(action, arg, null);

        public void Via<T>(Action<T> action, T arg, Action orElse)
        {
            var type = arg?.GetType();
            if ((type != null) && _action1.TryGetValue(type, out var bound))
            {
                bound(arg);
                return;
            }
            orElse?.Invoke();
        }

        public TResult Via<T, TResult>(Func<T, TResult> function, T arg) =>
            Via(function, arg, null, default(TResult));

        public TResult Via<T, TResult>(Func<T, TResult> function, T arg, Func<TResult> orElse) =>
            Via(function, arg, orElse, default(TResult));

        public TResult Via<T, TResult>(Func<T, TResult> function, T arg, TResult defaultResult) =>
            Via(function, arg, null, defaultResult);

        public TResult Via<T, TResult>(Func<T, TResult> function, T arg, Func<TResult> orElse, TResult defaultResult)
        {
            var type = arg?.GetType();
            if ((type != null) && _function1.TryGetValue(type, out var bound))
            {
                return (TResult)bound(arg);
            }
            return orElse != null ? orElse() : defaultResult;
        }
    }
}