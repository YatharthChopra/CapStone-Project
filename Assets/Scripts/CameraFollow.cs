using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraFollow : MonoBehaviour
{
    // Alice Bellefeuille
    private Transform target;

    private Rigidbody2D rb;

    public Vector3 offset;

    public float speed = 1;

    private bool firstrun = true; // have to do this because of script order stuff
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
    }
    // Update is called once per frame
    void Update()
    {
        if (firstrun)
        { // this changes the position of the camera to the players. i should find a better way but the player gets moved on start by another script so it doesn't work in start. - alice
            if(rb.constraints != RigidbodyConstraints2D.FreezeAll)
            {
                Vector3 raycastTarget = ((target.position+offset) - transform.position);
                RaycastHit2D hit;
                //Debug.Log(raycastTarget.);
                LayerMask mask = LayerMask.GetMask("CameraColliders");
                //Debug.Log(mask);

               

                float distance = (transform.position - (target.position + offset + (Vector3.back * 10))).magnitude;

                hit = Physics2D.Raycast(transform.position, raycastTarget.normalized, distance, mask);
                if (hit.point != Vector2.zero)
                {
                    transform.position = hit.point;
                }
                else
                {
                    transform.position = target.position + offset;
                }
                transform.position += (Vector3.back * 10);



                Debug.Log(hit.point);
            }
            firstrun = false;
            
        }

        
        

        Vector3 targetpos = target.position + offset;
        rb.linearVelocity = (targetpos - transform.position) * speed;
    }
}
