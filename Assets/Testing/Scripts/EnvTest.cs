using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvTest : MonoBehaviour {
    [SerializeField] private MeshRenderer _floorMeshRenderer;

    [SerializeField] private Material _successMaterial;
    [SerializeField] private Material _failMaterial;
    [SerializeField] private Material _normalMaterial;

    public void Success() {
        _floorMeshRenderer.materials = new[] {_successMaterial};
    }
    
    public void Fail() {
        _floorMeshRenderer.materials = new[] {_failMaterial};
    }
    
    public void Normal() {
        _floorMeshRenderer.materials = new[] {_normalMaterial};
    }
}