using UnityEngine;

public class RotationTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //public int gravityNumber;
    public bool isInverted;
    public bool isEnabler;
    public GravityType GravType;
}
public enum GravityType
{
    Normal,
    UpsideDown,
    Rotation1,
    Rotation2,
}
