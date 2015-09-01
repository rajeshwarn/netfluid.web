using System;

namespace Netfluid
{
    public class Filter:Route
    {
        #region GENERATORS

        public static new Filter New<T0>(Func<T0> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1>(Func<T0, T1> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2>(Func<T0, T1, T2> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3>(Func<T0, T1, T2, T3> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4>(Func<T0, T1, T2, T3, T4> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5>(Func<T0, T1, T2, T3, T4, T5> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6>(Func<T0, T1, T2, T3, T4, T5, T6> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7>(Func<T0, T1, T2, T3, T4, T5, T6, T7> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }

        public static new Filter New<T0>(Action<T0> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1>(Action<T0, T1> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2>(Action<T0, T1, T2> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3>(Action<T0, T1, T2, T3> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }
        public static new Filter New<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> f) { return new Filter() { MethodInfo = f.Method, Target = f.Target }; }

        #endregion
    }
}
