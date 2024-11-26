using System.Collections.Generic;
using LSL4Unity.Utils;
using UnityEngine;

namespace Project.GameSystems.DataManagement {
    // Based on the PoseOutlet example from LSL4Unity
    public class PlayerPoseOutlet : AFloatOutlet {
        public enum PoseFormat {
            PlayerData
        }

        public PoseFormat _transformFormat = PoseFormat.PlayerData;

        [SerializeField] private Transform _playerCamera;

        public override List<string> ChannelNames {
            get {
                List<string> chanNames = new List<string>();

                if (_transformFormat == PoseFormat.PlayerData) {
                    chanNames.AddRange(new string[] { "PosX", "PosY", "PosZ" });
                    chanNames.AddRange(new string[] { "RotX", "RotY", "RotZ" });
                }

                return chanNames;
            }
        }

        protected override void ExtendHash(Hash128 hash) {
            hash.Append(_transformFormat.ToString());
        }

        protected override bool BuildSample() {
            if (_transformFormat == PoseFormat.PlayerData) {
                var position = gameObject.transform.localPosition;
                var rotation = gameObject.transform.localRotation;
                sample[0] = position.x;
                sample[1] = position.y;
                sample[2] = position.z;
                // Use Camera object rotation
                sample[3] = _playerCamera.localRotation.x;
                sample[4] = rotation.y;
                sample[5] = rotation.z;
                return true;
            }

            return true;
        }
    }
}