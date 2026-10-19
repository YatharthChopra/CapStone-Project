using System.Collections.Generic;
using Unity.VisualScripting;
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


    public bool InView = false;
    [SerializeField] private List<GameObject> EnemiesList = new List<GameObject>();
    [SerializeField] private float detection = 0;
    [SerializeField] private float maxDetection = 100;
    public float detectionScaling = 1;
    //private PlayerMovement movementScript = this.movementScript;

    // Added so the enemy scripts can read the detection meter (they can't see private variables)
    public float Detection { get { return detection; } }
    public float MaxDetection { get { return maxDetection; } }

    // Added so an enemy can push the meter up by itself (Glaring Eyes use this to fill it faster)
    public void AddDetection(float amount)
    {
        detection = Mathf.Min(detection + amount, maxDetection);
    }

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

        //detectioncode
        
        if (hiding)
        {
            if (detection > 0)
            {
                detection -= 2 * detectionScaling * Time.deltaTime;
            }
        }
        else if(InView == true)
        {
            foreach(GameObject enemyObject in EnemiesList)
            {
                detection += (1 / (Vector3.Distance(enemyObject.transform.position, transform.position))) * Time.deltaTime * detectionScaling;
                Debug.Log((1/(Vector3.Distance(enemyObject.transform.position, transform.position))) * detectionScaling);
            }
            bool seenByAnyone = false;   // (added) stays false if every enemy is behind a wall
            foreach(GameObject enemyObject in EnemiesList)
            {
                // (added) an enemy behind a wall can't see us, so it doesn't fill the meter
                if (!LineOfSight.CanSeeBody(enemyObject.transform.position, GetComponent<Collider2D>())) continue;
                seenByAnyone = true;

                detection += (1 / (Vector3.Distance(enemyObject.transform.position, transform.position))) * Time.deltaTime * detectionScaling;
                Debug.Log((1/(Vector3.Distance(enemyObject.transform.position, transform.position))) * detectionScaling);
            }

            // (added) if nobody can actually see us, the meter goes down like it does out of view
            if (!seenByAnyone && detection > 0)
            {
                detection -= 1 * Time.deltaTime * detectionScaling;
            }
        }
        else
        {
            if (detection > 0)
            {
                detection -= 1 * Time.deltaTime*detectionScaling;
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

        if(collision.tag == "EnemyView")
        {
            InView = true;
            EnemiesList.Add(collision.GetComponentInParent<Transform>().gameObject);
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
        if (collision.tag == "EnemyView")
        {
            EnemiesList.Remove(collision.GetComponentInParent<Transform>().gameObject);
            if (EnemiesList.Count == 0)
            {
                InView = false;
            }
        }
    }

}
