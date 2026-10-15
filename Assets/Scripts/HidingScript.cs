using UnityEngine;
using UnityEngine.InputSystem;

public class HidingScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool NearHidingPlace;
    public bool NearCrouchPlace;
    private GameObject HidingPlace = null;
    [SerializeField] public bool hiding = false;
    [SerializeField] private bool crouching = false;
    [SerializeField] private bool crouchHiding = false;

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
            // this checks if they are near a hiding place
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                // when they press e it toggles their hiding state
                if (hiding)
                {
                    Unhide();
                }
                else
                {
                    Hide();
                }
                    
            }

        }

        if (NearCrouchPlace)
        {
            // this checks if their near something they can crouch behind
            crouching = this.GetComponent<PlayerMovement>().IsCrouching; // this checks if they are crouching, can be more optomized.
            if (crouchHiding && !crouching)
            {
                Unhide();
                crouchHiding = false;
            }
            else if (!crouchHiding && crouching)
            {
                crouchHiding = true;
                Hide();
            }
            
        }
    }

    void Hide()
    {
        this.GetComponentInChildren<SpriteRenderer>().color = Color.ghostWhite;
        hiding = true;
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
            if (hiding && !crouchHiding)
            {
                Unhide();
            }
        }

        if (collision.tag == "CrouchingSpot")
        {
            NearCrouchPlace = false;
            HidingPlace = null;
            if (crouchHiding)
            {
                crouchHiding = false;
                Unhide();
            }
        }
    }

}
