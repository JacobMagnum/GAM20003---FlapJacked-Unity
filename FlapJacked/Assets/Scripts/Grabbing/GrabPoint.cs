using UnityEngine;

public class GrabPoint : MonoBehaviour
{
    public enum Hand
    {
        Any,
        Left,
        Right
    }

    [SerializeField] private Hand hand = Hand.Any;

    public Hand GrabHand => hand;
}