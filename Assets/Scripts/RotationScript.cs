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
    public GravityType gravType = GravityType.Normal;
    RaycastHit2D hit;
    LayerMask mask;
    public GameObject rotationPoint;
    public GameObject rotationPoint2;
    private bool isInverted = false;
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
            if(gravType == GravityType.Rotation1)
            {
                RotateAroundPoint(rotationPoint.transform.position, isInverted);
            }
            else if (gravType == GravityType.Rotation2)
            {
                RotateAroundPoint(rotationPoint2.transform.position, isInverted);
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
            RotationTrigger collVariables = collision.GetComponent<RotationTrigger>();
            
            gravType = collVariables.GravType;
            isInverted = collVariables.isInverted;
            switch (gravType)
            {
                case GravityType.UpsideDown: // this will set the players gravity to be upside down.
                    GravityFlipping(true);
                    transform.up = Vector3.down.normalized;
                    break;
                case GravityType.Normal: // this will set the players gravity to be right side up
                    GravityFlipping(false);
                    transform.up = Vector3.up.normalized;
                    break;
                case GravityType.Rotation1: //
                    GravityFlipping(true);
                    break;
                case GravityType.Rotation2:
                    GravityFlipping(true);
                    break;

            }

        }
    }
}
