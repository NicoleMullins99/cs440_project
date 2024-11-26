using System.Collections.Generic;
using UnityEngine;

namespace Project.GameSystems.WayFindingTest {
    [CreateAssetMenu(fileName = "WayFindingTask", menuName = "ScriptableObjects/WayFindingTask", order = 1)]
    public class WF_TaskSO : ScriptableObject
    {
        public TaskTargetEnum SpawnPoint;
        public List<WayFindingTask.TaskSettings> Tasks;
    }
}
