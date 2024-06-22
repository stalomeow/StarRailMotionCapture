using System;
using UnityEngine;

namespace HSR.MotionCapture.Editor
{
    internal static partial class UnityInternalAPI
    {
        [Reflect(Member.Method, typeof(UnityEditor.EditorGUIUtility), Name = "HandleHorizontalSplitter")]
        private static Func<Rect, float, float, float, Rect> EditorGUIUtility_HandleHorizontalSplitter;

        [Reflect(Member.Method, typeof(UnityEditor.EditorGUIUtility), Name = "DrawHorizontalSplitter")]
        private static Action<Rect> EditorGUIUtility_DrawHorizontalSplitter;

        public static class EditorGUIUtility
        {
            public static Rect HandleHorizontalSplitter(Rect dragRect, float width, float minLeftSide, float minRightSide) =>
                EditorGUIUtility_HandleHorizontalSplitter(dragRect, width, minLeftSide, minRightSide);

            public static void DrawHorizontalSplitter(Rect dragRect) =>
                EditorGUIUtility_DrawHorizontalSplitter(dragRect);
        }
    }
}
