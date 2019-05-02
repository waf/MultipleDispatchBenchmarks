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
        private readonly IDictionary<string, IDictionary<Type, Tuple<Action<object>, Type>>> _action1 = new Dictionary<string, IDictionary<Type, Tuple<Action<object>, Type>>>();
        private readonly IDictionary<string, IDictionary<Type, Tuple<Func<object, object>, Type>>> _function1 = new Dictionary<string, IDictionary<Type, Tuple<Func<object, object>, Type>>>();

        private void PopulateMultimethodMap<TBound>(IDictionary<string, IDictionary<Type, Tuple<TBound, Type>>> multimethodMap, bool forFunctionType)
        {
            ParameterInfo[] parameters;
            (
                !(Target is Type) ?
                Target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                :
                ((Type)Target).GetMethods(BindingFlags.Public | BindingFlags.Static)
            )
            .Where
            (
                m =>
                    (Target is Type ? m.IsPublic : (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly)) &&
                    !m.IsSpecialName &&
                    (forFunctionType ? m.ReturnType != typeof(void) : m.ReturnType == typeof(void)) &&
                    (parameters = m.GetParameters()).Length == 1 &&
                    !parameters[0].ParameterType.ContainsGenericParameters &&
                    !
                    (
                        (m.Name == "Equals") &&
                        (parameters[0].ParameterType == typeof(object)) &&
                        (m.ReturnType == typeof(bool))
                    )
            )
            .Aggregate
            (
                multimethodMap,
                (map, method) =>
                {
                    var parameterType = method.GetParameters()[0].ParameterType;
                    var returnType = method.ReturnType != typeof(void) ? method.ReturnType : null;
                    var binder = Binder.Create<TBound>(!(Target is Type) ? Target : null, method, new[] { parameterType }, returnType);
                    if (!multimethodMap.TryGetValue(method.Name, out var boundTypeMap))
                    {
                        multimethodMap.Add(method.Name, boundTypeMap = new Dictionary<Type, Tuple<TBound, Type>>());
                    }
                    if (!boundTypeMap.ContainsKey(parameterType))
                    {
                        boundTypeMap.Add(parameterType, Tuple.Create(binder.Bound, returnType));
                    }
                    return map;
                }
            );
        }

        protected static bool CovarianceCheck(Type functionReturnType, Type boundFunctionReturnType) =>
            functionReturnType.IsAssignableFrom(boundFunctionReturnType);

        protected virtual void Initialize()
        {
            if (!(Target is Type))
            {
                PopulateMultimethodMap(_action1, false);
            }
            PopulateMultimethodMap(_function1, true);
        }

        protected virtual bool TryBindAction1(string methodName, Type argType, Type viaType, out Action<object> bound)
        {
            methodName = !string.IsNullOrEmpty(methodName) ? methodName : throw new ArgumentException("cannot be null or empty", nameof(methodName));
            bound = null;
            if (_action1.TryGetValue(methodName, out var boundTypeMap))
            {
                Tuple<Action<object>, Type> tuple = null;
                if (argType.IsClass)
                {
                    // If no exact match for argType, walk down its bases (as a class type),
                    // until we find one that is assignment-compatible with our initial argType
                    while (((argType != viaType) || ForSurrogate) && !boundTypeMap.TryGetValue(argType, out tuple) && (argType != typeof(object)))
                    {
                        argType = argType.BaseType;
                    }
                }
                else
                {
                    if (((argType != viaType) || ForSurrogate) && !boundTypeMap.TryGetValue(argType, out tuple) && argType.IsValueType)
                    {
                        // Special handling of value type argument:
                        // couldn't bind with argType, so try to bind with System.Object
                        boundTypeMap.TryGetValue(typeof(object), out tuple);
                    }
                }
                if (tuple != null)
                {
                    bound = tuple.Item1;
                    return true;
                }
            }
            return false;
        }

        protected virtual bool TryInvoke<T>(string methodName, T arg)
        {
            var type = arg?.GetType();
            if ((type != null) && TryBindAction1(methodName, type, !type.IsValueType && !typeof(Delegate).IsAssignableFrom(type) ? typeof(T) : null, out var bound))
            {
                bound(arg);
                return true;
            }
            return false;
        }

        protected virtual bool TryBindFunc1(string methodName, Type argType, Type returnType, Type viaType, out Func<object, object> bound)
        {
            methodName = !string.IsNullOrEmpty(methodName) ? methodName : throw new ArgumentException("cannot be null or empty", nameof(methodName));
            bound = null;
            if (_function1.TryGetValue(methodName, out var boundTypeMap))
            {
                Tuple<Func<object, object>, Type> tuple = null;
                if (argType.IsClass)
                {
                    // If no exact match for argType, walk down its bases (as a class type),
                    // until we find one that is assignment-compatible with our initial argType
                    while (((argType != viaType) || ForSurrogate) && !(boundTypeMap.TryGetValue(argType, out tuple) && CovarianceCheck(returnType, tuple.Item2)) && (argType != typeof(object)))
                    {
                        argType = argType.BaseType;
                    }
                }
                else
                {
                    if (((argType != viaType) || ForSurrogate) && !(boundTypeMap.TryGetValue(argType, out tuple) && CovarianceCheck(returnType, tuple.Item2)) && argType.IsValueType)
                    {
                        // Special handling of value type argument:
                        // couldn't bind with argType, so try to bind with System.Object
                        if (boundTypeMap.TryGetValue(typeof(object), out tuple) && !CovarianceCheck(returnType, tuple.Item2))
                        {
                            // Could eventually bind with System.Object, but covariance check failed, so back off
                            tuple = null;
                        }
                    }
                }
                if (tuple != null)
                {
                    bound = tuple.Item1;
                    return true;
                }
            }
            return false;
        }

        protected virtual bool TryInvoke<T, TResult>(string methodName, T arg, TResult defaultResult, out TResult result)
        {
            var type = arg?.GetType();
            result = defaultResult;
            if ((type != null) && TryBindFunc1(methodName, type, typeof(TResult), !type.IsValueType && !typeof(Delegate).IsAssignableFrom(type) ? typeof(T) : null, out var bound))
            {
                result = (TResult)bound(arg);
                return true;
            }
            return false;
        }

        protected object Target { get; private set; }

        protected bool ForSurrogate { get; private set; }

        /// <summary>
        /// Creates a surrogate of the action, which enables double dispatch in the runtime type of its target
        /// </summary>
        public static Action<T> CreateSurrogate<T>(Action<T> action, T prototype) =>
            CreateSurrogate(action, prototype, null);

        /// <summary>
        /// Creates a surrogate of the action, which enables double dispatch in the runtime type of its target
        /// </summary>
        public static Action<T> CreateSurrogate<T>(Action<T> action, T prototype, Action orElse)
        {
            action = action ?? throw new ArgumentNullException(nameof(action));
            var target = action.Target ?? throw new ArgumentException("must be bound", nameof(action));
            var dispatch = new DoubleDispatchObject(target, true);
            var methodName = action.GetMethodInfo().Name;
            Action<T> surrogate =
                arg =>
                    dispatch.Via(methodName, arg, orElse);
            return surrogate;
        }

        /// <summary>
        /// Creates a surrogate of the type's named (static) function, which enables double dispatch in the same type
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Type type, string functionName, T prototype, Func<TResult> orElse) =>
            CreateSurrogate(type, functionName, prototype, orElse, default(TResult));

        /// <summary>
        /// Creates a surrogate of the type's named (static) function, which enables double dispatch in the same type
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Type type, string functionName, T prototype, TResult defaultResult) =>
            CreateSurrogate(type, functionName, prototype, null, defaultResult);

        /// <summary>
        /// Creates a surrogate of the type's named (static) function, which enables double dispatch in the same type
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Type type, string functionName, T prototype, Func<TResult> orElse, TResult defaultResult)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            functionName = !string.IsNullOrEmpty(functionName) ? functionName : throw new ArgumentException("cannot be null or empty", nameof(functionName));
            var dispatch = new DoubleDispatchObject(type, true);
            Func<T, TResult> surrogate =
                arg =>
                    dispatch.Via(functionName, arg, orElse, defaultResult);
            return surrogate;
        }

        /// <summary>
        /// Creates a surrogate of the function, which enables double dispatch in the runtime type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype) =>
            CreateSurrogate(function, prototype, null, default(TResult));

        /// <summary>
        /// Creates a surrogate of the function, which enables double dispatch in the runtime type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, Func<TResult> orElse) =>
            CreateSurrogate(function, prototype, orElse, default(TResult));

        /// <summary>
        /// Creates a surrogate of the function, which enables double dispatch in the runtime type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, TResult defaultResult) =>
            CreateSurrogate(function, prototype, null, defaultResult);

        /// <summary>
        /// Creates a surrogate of the function, which enables double dispatch in the runtime type of its target
        /// </summary>
        public static Func<T, TResult> CreateSurrogate<T, TResult>(Func<T, TResult> function, T prototype, Func<TResult> orElse, TResult defaultResult)
        {
            function = function ?? throw new ArgumentNullException(nameof(function));
            var target = function.Target ?? throw new ArgumentException("must be bound", nameof(function));
            var dispatch = new DoubleDispatchObject(target, true);
            var methodName = function.GetMethodInfo().Name;
            Func<T, TResult> surrogate =
                arg =>
                    dispatch.Via(methodName, arg, orElse, defaultResult);
            return surrogate;
        }

        /// <summary>
        /// Constructs a new DoubleDispatchObject instance, to enable double dispatch in this
        /// </summary>
        public DoubleDispatchObject() : this(null) { }

        /// <summary>
        /// Constructs a new DoubleDispatchObject instance, to enable double dispatch in the Target
        /// </summary>
        public DoubleDispatchObject(object target) : this(target, false) { }

        /// <summary>
        /// Constructs a new DoubleDispatchObject instance, to enable double dispatch in the Target
        /// </summary>
        public DoubleDispatchObject(object target, bool forSurrogate)
        {
            Target = target ?? this;
            ForSurrogate = forSurrogate;
            Initialize();
        }

        /// <summary>
        /// Invokes the named method on the Target, with double dispatch through arg's runtime type
        /// </summary>
        public void Via<T>(string methodName, T arg) =>
            Via(methodName, arg, null);

        /// <summary>
        /// Invokes the named method on the Target, with double dispatch through arg's runtime type
        /// </summary>
        public void Via<T>(string methodName, T arg, Action orElse)
        {
            if (!TryInvoke(methodName, arg))
            {
                orElse?.Invoke();
            }
        }

        /// <summary>
        /// Invokes the named method on the Target, with double dispatch through arg's runtime type
        /// </summary>
        public TResult Via<T, TResult>(string methodName, T arg, Func<TResult> orElse) =>
            Via(methodName, arg, orElse, default(TResult));

        /// <summary>
        /// Invokes the named method on the Target, with double dispatch through arg's runtime type
        /// </summary>
        public TResult Via<T, TResult>(string methodName, T arg, TResult defaultResult) =>
            Via(methodName, arg, null, defaultResult);

        /// <summary>
        /// Invokes the named method on the Target, with double dispatch through arg's runtime type
        /// </summary>
        public TResult Via<T, TResult>(string methodName, T arg, Func<TResult> orElse, TResult defaultResult)
        {
            if (!TryInvoke(methodName, arg, defaultResult, out var result))
            {
                result = orElse != null ? orElse() : result;
            }
            return result;
        }
    }
}