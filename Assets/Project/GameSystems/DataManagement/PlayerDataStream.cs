using System;
using LSL;
using UnityEngine;

namespace Project.GameSystems.DataManagement {
    public sealed class PlayerDataStream : MonoBehaviour {
        [SerializeField] private string _eventMarkerStreamName = "LSL4Unity.NavigationGame.Markers";
        [SerializeField] private string _signalStreamName = "LSL4Unity.NavigationGame.Signals";
        [SerializeField] private string _behaviorDataStreamName = "LSL4Unity.NavigationGame.BehaviorMarkers";

        private string _markerStreamType = "Markers";
        private string _signalStreamType = "signal";

        private StreamOutlet _eventMarkerStream;
        private StreamOutlet _behaviorDataStream;

        private readonly float[] _playerValues = new float[7];
        private readonly int[] _stepValue = new int[1];
        private readonly string[] _timeStamp = new string[1];

        [SerializeField] private bool _enableEventMarkerStream = true;
        [SerializeField] private bool _initialiseOnAwake = true;
        [SerializeField] private bool _isMarkerOnlyStream = true;

        private bool _areStreamsOpen;

        public bool AreStreamsOpen => _areStreamsOpen;

        private void Awake() {
            if (_initialiseOnAwake) {
                InitialiseStreams();
            }
        }

        public void InitialiseStreams() {
            if (_areStreamsOpen) return;

            // Used for WayFindingTask
            if (_enableEventMarkerStream) {
                var markerStreamHash = new Hash128();
                markerStreamHash.Append(_eventMarkerStreamName);
                markerStreamHash.Append(_markerStreamType);
                markerStreamHash.Append(gameObject.GetInstanceID());
                StreamInfo markerStreamInfo = new StreamInfo(_eventMarkerStreamName, _markerStreamType, 1,
                    LSL.LSL.IRREGULAR_RATE,
                    channel_format_t.cf_int32, markerStreamHash.ToString());
                _eventMarkerStream = new StreamOutlet(markerStreamInfo);
            }

            if (_isMarkerOnlyStream) {
                MarkerOnlyStreamInit();
            }
            else {
                SignalStreamInit();
            }

            _areStreamsOpen = true;
        }

        private void SignalStreamInit() {
            var signalStreamHash = new Hash128();
            signalStreamHash.Append(_signalStreamName);
            signalStreamHash.Append(_signalStreamType);
            signalStreamHash.Append(gameObject.GetInstanceID());
            // 7 values for player position, rotation, and distance to target
            StreamInfo signalStreamInfo = new StreamInfo(_signalStreamName, _signalStreamType, 8,
                4,
                channel_format_t.cf_float32, signalStreamHash.ToString());
            _behaviorDataStream = new StreamOutlet(signalStreamInfo);
        }

        private void MarkerOnlyStreamInit() {
            var markerStreamHash = new Hash128();
            markerStreamHash.Append(_behaviorDataStreamName);
            markerStreamHash.Append(_markerStreamType);
            markerStreamHash.Append(gameObject.GetInstanceID());
            // 7 values for player position, rotation, and distance to target
            StreamInfo markerStreamInfo = new StreamInfo(_behaviorDataStreamName, _markerStreamType, 1,
                LSL.LSL.IRREGULAR_RATE,
                channel_format_t.cf_string, markerStreamHash.ToString());
            _behaviorDataStream = new StreamOutlet(markerStreamInfo);
        }

        public void StreamPlayerData(IStepData data) {
            Vector3 playerPosition = data.GetPlayerPosition();
            _playerValues[0] = playerPosition.x;
            _playerValues[1] = playerPosition.y;
            _playerValues[2] = playerPosition.z;

            Vector3 playerRotation = data.GetPlayerRotation();
            _playerValues[3] = playerRotation.x;
            _playerValues[4] = playerRotation.y;
            _playerValues[5] = playerRotation.z;
            
            _playerValues[6] = data.GetDistanceToTarget();

            _stepValue[0] = data.GetStepCount();

            _behaviorDataStream.push_sample(_playerValues);
            _behaviorDataStream.push_sample(_stepValue);

            // Timestamp is only streamed if the stream is marker only (OpenViBE can't handle string values)
            if (_isMarkerOnlyStream) {
                _timeStamp[0] = data.GetTimeStamp();
                _behaviorDataStream.push_sample(_timeStamp);
            }
        }

        public void StreamEventMarker(int[] data) {
            _eventMarkerStream.push_sample(data);
        }

        public bool IsStreamOpen() {
            return !_eventMarkerStream.IsClosed;
        }

        public void SetBehaviorDataStreamMarkerOnly(bool isMarkerOnly) {
            _isMarkerOnlyStream = isMarkerOnly;
        }
    }
}