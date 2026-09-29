using UnityEngine;

public class HeldGrabPoint : MonoBehaviour
{
    public enum Hand
    {
        Any,
        Left,
        Right
    }

    [SerializeField] private Hand hand = Hand.Any;
    [SerializeField] private bool matchHoldRotation = true;

    public Hand GrabHand
    {
        get { return hand; }
    }

    public bool MatchHoldRotation
    {
        get { return matchHoldRotation; }
    }
}
