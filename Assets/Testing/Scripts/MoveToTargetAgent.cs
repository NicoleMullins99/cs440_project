using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MoveToTargetAgent : Agent {
    [SerializeField] private EnvTest _envTest;
    [SerializeField] private Transform _targetTransform;
    [SerializeField] private Rigidbody _rigidbody;

    [SerializeField] private Vector3 _agentStartLocalPosition = Vector3.zero;
    [SerializeField] private float _moveSpeed = 5f;
    
    public override void CollectObservations(VectorSensor sensor) {
        // 6 float values passed
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(_targetTransform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // Receive 2 continuous actions
        // 1 for x axis
        // 1 for z axis
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        
        _rigidbody.Move(transform.position + (new Vector3(moveX,0, moveZ) * _moveSpeed * Time.deltaTime), Quaternion.identity);
    }

    public override void OnEpisodeBegin() {
        base.OnEpisodeBegin();
        transform.localPosition = new Vector3(Random.Range(-4, 4), 0.5f, Random.Range(-4, 1));
        _targetTransform.localPosition = new Vector3(Random.Range(-4, 4), 0.5f, Random.Range(1, 4));
        _rigidbody.Move(transform.position, Quaternion.identity);
    }

    public void OnCollisionEnter(Collision other) {
        if (other.gameObject.CompareTag("Wall")) {
            SetReward(-1f);
            EndEpisode();
            _envTest.Fail();
        } else if (other.gameObject.CompareTag("Target")) {
            SetReward(1f);
            EndEpisode();
            _envTest.Success();
        }
    }
}
