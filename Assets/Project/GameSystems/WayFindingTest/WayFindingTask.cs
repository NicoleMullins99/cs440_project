using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.GameSystems.WayFindingTest {
    public class WayFindingTask : MonoBehaviour {
        [SerializeField] private WF_TaskSO _wf_Task;

        public List<TaskSettings> TaskList => _wf_Task.Tasks;
        public TaskTargetEnum SpawnPoint => _wf_Task.SpawnPoint;
        public Dictionary<TaskTargetEnum, TaskTarget> TaskTargetLookup;

        private void Awake() {
            TaskTargetLookup = new Dictionary<TaskTargetEnum, TaskTarget>();
            var targets = GetComponentsInChildren<TaskTarget>();

            foreach (var target in targets) {
                TaskTargetLookup.Add(target.TargetEnum, target);
            }
        }

        public bool TryGetTask(int taskIndex, out TaskSettings task) {
            if (taskIndex >= TaskList.Count || taskIndex < 0) {
                task = default;
                return false;
            }
            task = TaskList[taskIndex];
            return true;
        }
        
        public bool TryGetTaskTarget(TaskTargetEnum targetEnum, out TaskTarget taskTarget) {
            return TaskTargetLookup.TryGetValue(targetEnum, out taskTarget);
        }
        
        public void SetTask(WF_TaskSO task) {
            _wf_Task = task;
        }


        [Serializable]
        public struct TaskSettings {
            public TaskTargetEnum TaskTarget;
            [TextArea] public string DisplayText;
            public bool ControlState;
            public WayFindingTaskTarget_SO TaskTargetSettings;
        }
    }
}