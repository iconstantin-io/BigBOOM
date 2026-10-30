using UnityEngine;

// Escala l'objecte des d'una mida petita fins a la seva mida real amb un petit rebot, i despres s'esborra.
public class AnimPop : MonoBehaviour
{
    public float desDe = 0.7f;
    public float durada = 0.25f;

    Vector3 escalaFinal;
    float t;

    void Awake()
    {
        escalaFinal = transform.localScale;
        transform.localScale = escalaFinal * desDe;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / durada);
        // ease out back
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float e = 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
        transform.localScale = escalaFinal * Mathf.LerpUnclamped(desDe, 1f, e);
        if (k >= 1f)
        {
            transform.localScale = escalaFinal;
            Destroy(this);
        }
    }
}
