using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Animacio dels botons fets amb UIKit: creix una mica en passar-hi el ratoli i s'enfonsa en clicar.
// Fa servir temps sense escalar perque funcioni tambe amb el joc en pausa.
public class BotoAnimat : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform cara;
    public Image imatgeCara;
    public Color color = Color.white;
    public float escalaHover = 1.04f;
    public float enfonsar = 6f;

    bool sobre, premut;
    float escala = 1f, baixada = 0f;

    void OnDisable()
    {
        sobre = premut = false;
        escala = 1f;
        baixada = 0f;
        transform.localScale = Vector3.one;
        if (cara != null) cara.anchoredPosition = Vector2.zero;
        if (imatgeCara != null) imatgeCara.color = color;
    }

    void Update()
    {
        float k = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
        escala = Mathf.Lerp(escala, sobre ? escalaHover : 1f, k);
        baixada = Mathf.Lerp(baixada, premut ? enfonsar : 0f, k);
        transform.localScale = Vector3.one * escala;
        if (cara != null) cara.anchoredPosition = Vector2.down * baixada;
        if (imatgeCara != null)
            imatgeCara.color = Color.Lerp(imatgeCara.color, sobre ? Color.Lerp(color, Color.white, 0.14f) : color, k);
    }

    public void OnPointerEnter(PointerEventData e) { sobre = true; }
    public void OnPointerExit(PointerEventData e) { sobre = false; premut = false; }
    public void OnPointerDown(PointerEventData e) { premut = true; }
    public void OnPointerUp(PointerEventData e) { premut = false; }
}
