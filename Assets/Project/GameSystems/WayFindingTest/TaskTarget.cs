using System;
using UnityEngine;

namespace Project.GameSystems.WayFindingTest {
    public class TaskTarget : MonoBehaviour {
        [SerializeField] private TaskTargetEnum _targetEnum;
        [SerializeField] private BoxCollider _triggerCollider;
        [SerializeField] private BoxCollider _boxCollider;
        [SerializeField] private BoxCollider _sensorCollider;
        [SerializeField] private Transform _signTransform;

        [SerializeField] private bool _isCurrentTarget;

        public event Action OnReachedTarget;

        public TaskTargetEnum TargetEnum => _targetEnum;
        public Transform SignTransform => _signTransform;
        public bool IsTarget => _isCurrentTarget;

        public void EnableTarget() {
            _triggerCollider.enabled = true;
            _boxCollider.enabled = true;
            _sensorCollider.enabled = true;
            _isCurrentTarget = true;
        }

        public void DisableTarget() {
            _triggerCollider.enabled = false;
            _sensorCollider.enabled = false;
            _isCurrentTarget = false;
            
            // Disable the box collider after 1 second, to prevent the player gliding away from the target
            Invoke(nameof(DelayDisableBoxCollider), 1f);
        }
        
        private void DelayDisableBoxCollider() {
            _boxCollider.enabled = false;
        }

        private void OnTriggerEnter(Collider other) {
            if (other.CompareTag("Player")) {
                OnReachedTarget?.Invoke();
            }
        }
    }
}