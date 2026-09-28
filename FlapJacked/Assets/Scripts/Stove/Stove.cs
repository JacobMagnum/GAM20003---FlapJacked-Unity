using UnityEngine;
public class Stove : MonoBehaviour
{
    [SerializeField] private StoveKnob frontLeft;
    [SerializeField] private StoveKnob frontRight;
    [SerializeField] private StoveKnob backLeft;
    [SerializeField] private StoveKnob backRight;
    public StoveKnob FrontLeft => frontLeft;
    public StoveKnob FrontRight => frontRight;
    public StoveKnob BackLeft => backLeft;
    public StoveKnob BackRight => backRight;
    public float FrontLeftHeat => frontLeft != null ? frontLeft.Heat : 0f;
    public float FrontRightHeat => frontRight != null ? frontRight.Heat : 0f;
    public float BackLeftHeat => backLeft != null ? backLeft.Heat : 0f;
    public float BackRightHeat => backRight != null ? backRight.Heat : 0f;
}