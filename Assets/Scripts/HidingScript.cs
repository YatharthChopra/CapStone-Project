using UnityEngine;
using UnityEngine.InputSystem;

public class HidingScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool NearHidingPlace;
    public bool NearCrouchPlace;
    private GameObject HidingPlace = null;
    public bool hiding = false;
    private bool crouching = false;
    //private PlayerMovement movementScript = this.movementScript;

    private Color defaultColor;
    //private bool
    void Start()
    {
        defaultColor = this.GetComponentInChildren<SpriteRenderer>().color;
        crouching = this.GetComponent<PlayerMovement>().IsCrouching;
    }

    // Update is called once per frame
    void Update()
    {
        if (NearHidingPlace)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                
                if (hiding)
                {
                    Unhide();
                }
                else
                {
                    this.GetComponentInChildren<SpriteRenderer>().color = Color.ghostWhite;
                    hiding = true;
                }
                    
            }

        }

        if (NearCrouchPlace)
        {
            crouching = this.GetComponent<PlayerMovement>().IsCrouching;
            if (hiding)
            {
                if (!crouching)
                {
                    Unhide();
                }
            }
            else
            {
                if (crouching)
                {
                    this.GetComponentInChildren<SpriteRenderer>().color = Color.ghostWhite;
                    hiding = true;
                }
                
            }
            
        }
    }


    void Unhide()
    {
        this.GetComponentInChildren<SpriteRenderer>().color = defaultColor;
        hiding = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "HidingSpot")
        {
            NearHidingPlace = true; //when near an alcove this saves that alcove and registers the player as near it.
            HidingPlace = collision.gameObject;
        }

        if(collision.tag == "CrouchingSpot")
        {
            NearCrouchPlace = true;
        }
        
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "HidingSpot")
        {
            NearHidingPlace = false;
            HidingPlace = null;
            if (hiding)
            {
                Unhide();
            }
        }

        if (collision.tag == "CrouchingSpot")
        {
            NearCrouchPlace = false;
            HidingPlace = null;
            if (hiding)
            {
                Unhide();
            }
        }
    }

}
