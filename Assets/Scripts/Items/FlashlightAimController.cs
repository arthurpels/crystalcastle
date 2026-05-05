using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightAimController : MonoBehaviour
{
    [SerializeField] private Transform target;
    void LateUpdate()
    {
        transform.position = target.position;
    }
}
