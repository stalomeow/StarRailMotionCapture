using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HSR.MotionCapture.Editor
{
    internal static class UnityInternalAPI
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

        [Reflect(Member.Method, typeof(UnityEditor.AnimationMode), Name = "StartAnimationRecording")]
        private static Action AnimationMode_StartAnimationRecording;

        [Reflect(Member.Method, typeof(UnityEditor.AnimationMode), Name = "StopAnimationRecording")]
        private static Action AnimationMode_StopAnimationRecording;

        [Reflect(Member.Method, typeof(UnityEditor.AnimationMode), Name = "InAnimationRecording")]
        private static Func<bool> AnimationMode_InAnimationRecording;

        [Reflect(Member.Method, typeof(UnityEditor.EditorGUIUtility), Name = "HandleHorizontalSplitter")]
        private static Func<Rect, float, float, float, Rect> EditorGUIUtility_HandleHorizontalSplitter;

        [Reflect(Member.Method, typeof(UnityEditor.EditorGUIUtility), Name = "DrawHorizontalSplitter")]
        private static Action<Rect> EditorGUIUtility_DrawHorizontalSplitter;

        [Reflect(Member.EventAdd, typeof(UnityEditor.PrefabUtility), Name = "allowRecordingPrefabPropertyOverridesFor")]
        private static Action<Func<Object, bool>> PrefabUtility_allowRecordingPrefabPropertyOverridesFor_Add;

        [Reflect(Member.EventRemove, typeof(UnityEditor.PrefabUtility), Name = "allowRecordingPrefabPropertyOverridesFor")]
        private static Action<Func<Object, bool>> PrefabUtility_allowRecordingPrefabPropertyOverridesFor_Remove;

        public static class AnimationMode
        {
            public static void StartAnimationRecording() => AnimationMode_StartAnimationRecording();

            public static void StopAnimationRecording() => AnimationMode_StopAnimationRecording();

            public static bool InAnimationRecording() => AnimationMode_InAnimationRecording();
        }

        public static class EditorGUIUtility
        {
            public static Rect HandleHorizontalSplitter(Rect dragRect, float width, float minLeftSide, float minRightSide) =>
                EditorGUIUtility_HandleHorizontalSplitter(dragRect, width, minLeftSide, minRightSide);

            public static void DrawHorizontalSplitter(Rect dragRect) =>
                EditorGUIUtility_DrawHorizontalSplitter(dragRect);
        }

        public static class PrefabUtility
        {
            public static event Func<Object, bool> allowRecordingPrefabPropertyOverridesFor
            {
                add => PrefabUtility_allowRecordingPrefabPropertyOverridesFor_Add(value);
                remove => PrefabUtility_allowRecordingPrefabPropertyOverridesFor_Remove(value);
            }
        }
    }
}
