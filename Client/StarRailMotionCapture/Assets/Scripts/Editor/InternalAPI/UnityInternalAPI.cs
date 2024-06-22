using System;
using System.Reflection;

namespace HSR.MotionCapture.Editor
{
    internal static partial class UnityInternalAPI
    {
        private enum Member
        {
            Method = 0,
            EventAdd = 1,
            EventRemove = 2,
        }

        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
        private sealed class ReflectAttribute : Attribute
        {
            public Member MemberType { get; }

            public Type DeclaringType { get; }

            public string Name { get; set; }

            public ReflectAttribute(Member member, Type declaringType)
            {
                MemberType = member;
                DeclaringType = declaringType;
            }
        }

        static UnityInternalAPI()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            foreach (var field in typeof(UnityInternalAPI).GetFields(flags))
            {
                var binding = field.GetCustomAttribute<ReflectAttribute>();

                if (binding == null)
                {
                    continue;
                }

                string memberName = binding.Name ?? field.Name;
                Type declaringType = binding.DeclaringType;
                MethodInfo methodInfo = binding.MemberType switch
                {
                    Member.Method => declaringType.GetMethod(memberName, flags),
                    Member.EventAdd => declaringType.GetEvent(memberName, flags).AddMethod,
                    Member.EventRemove => declaringType.GetEvent(memberName, flags).RemoveMethod,
                    _ => throw new NotSupportedException()
                };

                object @delegate = methodInfo.CreateDelegate(field.FieldType, null);
                field.SetValue(null, @delegate);
            }
        }
    }
}
