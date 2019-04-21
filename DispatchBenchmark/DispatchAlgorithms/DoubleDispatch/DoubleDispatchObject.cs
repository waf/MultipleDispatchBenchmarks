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
        private readonly IDictionary<Type, Tuple<Action<object>, Type>> _action1 = new Dictionary<Type, Tuple<Action<object>, Type>>();
        private readonly IDictionary<Type, Tuple<Func<object, object>, Type>> _function1 = new Dictionary<Type, Tuple<Func<object, object>, Type>>();

        private void PopulateBoundTypeMap<TBound>(IDictionary<Type, Tuple<TBound, Type>> boundTypeMap, bool forFunctionType)
        {
            var parameterCount = typeof(TBound).GetGenericArguments().Length - (forFunctionType ? 1 : 0);
            Target
            .GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where
            (
                m =>
                    (forFunctionType ? m.ReturnType != typeof(void) : m.ReturnType == typeof(void)) &&
                    m.GetParameters().Length == parameterCount &&
                    !m.GetParameters().Any(p => p.ParameterType.ContainsGenericParameters)
            )
            .Aggregate
            (
                boundTypeMap,
                (map, method) =>
                {
                    var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                    var returnType = method.ReturnType != typeof(void) ? method.ReturnType : null;
                    if
                    (
                        !(
                            (method.Name == "Equals") &&
                            (parameterTypes.Length == 1) &&
                            (parameterTypes[0] == typeof(object)) &&
                            (returnType == typeof(bool))
                        )
                    )
                    {
                        var binder = Binder.Create<TBound>(Target, method, parameterTypes, returnType);
                        map.Add(parameterTypes[0], Tuple.Create(binder.Bound, returnType));
                    }
                    return boundTypeMap;
                }
            );
        }

        protected static bool CovarianceCheck(Type functionReturnType, Type boundFunctionReturnType) =>
            functionReturnType.IsAssignableFrom(boundFunctionReturnType);

        protected virtual void Initialize()
        {
            PopulateBoundTypeMap(_action1, false);
            PopulateBoundTypeMap(_function1, true);
        }

        protected object Target { get; private set; }

        //TODO: more unit tests
        /// <summary>
        /// Creates a surrogate of the action delegate, which enables double dispatch in the concrete type of its target
        /// </summary>
        public static Action<T> CreateSurrogate<T>(Action<T> action, T prototype) =>
            CreateSurrogate(action, prototype, null);

        /// <summary>
        /// Creates a surrogate of the action delegate, which enables double dispatch in the concrete type of its target
        /// </summary>
        public static Action<T> CreateSurrogate<T>(Action<T> action, T prototype, Action orElse)
        {
            action = action ?? throw new ArgumentNullException(nameof(action));
            var target = action.Target ?? throw new ArgumentException("must be bound", nameof(action));
            var dispatch = new DoubleDispatchObject(target);
            Action<T> surrogate =
                arg =>
                    dispatch.Via(action, arg, orElse);
            return surrogate;
        }

        /// <summary>
        /// Creates a surrogate of the function delegate, which enables double dispatch in the concrete type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype) =>
            CreateSurrogate(function, prototype, null, default(TResult));

        /// <summary>
        /// Creates a surrogate of the function delegate, which enables double dispatch in the concrete type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, Func<TResult> orElse) =>
            CreateSurrogate(function, prototype, orElse, default(TResult));

        /// <summary>
        /// Creates a surrogate of the function delegate, which enables double dispatch in the concrete type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, TResult defaultResult) =>
            CreateSurrogate(function, prototype, null, defaultResult);

        /// <summary>
        /// Creates a surrogate of the function delegate, which enables double dispatch in the concrete type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, Func<TResult> orElse, TResult defaultResult)
        {
            function = function ?? throw new ArgumentNullException(nameof(function));
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
            Target = target ?? this;
            Initialize();
        }

        public void Via<T>(Action<T> action, T arg) =>
            Via(action, arg, null);

        public void Via<T>(Action<T> action, T arg, Action orElse)
        {
            var type = arg?.GetType();
            if ((type != null) && _action1.TryGetValue(type, out var tuple))
            {
                var bound = tuple.Item1;
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
            if ((type != null) && _function1.TryGetValue(type, out var tuple) && CovarianceCheck(typeof(TResult), tuple.Item2))
            {
                var bound = tuple.Item1;
                return (TResult)bound(arg);
            }
            return orElse != null ? orElse() : defaultResult;
        }
    }
}