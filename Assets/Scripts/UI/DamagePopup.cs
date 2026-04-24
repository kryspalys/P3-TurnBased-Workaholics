using UnityEngine;
using TMPro;

/// <summary>
/// Controls the visual behavior of a floating text mesh, animating its movement, scale, and opacity over time.
/// </summary>
/// <remarks>
/// <para>This script manipulates the object's <see cref="Transform"/> and <see cref="Color"/> alpha channel every frame until destruction.</para>
/// <include file='ExternalDocs.xml' path='docs/members[@name="DamagePopup"]/DamagePopup/*'/>
/// </remarks>
public class DamagePopup : MonoBehaviour
{
    [Header("UI Dependencies")]
    /// <summary>The TextMeshPro component responsible for physically rendering the alphanumeric characters.</summary>
    [SerializeField] private TMP_Text textMesh;

    [Header("Animation Settings")]
    /// <summary>The vertical translation speed applied per second.</summary>
    [SerializeField] private float floatSpeed = 2f;
    /// <summary>The duration in seconds the text remains fully opaque before initiating the fade sequence.</summary>
    [SerializeField] private float disappearTimer = 0.8f;
    /// <summary>The mathematical rate at which the alpha channel depletes once fading begins.</summary>
    [SerializeField] private float fadeSpeed = 3f;

    [Header("Styling")]
    /// <summary>The color applied to standard calculated damage numbers.</summary>
    [SerializeField] private Color normalColor = Color.white;
    /// <summary>The visual color applied to damage numbers resulting from a critical hit.</summary>
    [SerializeField] private Color critColor = Color.red;
    /// <summary>The visual color applied to hit point restoration numbers.</summary>
    [SerializeField] private Color healColor = Color.green;
    /// <summary>The visual color applied to mitigated damage numbers.</summary>
    [SerializeField] private Color mitigationColor = new Color(0.2f, 0.6f, 1f); // Default to a cyan/blue
    /// <summary>The scale multiplier applied to the font size for mitigated damage text.</summary>
    [SerializeField, Range(0.1f, 1f)] private float mitigationTextScale = 0.20f;

    /// <summary>Internal tracking variable for the current applied text color.</summary>
    private Color textColor;

    /// <summary>
    /// Validates internal UI references prior to execution.
    /// </summary>
    /// <exception cref="System.NullReferenceException">Thrown if the TextMeshPro component is missing.</exception>
    private void Awake()
    {
        if (textMesh == null)
        {
            throw new System.NullReferenceException("DamagePopup requires a TMP_Text component assignment.");
        }
    }

    /// <summary>
    /// Initializes the visual representation of the text based on incoming damage calculations.
    /// </summary>
    /// <param name="damageAmount">The exact numerical damage to process and display.</param>
    /// <param name="isCrit">If <c>true</c>, applies the aggressive critical style formatting.</param>
    public void Setup(float damageAmount, bool isCrit)
    {
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        if (isCrit)
        {
            textMesh.fontSize *= 1.5f;
            textColor = critColor;
            textMesh.text += "!";
        }
        else
        {
            textColor = normalColor;
        }

        ApplyStyling();
    }

    /// <summary>
    /// Initializes the visual representation of the text for restorative actions.
    /// </summary>
    /// <param name="healAmount">The numerical amount of health restored.</param>
    public void SetupHeal(float healAmount)
    {
        textMesh.text = "+" + Mathf.RoundToInt(healAmount).ToString();
        textColor = healColor;

        ApplyStyling();
    }

    /// <summary>
    /// Initializes the visual representation of the text for blocked damage.
    /// </summary>
    /// <param name="mitigationPercentage">The percentage of damage blocked by a defensive stance.</param>
    public void SetupMitigation(float mitigationPercentage)
    {
        // Formats the text to read exactly like: "50% blocked"
        textMesh.text = Mathf.RoundToInt(mitigationPercentage).ToString() + "% blocked";
        textColor = mitigationColor;

        // Shrinks the font size using the Inspector-defined multiplier
        textMesh.fontSize *= mitigationTextScale;

        ApplyStyling();
    }

    /// <summary>
    /// Applies the calculated text color and introduces a slight random positional offset.
    /// </summary>
    private void ApplyStyling()
    {
        textMesh.color = textColor;
        transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), 0);
    }

    /// <summary>
    /// Executes coordinate and alpha modifications during the primary physics-independent loop.
    /// </summary>
    private void Update()
    {
        transform.position += new Vector3(0, floatSpeed * Time.deltaTime, 0);

        disappearTimer -= Time.deltaTime;

        if (disappearTimer < 0)
        {
            float alpha = textMesh.color.a - (fadeSpeed * Time.deltaTime);
            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);

            if (alpha <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}