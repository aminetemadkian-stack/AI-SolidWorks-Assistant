using System;
using System.Reflection;

namespace Ai.SolidWorksAssistant.Core
{
    /// <summary>
    /// Late-binding helpers for the SOLIDWORKS COM API.
    ///
    /// Design note: the whole add-in intentionally talks to SOLIDWORKS through IDispatch
    /// late binding (System.__ComObject InvokeMember) instead of referencing the
    /// SolidWorks.Interop.* assemblies. This keeps the build independent from
    /// non-redistributable interop DLLs while remaining 100% API-compatible at runtime.
    /// Every call site must be defensive (try/catch) — a failed late-bound call must
    /// never take SOLIDWORKS down.
    /// </summary>
    internal static class ComLate
    {
        public static object Call(object target, string member, params object[] args)
        {
            return target.GetType().InvokeMember(
                member,
                BindingFlags.InvokeMethod,
                null,
                target,
                args ?? new object[0]);
        }

        /// <summary>
        /// Invoke a COM method where some parameters must be marshaled by reference
        /// (e.g. RunMacro2's ByRef error code). <paramref name="byRefIndices"/> lists
        /// which argument slots are ByRef; the (possibly updated) args array is returned.
        /// </summary>
        public static object CallWithByRef(object target, string member, object[] args, int[] byRefIndices)
        {
            var modifier = new ParameterModifier(args.Length);
            foreach (var i in byRefIndices)
            {
                modifier[i] = true;
            }

            return target.GetType().InvokeMember(
                member,
                BindingFlags.InvokeMethod,
                null,
                target,
                args,
                new[] { modifier },
                null,
                null);
        }

        public static object Get(object target, string member)
        {
            return target.GetType().InvokeMember(
                member,
                BindingFlags.GetProperty,
                null,
                target,
                new object[0]);
        }

        public static void Set(object target, string member, object value)
        {
            target.GetType().InvokeMember(
                member,
                BindingFlags.SetProperty,
                null,
                target,
                new[] { value });
        }

        /// <summary>Call a member, returning null instead of throwing. Use for best-effort API calls.</summary>
        public static object TryCall(object target, string member, params object[] args)
        {
            try
            {
                return Call(target, member, args);
            }
            catch
            {
                return null;
            }
        }

        public static string TryString(object target, string member)
        {
            try
            {
                var v = Get(target, member);
                return v == null ? null : v.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static int? TryInt(object target, string member)
        {
            try
            {
                return Convert.ToInt32(Get(target, member));
            }
            catch
            {
                return null;
            }
        }

        public static object TryGet(object target, string member)
        {
            try
            {
                return Get(target, member);
            }
            catch
            {
                return null;
            }
        }
    }
}
