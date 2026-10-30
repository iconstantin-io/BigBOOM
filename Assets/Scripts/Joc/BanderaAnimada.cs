using UnityEngine;

// Bandera feta per codi: base, pal, bola daurada i una tela triangular que oneja.
// Quan es posa cau des de dalt amb un rebot, i quan es treu s'encongeix abans d'esborrar-se.
public class BanderaAnimada : MonoBehaviour
{
    public Color colorTela = new Color32(229, 57, 53, 255);
    public float amplitud = 0.07f;
    public float velocitat = 7f;

    const int SU = 10, SV = 4;       // subdivisions de la tela
    const float Llarg = 0.72f, Alt = 0.46f;

    Mesh malla;
    Vector3[] verts;
    Vector2[] uv;
    float fase, t, tTreure = -1f;
    Vector3 escalaFinal;
    Transform contingut;

    public static BanderaAnimada Crear(Transform pare, float escala)
    {
        GameObject g = new GameObject("Bandera");
        g.transform.SetParent(pare, false);
        g.transform.localScale = Vector3.one * escala;
        return g.AddComponent<BanderaAnimada>();
    }

    void Start()
    {
        escalaFinal = transform.localScale;
        fase = Random.Range(0f, 6.28f);

        contingut = new GameObject("Contingut").transform;
        contingut.SetParent(transform, false);

        EfectesJoc.Primitiva(PrimitiveType.Cylinder, contingut, new Color32(55, 60, 72, 255), new Vector3(0f, 0.04f, 0f), new Vector3(0.42f, 0.04f, 0.42f));
        EfectesJoc.Primitiva(PrimitiveType.Cylinder, contingut, new Color32(238, 238, 242, 255), new Vector3(0f, 0.7f, 0f), new Vector3(0.06f, 0.66f, 0.06f));
        EfectesJoc.Primitiva(PrimitiveType.Sphere, contingut, new Color32(255, 196, 30, 255), new Vector3(0f, 1.38f, 0f), Vector3.one * 0.13f);

        GameObject tela = new GameObject("Tela");
        tela.transform.SetParent(contingut, false);
        tela.transform.localPosition = new Vector3(0.03f, 0.86f, 0f);
        malla = new Mesh { name = "Tela_Bandera" };
        malla.MarkDynamic();
        ConstruirTela();
        tela.AddComponent<MeshFilter>().sharedMesh = malla;
        tela.AddComponent<MeshRenderer>().sharedMaterial = EfectesJoc.Material(colorTela);

        transform.localScale = Vector3.zero;
    }

    void ConstruirTela()
    {
        int n = (SU + 1) * (SV + 1);
        verts = new Vector3[n * 2];
        uv = new Vector2[n * 2];
        int[] tris = new int[SU * SV * 12];
        int k = 0;
        for (int i = 0; i <= SU; i++)
            for (int j = 0; j <= SV; j++)
            {
                int idx = i * (SV + 1) + j;
                uv[idx] = uv[idx + n] = new Vector2(i / (float)SU, j / (float)SV);
            }
        for (int i = 0; i < SU; i++)
            for (int j = 0; j < SV; j++)
            {
                int a = i * (SV + 1) + j, b = a + 1, c = a + SV + 1, d = c + 1;
                // cara davantera i cara del darrere (la tela es veu pels dos costats)
                tris[k++] = a; tris[k++] = b; tris[k++] = c;
                tris[k++] = c; tris[k++] = b; tris[k++] = d;
                tris[k++] = a + n; tris[k++] = c + n; tris[k++] = b + n;
                tris[k++] = c + n; tris[k++] = d + n; tris[k++] = b + n;
            }
        ActualitzarTela(0f);
        malla.vertices = verts;
        malla.uv = uv;
        malla.triangles = tris;
        malla.RecalculateNormals();
        malla.RecalculateBounds();
    }

    void ActualitzarTela(float temps)
    {
        int n = (SU + 1) * (SV + 1);
        for (int i = 0; i <= SU; i++)
        {
            float u = i / (float)SU;
            float baix = u * Alt * 0.5f, dalt = Alt - u * Alt * 0.5f;
            float z = Mathf.Sin(u * 6f - temps * velocitat + fase) * amplitud * u;
            float caiguda = -u * u * 0.04f;
            for (int j = 0; j <= SV; j++)
            {
                float v = j / (float)SV;
                Vector3 p = new Vector3(u * Llarg, Mathf.Lerp(baix, dalt, v) + caiguda, z);
                verts[i * (SV + 1) + j] = p;
                verts[i * (SV + 1) + j + n] = p;
            }
        }
    }

    void Update()
    {
        if (malla == null) return;
        t += Time.deltaTime;

        if (tTreure >= 0f)
        {
            tTreure += Time.deltaTime;
            float k = Mathf.Clamp01(tTreure / 0.18f);
            transform.localScale = escalaFinal * (1f - k);
            if (k >= 1f) Destroy(gameObject);
            return;
        }

        // Entrada: cau des de dalt i rebota
        float ke = Mathf.Clamp01(t / 0.4f);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float e = 1f + c3 * Mathf.Pow(ke - 1f, 3f) + c1 * Mathf.Pow(ke - 1f, 2f);
        transform.localScale = escalaFinal * Mathf.Max(0f, e);
        contingut.localPosition = Vector3.up * (1f - Mathf.Clamp01(ke * 1.6f)) * 0.8f;

        ActualitzarTela(t);
        malla.vertices = verts;
        malla.RecalculateNormals();
    }

    public void Treure()
    {
        if (tTreure < 0f) tTreure = 0f;
        if (malla == null) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (malla != null) Destroy(malla);
    }
}
