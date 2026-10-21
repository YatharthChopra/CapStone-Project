using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class FlashlightScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // i should make a manager so that the scripts aren't all calling their own updates.
    Keyboard kb;
    bool flashlightOn = true;
    private GameObject flashlightObject;
    void Start()
    {
        kb = Keyboard.current;
        flashlightObject = GetComponentInChildren<Light2D>().gameObject; // this gets the flashlight object.
    }

    // Update is called once per frame
    void Update()
    {
        if (kb.fKey.wasPressedThisFrame)
        {
            flashlightOn = !flashlightOn;
            flashlightObject.SetActive(flashlightOn);
        }
    }
}
