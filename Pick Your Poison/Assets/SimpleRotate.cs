using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRotate : MonoBehaviour
{

    [SerializeField]
    private Transform target;

    [SerializeField]
    private float rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(target.position, Vector3.up, rotationSpeed * Time.deltaTime * 10);
    }
}
