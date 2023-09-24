using System;

namespace HSR.MotionCapture.Editor
{
    internal static partial class UnityInternalAPI
    {
        [Reflect(Member.Method, typeof(UnityEditor.AnimationMode), Name = "StartAnimationRecording")]
        private static Action AnimationMode_StartAnimationRecording;

        [Reflect(Member.Method, typeof(UnityEditor.AnimationMode), Name = "StopAnimationRecording")]
        private static Action AnimationMode_StopAnimationRecording;

        [Reflect(Member.Method, typeof(UnityEditor.AnimationMode), Name = "InAnimationRecording")]
        private static Func<bool> AnimationMode_InAnimationRecording;

        public static class AnimationMode
        {
            public static void StartAnimationRecording() => AnimationMode_StartAnimationRecording();

            public static void StopAnimationRecording() => AnimationMode_StopAnimationRecording();

            public static bool InAnimationRecording() => AnimationMode_InAnimationRecording();
        }
    }
}
