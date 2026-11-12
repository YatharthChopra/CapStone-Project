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
        if (GameControls.FlashlightTypePressed)
        {
            ChangeFlashlightType();
        }

        // Point the flashlight the way Nin is walking (left / right)
        float x = GameControls.MoveX;
        if (x < 0f) flashlightObject.transform.eulerAngles = new Vector3(0, 0, -270);
        else if (x > 0f) flashlightObject.transform.eulerAngles = new Vector3(0, 0, -90);
    }

    // Nothing to change to yet
    private void ChangeFlashlightType()
    {
    }
}