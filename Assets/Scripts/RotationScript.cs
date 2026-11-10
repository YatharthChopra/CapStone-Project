using UnityEngine;
using UnityEngine.UIElements;

public class RotationScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool GravityFlip = false;
    private Rigidbody2D rb;
    private Vector2 gravity;
    private float gravityScale;
    private Vector2 normalGrav;
    public int gravityFlipNum = 0;
    RaycastHit2D hit;
    LayerMask mask;
    public GameObject rotationPoint;
    public GameObject rotationPoint2;
    void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
        gravityScale = rb.gravityScale;
        //normalGrav = Physics2D.gravity;
        //inverted 
        //inverted = new bool[2];
        gravity = Physics2D.gravity;
        //Physics2D.gravity = gravity;
        mask = LayerMask.GetMask("Default");
    }
    class RotationPoint
    {
        public Transform point;
        public bool isInverted;

    }

    // Update is called once per frame
    void Update()
    {
        
        if (GravityFlip)
        {
            //rb.gravityScale = 0f;
            //gravity = Vector2.zero;
            //Physics2D.gravity = -transform.up * 9.8f;
            //Debug.Log(Physics2D.gravity);

            //hit = Physics2D.Raycast(transform.position, raycastTarget.normalized, distance, mask);
            if(gravityFlipNum == 1)
            {
                RotateAroundPoint(rotationPoint.transform.position, false);
            }
            else if (gravityFlipNum == 2)
            {
                RotateAroundPoint(rotationPoint2.transform.position, false);
            }
            
            
        }
    }

    private void RotateAroundPoint(Vector3 point, bool inverted)
    {
        Vector2 direction = (point - transform.position).normalized;
        
        if (inverted)
        {
            transform.up = direction;
        }
        else
        {
            transform.up = -direction;
        }
        

    }
    public void GravityFlipping (bool TurnOffGravity)
    {
        GravityFlip = TurnOffGravity;
        if (GravityFlip)
        {
            //Physics2D.gravity = Vector2.zero;
            //GravityFlip
            rb.gravityScale = 3f;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
        this.GetComponent<PlayerMovement>().GravityFlip = GravityFlip;
        
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "GravityFlipSegment")
        {

            GravityFlipping(!GravityFlip);

            if(collision.gameObject.name == "GravityCollider1")
            {
                gravityFlipNum = 1;
            }
            else if(collision.gameObject.name == "GravityCollider3")
            {
                gravityFlipNum = 2;
            }
        }
    }
}
