using UnityEngine;

namespace Platformer.UI
{
    /// <summary>
    /// Spawns floating combat texts in world space for damage, critical hits, parries, and deflects.
    /// </summary>
    public static class FloatingTextManager
    {
        public static void ShowDamage(Vector3 position, int amount, bool isCritical = false)
        {
            GameObject go = new GameObject("FloatingDamageText");
            go.transform.position = position + new Vector3(0f, 0.5f, 0f);

            FloatingText ft = go.AddComponent<FloatingText>();
            Color col = isCritical ? new Color(1f, 0.82f, 0.1f) : Color.white;
            string text = isCritical ? $"CRIT! -{amount}" : $"-{amount}";
            ft.Initialize(text, col, isCritical ? 1.25f : 1.0f);
        }

        public static void ShowPlayerDamage(Vector3 position, int amount)
        {
            GameObject go = new GameObject("FloatingPlayerDamageText");
            go.transform.position = position + new Vector3(0f, 0.5f, 0f);

            FloatingText ft = go.AddComponent<FloatingText>();
            ft.Initialize($"-{amount}", new Color(1f, 0.3f, 0.3f), 1.1f);
        }

        public static void ShowDeflect(Vector3 position)
        {
            GameObject go = new GameObject("FloatingDeflectText");
            go.transform.position = position + new Vector3(0f, 0.6f, 0f);

            FloatingText ft = go.AddComponent<FloatingText>();
            ft.Initialize("DEFLECT!", new Color(0.2f, 0.95f, 1f), 1.3f);
        }

        public static void ShowParry(Vector3 position)
        {
            GameObject go = new GameObject("FloatingParryText");
            go.transform.position = position + new Vector3(0f, 0.6f, 0f);

            FloatingText ft = go.AddComponent<FloatingText>();
            ft.Initialize("PARRY!", new Color(1f, 0.95f, 0.2f), 1.35f);
        }
    }
}
