using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.GameSystems.DataManagement {
    [Serializable]
    public class SaveFileData : SaveFileDataBase {
        public string MapShowcase;
        public Vector3 MazeTargetPosition;
        public List<StepData> Steps = new List<StepData>();

        public override IStepData GetStep(int index) {
            return Steps[index];
        }

        public override int GetStepCount() {
            return Steps.Count;
        }
    }

    [Serializable]
    public struct StepData : IStepData {
        public int StepCount;
        public float DistanceToTarget;
        public string TimeStamp;
        public Vector3 PlayerPosition;
        public Vector3 PlayerRotation;

        public int GetStepCount() {
            return StepCount;
        }

        public float GetDistanceToTarget() {
            return DistanceToTarget;
        }

        public string GetTimeStamp() {
            return TimeStamp;
        }

        public Vector3 GetPlayerPosition() {
            return PlayerPosition;
        }

        public Vector3 GetPlayerRotation() {
            return PlayerRotation;
        }
    }

    [Serializable]
    public class SaveFileDataWF : SaveFileDataBase {
        public int SuccessCount;
        public int FailCount;
        public List<StepDataWF> Steps = new List<StepDataWF>();

        public override IStepData GetStep(int index) {
            return Steps[index];
        }

        public override int GetStepCount() {
            return Steps.Count;
        }
    }

    [Serializable]
    public struct StepDataWF : IStepData {
        public int StepCount;
        public float DistanceToTarget;
        public string TimeStamp;
        public Vector3 PlayerPosition;
        public Vector3 PlayerRotation;
        public string TaskTarget;

        public int GetStepCount() {
            return StepCount;
        }

        public float GetDistanceToTarget() {
            return DistanceToTarget;
        }

        public string GetTimeStamp() {
            return TimeStamp;
        }

        public Vector3 GetPlayerPosition() {
            return PlayerPosition;
        }

        public Vector3 GetPlayerRotation() {
            return PlayerRotation;
        }
    }

    [Serializable]
    public abstract class SaveFileDataBase {
        public string RunType;
        public string MapName;
        public float Duration;
        public abstract IStepData GetStep(int index);
        public abstract int GetStepCount();
    }

    public interface IStepData {
        public int GetStepCount();
        public float GetDistanceToTarget();
        public string GetTimeStamp();
        public Vector3 GetPlayerPosition();
        public Vector3 GetPlayerRotation();
    }
}