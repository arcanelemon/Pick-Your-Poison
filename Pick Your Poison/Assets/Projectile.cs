using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float travelDistance;

    private bool active;

    private Rigidbody rb;

    void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (active)
        {
            rb.velocity = transform.forward * speed;
            travelDistance += speed * Time.deltaTime;

            if (travelDistance > 100)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

    public void Initialize(float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;

        active = true;
    }
}
