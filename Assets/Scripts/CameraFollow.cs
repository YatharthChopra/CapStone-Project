using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Transform target;

    private Rigidbody2D rb;

    public Vector3 offset;

    public float speed = 1;
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetpos = target.position + offset;
        rb.linearVelocity = (targetpos - transform.position) * speed;
    }
}
