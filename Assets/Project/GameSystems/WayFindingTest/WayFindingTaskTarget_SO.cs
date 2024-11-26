using UnityEngine;

namespace Project.GameSystems.WayFindingTest {
    [CreateAssetMenu(fileName = "WayFindingTaskTarget", menuName = "ScriptableObjects/WayFindingTaskTarget", order = 2)]
    public class WayFindingTaskTarget_SO : ScriptableObject {
        [Min(0.1f)] public float ReachedTargetWaitTime = 3f;
        [Min(0.1f)] public float TextDuration = 3f;
        [Min(0.1f)] public float TaskTimer = 30f;
    }
}
