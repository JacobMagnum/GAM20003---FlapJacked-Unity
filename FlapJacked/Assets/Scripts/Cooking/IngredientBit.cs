using UnityEngine;

public enum IngredientKind
{
    Flour,
    Milk,
    EggWhite,
    EggYolk,
    Batter,
    Butter
}

public class IngredientBit : MonoBehaviour
{
    [SerializeField] private IngredientKind kind = IngredientKind.Flour;
    [SerializeField] private float amount = 1f;
    [SerializeField][Range(0f, 1f)] private float quality = 1f;

    public IngredientKind Kind
    {
        get { return kind; }
    }

    public float Amount
    {
        get { return amount; }
    }

    public float Quality
    {
        get { return quality; }
    }

    public void Configure(IngredientKind ingredientKind, float ingredientAmount, float mixQuality)
    {
        kind = ingredientKind;
        amount = Mathf.Max(0.01f, ingredientAmount);
        quality = Mathf.Clamp01(mixQuality);
    }
}
