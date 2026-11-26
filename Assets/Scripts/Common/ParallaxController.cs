using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Common
{
    /// <summary>
    /// Master controller managing all parallax layers in the active scene.
    /// Provides central control over background depth, visibility, and camera alignment.
    /// </summary>
    public class ParallaxController : MonoBehaviour
    {
        [Header("Layer References")]
        [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

        public IReadOnlyList<ParallaxLayer> Layers => layers;

        private void Reset()
        {
            layers.Clear();
            layers.AddRange(GetComponentsInChildren<ParallaxLayer>(true));
        }

        private void Awake()
        {
            if (layers.Count == 0)
            {
                layers.AddRange(GetComponentsInChildren<ParallaxLayer>(true));
            }
        }

        public void RegisterLayer(ParallaxLayer layer)
        {
            if (layer != null && !layers.Contains(layer))
            {
                layers.Add(layer);
            }
        }
    }
}
