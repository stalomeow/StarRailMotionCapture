using System;
using Object = UnityEngine.Object;

namespace HSR.MotionCapture.Editor
{
    internal static partial class UnityInternalAPI
    {
        [Reflect(Member.EventAdd, typeof(UnityEditor.PrefabUtility), Name = "allowRecordingPrefabPropertyOverridesFor")]
        private static Action<Func<Object, bool>> PrefabUtility_allowRecordingPrefabPropertyOverridesFor_Add;

        [Reflect(Member.EventRemove, typeof(UnityEditor.PrefabUtility), Name = "allowRecordingPrefabPropertyOverridesFor")]
        private static Action<Func<Object, bool>> PrefabUtility_allowRecordingPrefabPropertyOverridesFor_Remove;

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
