using UnityEngine;

public class RotationScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool GravityFlip = false;
    private Rigidbody2D rb;
    private Vector2 gravity;
    private float gravityScale;
    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
        gravityScale = rb.gravityScale;
        Physics2D.gravity = gravity;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (GravityFlip)
        {
            Physics2D.gravity = -transform.up * 9.8f;
            Debug.Log(Physics2D.gravity);
            //hit = Physics2D.Raycast(transform.position, raycastTarget.normalized, distance, mask);
        }
    }


    public void GravityFlipping (bool TurnOffGravity)
    {
        GravityFlip = TurnOffGravity;
        if (GravityFlip)
        {
            rb.gravityScale = 0;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }
}
