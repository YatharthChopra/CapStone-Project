using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashlightScript : MonoBehaviour
{
    // i should make a manager so that the scripts aren't all calling their own updates.
    bool flashlightOn = true;
    private GameObject flashlightObject;
    void Start()
    {
        flashlightObject = GetComponentInChildren<Light2D>().gameObject; // this gets the flashlight object.
    }

    // Update is called once per frame
    void Update()
    {
        // The key and button are set in GameControls (X on the keyboard, B / Circle on a controller)
        if (GameControls.FlashlightTogglePressed)
        {
            flashlightOn = !flashlightOn;
            flashlightObject.SetActive(flashlightOn);
        }

        // "Change flashlight type" is on the control list (S / R2) but we only have one kind of flashlight so far.
        // When there is a second one, switch to it here.
        if (GameControls.FlashlightTypePressed)
        {
            ChangeFlashlightType();
        }
    }

    // Nothing to change to yet
    private void ChangeFlashlightType()
    {
    }
}
