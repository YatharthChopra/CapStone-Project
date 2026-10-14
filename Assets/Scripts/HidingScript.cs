using UnityEngine;
using UnityEngine.InputSystem;

public class HidingScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool NearHidingPlace;
    private GameObject HidingPlace = null;
    public bool hiding = false;
    private Color defaultColor;
    void Start()
    {
        defaultColor = this.GetComponentInChildren<SpriteRenderer>().color;
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
    }

}
