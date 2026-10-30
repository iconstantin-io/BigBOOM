using System.Collections;
using UnityEngine;

// Efectes visuals fets amb primitives (no necessiten cap asset): explosio de mina, banderes i petites animacions.
// Els fan servir el JocBuscamines i la vista previa del menu principal.
public static class EfectesJoc
{
    static Material materialBase;

    // Material amb el shader per defecte del projecte (el mateix que fan servir les primitives)
    public static Material Material(Color c)
    {
        if (materialBase == null)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            materialBase = g.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(g);
        }
        Material m = new Material(materialBase);
        m.color = c;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
        return m;
    }

    public static GameObject Primitiva(PrimitiveType tipus, Transform pare, Color c, Vector3 pos, Vector3 escala, bool collider = false)
    {
        GameObject g = GameObject.CreatePrimitive(tipus);
        if (!collider) Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(pare, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = Material(c);
        return g;
    }

    // Petit salt d'escala quan apareix una casella o una mina
    public static void Pop(GameObject g, float desDe = 0.7f, float durada = 0.25f)
    {
        if (g == null) return;
        AnimPop p = g.AddComponent<AnimPop>();
        p.desDe = desDe;
        p.durada = durada;
    }

    // ---------- Explosio ----------
    // sacsejar(intensitat, durada) es crida a l'inici perque la camera tremoli
    public static void Explosio(MonoBehaviour executor, Vector3 pos, float escala, System.Action<float, float> sacsejar = null)
    {
        if (executor == null || !executor.isActiveAndEnabled) return;
        executor.StartCoroutine(RutinaExplosio(pos, escala, sacsejar));
    }

    static IEnumerator RutinaExplosio(Vector3 pos, float e, System.Action<float, float> sacsejar)
    {
        Transform arrel = new GameObject("Explosio").transform;
        arrel.position = pos;
        if (sacsejar != null) sacsejar(0.35f * e, 0.6f);

        Light llum = new GameObject("Llum").AddComponent<Light>();
        llum.transform.SetParent(arrel, false);
        llum.transform.localPosition = Vector3.up * e;
        llum.type = LightType.Point;
        llum.color = new Color(1f, 0.6f, 0.2f);
        llum.range = 14f * e;
        llum.intensity = 8f;

        Renderer flaix = Primitiva(PrimitiveType.Sphere, arrel, new Color(1f, 0.97f, 0.8f), Vector3.up * 0.4f * e, Vector3.one * 0.2f * e).GetComponent<Renderer>();
        Renderer bola = Primitiva(PrimitiveType.Sphere, arrel, new Color(1f, 0.6f, 0.1f), Vector3.up * 0.4f * e, Vector3.one * 0.2f * e).GetComponent<Renderer>();
        Brillar(flaix.sharedMaterial, new Color(1f, 0.9f, 0.6f));
        Brillar(bola.sharedMaterial, new Color(1f, 0.45f, 0.05f));
        Transform ona = Primitiva(PrimitiveType.Cylinder, arrel, new Color(1f, 0.85f, 0.5f), Vector3.up * 0.15f * e, Vector3.one * 0.1f).transform;

        // Runes (trossos de gespa, terra i metall)
        Color[] colorsRunes = { new Color32(124, 179, 66, 255), new Color32(139, 94, 52, 255), new Color32(60, 60, 66, 255), new Color32(170, 215, 81, 255) };
        const int nRunes = 18;
        Transform[] runes = new Transform[nRunes];
        Vector3[] vel = new Vector3[nRunes];
        Vector3[] gir = new Vector3[nRunes];
        for (int i = 0; i < nRunes; i++)
        {
            runes[i] = Primitiva(PrimitiveType.Cube, arrel, colorsRunes[i % colorsRunes.Length], Vector3.up * 0.3f * e,
                Vector3.one * Random.Range(0.12f, 0.32f) * e).transform;
            Vector3 d = Random.insideUnitSphere;
            d.y = Mathf.Abs(d.y) + 0.7f;
            vel[i] = d.normalized * Random.Range(4f, 9f) * e;
            gir[i] = Random.insideUnitSphere * 720f;
        }

        // Fum
        const int nFum = 8;
        Transform[] fum = new Transform[nFum];
        float[] retard = new float[nFum];
        for (int i = 0; i < nFum; i++)
        {
            float g = Random.Range(0.28f, 0.45f);
            Vector3 off = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.2f, 0.8f), Random.Range(-0.8f, 0.8f)) * e;
            fum[i] = Primitiva(PrimitiveType.Sphere, arrel, new Color(g, g, g), off, Vector3.zero).transform;
            retard[i] = Random.Range(0.1f, 0.35f);
        }

        const float durada = 1.8f;
        for (float t = 0f; t < durada; t += Time.deltaTime)
        {
            float dt = Time.deltaTime;

            float kf = Mathf.Clamp01(t / 0.12f);
            float sf = kf < 1f ? Mathf.Lerp(0.2f, 3.4f, kf) : Mathf.Max(0f, 3.4f - (t - 0.12f) * 18f);
            flaix.transform.localScale = Vector3.one * e * sf;

            float kb = Mathf.Clamp01(t / 0.7f);
            bola.transform.localScale = Vector3.one * e * Mathf.Max(0.001f, Mathf.Sin(kb * Mathf.PI) * 3.6f);
            bola.sharedMaterial.color = Color.Lerp(new Color(1f, 0.85f, 0.25f), new Color(0.3f, 0.18f, 0.12f), kb);

            float ko = Mathf.Clamp01(t / 0.45f);
            ona.localScale = new Vector3(Mathf.Lerp(0.3f, 8f, ko) * e, 0.02f * e * (1f - ko) + 0.002f, Mathf.Lerp(0.3f, 8f, ko) * e);
            if (ko >= 1f && ona.gameObject.activeSelf) ona.gameObject.SetActive(false);

            llum.intensity = Mathf.Lerp(8f, 0f, Mathf.Clamp01(t / 0.7f));

            for (int i = 0; i < nRunes; i++)
            {
                vel[i] += Physics.gravity * dt;
                runes[i].localPosition += vel[i] * dt;
                runes[i].Rotate(gir[i] * dt, Space.Self);
                if (t > durada * 0.6f) runes[i].localScale *= 1f - dt * 4f;
            }

            for (int i = 0; i < nFum; i++)
            {
                float k = Mathf.Clamp01((t - retard[i]) / 1.3f);
                fum[i].localPosition += Vector3.up * dt * 1.4f * e;
                fum[i].localScale = Vector3.one * e * Mathf.Sin(k * Mathf.PI) * 1.8f;
            }
            yield return null;
        }
        Object.Destroy(arrel.gameObject);
    }

    static void Brillar(Material m, Color c)
    {
        if (!m.HasProperty("_EmissionColor")) return;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c);
    }
}
