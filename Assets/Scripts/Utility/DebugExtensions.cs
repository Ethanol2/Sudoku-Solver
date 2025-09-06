using UnityEngine;

namespace EditorTools
{
    public static class DebugExtension
    {
        public static void Log(this MonoBehaviour origine, object message) => Log(origine, message, null);
        public static void Log(this MonoBehaviour origine, object message, Object context)
        {
            Debug.Log($"[{origine.GetType().BaseType.Name}] {message}", context);
        }

        public static void LogWarning(this MonoBehaviour origine, object message) => LogWarning(origine, message, null);
        public static void LogWarning(this MonoBehaviour origine, object message, Object context)
        {
            Debug.LogWarning($"[{origine.GetType().BaseType.Name}] {message}", context);
        }

        public static void LogError(this MonoBehaviour origine, object message) => LogError(origine, message, null);
        public static void LogError(this MonoBehaviour origine, object message, Object context)
        {
            Debug.LogError($"[{origine.GetType().BaseType.Name}] {message}", context);
        }
    }
}