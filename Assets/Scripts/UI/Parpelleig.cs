using UnityEngine;
using UnityEngine.UI;

// Fa que una imatge parpellegi suaument (punt vermell de "EN DIRECTE", temps que s'acaba...).
public class Parpelleig : MonoBehaviour
{
    public float velocitat = 3f;
    public float alfaMinim = 0.25f;

    Graphic grafic;
    float alfaBase = 1f;

    void Awake()
    {
        grafic = GetComponent<Graphic>();
        if (grafic != null) alfaBase = grafic.color.a;
    }

    void OnDisable()
    {
        if (grafic == null) return;
        Color c = grafic.color;
        c.a = alfaBase;
        grafic.color = c;
    }

    void Update()
    {
        if (grafic == null) return;
        Color c = grafic.color;
        c.a = alfaBase * Mathf.Lerp(alfaMinim, 1f, (Mathf.Sin(Time.unscaledTime * velocitat) + 1f) * 0.5f);
        grafic.color = c;
    }
}
