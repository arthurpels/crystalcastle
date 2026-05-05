using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PosConstrant : MonoBehaviour
{   
    [SerializeField] private Transform targetPosition;


    void LateUpdate() {
        transform.position = targetPosition.transform.position;
    }
}
