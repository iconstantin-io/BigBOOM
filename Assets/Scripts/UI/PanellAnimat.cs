using UnityEngine;

// Fa apareixer un panell amb un fos i un petit zoom cada cop que s'activa.
public class PanellAnimat : MonoBehaviour
{
    public float durada = 0.22f;
    public float escalaInicial = 0.95f;

    CanvasGroup grup;
    float t;

    void OnEnable()
    {
        if (grup == null) grup = GetComponent<CanvasGroup>();
        if (grup == null) grup = gameObject.AddComponent<CanvasGroup>();
        t = 0f;
        Aplicar(0f);
    }

    void Update()
    {
        if (t >= durada) return;
        t += Time.unscaledDeltaTime;
        Aplicar(Mathf.Clamp01(t / durada));
    }

    void Aplicar(float k)
    {
        float e = 1f - (1f - k) * (1f - k) * (1f - k);
        grup.alpha = e;
        transform.localScale = Vector3.one * Mathf.Lerp(escalaInicial, 1f, e);
    }
}
