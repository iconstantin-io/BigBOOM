using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Mini taulell 3D per al menu principal. Es renderitza a una RenderTexture i reprodueix en bucle
// una partida curta: el taulell apareix, es destapen caselles, es posen banderes i explota una mina.
public class MenuPreviewTaulell : MonoBehaviour
{
    public int mida = 8;
    public int mines = 10;

    public RenderTexture Textura { get; private set; }
    // (index de fase, text) per poder posar un peu de foto al menu
    public System.Action<int, string> OnFase;

    static readonly Color Herba1 = new Color32(170, 215, 81, 255), Herba2 = new Color32(162, 209, 73, 255);
    static readonly Color Terra1 = new Color32(229, 194, 159, 255), Terra2 = new Color32(215, 184, 153, 255);
    static readonly Color ColorMina = new Color32(219, 50, 54, 255);
    static readonly Color[] ColorsNum =
    {
        Color.clear, new Color32(25, 118, 210, 255), new Color32(56, 142, 60, 255), new Color32(211, 47, 47, 255),
        new Color32(123, 31, 162, 255), new Color32(255, 143, 0, 255), new Color32(0, 151, 167, 255),
        new Color32(66, 66, 66, 255), new Color32(120, 120, 120, 255)
    };

    class Cel
    {
        public Transform t;
        public Material mat;
        public float h = 0.5f, hObj = 0.5f, y = -6f, yObj = 0f, s = 0f, sObj = 1f, retard;
        public Color col, colObj;
        public TextMeshPro num;
        public GameObject extra;
    }

    Transform arrel, pivot;
    Camera cam;
    Vector3 posCam;
    float sacsejada;
    Cel[,] cels;
    bool[,] esMina, oberta;
    int[,] num;
    readonly List<GameObject> temporals = new List<GameObject>();

    public void Inicialitzar(int amplada, int alcada)
    {
        Textura = new RenderTexture(amplada, alcada, 24, RenderTextureFormat.ARGB32);
        Textura.antiAliasing = 4;
        Textura.Create();

        arrel = new GameObject("Preview_Taulell").transform;
        arrel.SetParent(transform, false);
        arrel.position = new Vector3(0f, -3000f, 0f);
        pivot = new GameObject("Pivot").transform;
        pivot.SetParent(arrel, false);

        GameObject gc = new GameObject("Camera_Preview");
        gc.transform.SetParent(arrel, false);
        cam = gc.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.targetTexture = Textura;
        cam.allowHDR = false;     // perque el fons transparent es mantingui a la textura
        cam.allowMSAA = false;
        cam.fieldOfView = 32f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 80f;
        posCam = new Vector3(0f, 12f, -14.5f);
        gc.transform.localPosition = posCam;
        gc.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -0.9f, 0f) - posCam);

        GameObject gl = new GameObject("Llum_Preview");
        gl.transform.SetParent(arrel, false);
        Light l = gl.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.05f;
        l.color = new Color(1f, 0.96f, 0.88f);
        l.shadows = LightShadows.Soft;
        gl.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

        ConstruirIlla();
        StartCoroutine(Bucle());
    }

    void OnDestroy()
    {
        if (Textura != null) Textura.Release();
    }

    // ---------- Construccio ----------
    void ConstruirIlla()
    {
        float m = mida;
        EfectesJoc.Primitiva(PrimitiveType.Cube, pivot, new Color32(139, 94, 52, 255), new Vector3(0f, -1.1f, 0f), new Vector3(m + 0.7f, 1.6f, m + 0.7f));
        EfectesJoc.Primitiva(PrimitiveType.Cube, pivot, new Color32(96, 158, 52, 255), new Vector3(0f, -0.3f, 0f), new Vector3(m + 0.9f, 0.3f, m + 0.9f));
        System.Random r = new System.Random(3);
        for (int i = 0; i < 9; i++)
        {
            float x = (float)(r.NextDouble() - 0.5) * m * 0.8f, z = (float)(r.NextDouble() - 0.5) * m * 0.8f;
            float s = 1f + (float)r.NextDouble() * 1.6f;
            GameObject roca = EfectesJoc.Primitiva(PrimitiveType.Cube, pivot, new Color32(118, 84, 52, 255),
                new Vector3(x, -2.2f - (float)r.NextDouble() * 1.2f, z), new Vector3(s, s * 1.3f, s));
            roca.transform.localRotation = Quaternion.Euler(r.Next(0, 40), r.Next(0, 360), r.Next(0, 40));
        }

        cels = new Cel[mida, mida];
        for (int f = 0; f < mida; f++)
            for (int c = 0; c < mida; c++)
            {
                Cel cel = new Cel();
                GameObject g = EfectesJoc.Primitiva(PrimitiveType.Cube, pivot, Herba1, Vector3.zero, Vector3.zero);
                cel.t = g.transform;
                cel.mat = g.GetComponent<Renderer>().sharedMaterial;
                cels[f, c] = cel;
            }
    }

    Vector3 PosCel(int f, int c) { return new Vector3(c - (mida - 1) * 0.5f, 0f, f - (mida - 1) * 0.5f); }
    Vector3 DaltCel(int f, int c) { return PosCel(f, c) + Vector3.up * (cels[f, c].yObj - 0.15f + cels[f, c].hObj); }

    void Update()
    {
        if (cels == null) return;
        float dt = Time.unscaledDeltaTime;
        float k = 1f - Mathf.Exp(-10f * dt);
        for (int f = 0; f < mida; f++)
            for (int c = 0; c < mida; c++)
            {
                Cel cel = cels[f, c];
                if (cel.retard > 0f) { cel.retard -= dt; continue; }
                cel.h = Mathf.Lerp(cel.h, cel.hObj, k);
                cel.y = Mathf.Lerp(cel.y, cel.yObj, k);
                cel.s = Mathf.Lerp(cel.s, cel.sObj, k);
                cel.col = Color.Lerp(cel.col, cel.colObj, k * 1.5f);
                cel.mat.color = cel.col;
                cel.t.localScale = new Vector3(0.94f * cel.s, Mathf.Max(0.001f, cel.h * cel.s), 0.94f * cel.s);
                cel.t.localPosition = PosCel(f, c) + Vector3.up * (cel.y - 0.15f + cel.h * 0.5f);
                if (cel.num != null)
                {
                    cel.num.transform.localPosition = PosCel(f, c) + Vector3.up * (cel.y - 0.15f + cel.h + 0.01f);
                    cel.num.transform.localScale = Vector3.one * cel.s;
                }
            }

        float tm = Time.unscaledTime;
        pivot.localRotation = Quaternion.Euler(0f, 25f + Mathf.Sin(tm * 0.25f) * 22f, 0f);
        pivot.localPosition = Vector3.up * Mathf.Sin(tm * 0.8f) * 0.12f;
        sacsejada = Mathf.MoveTowards(sacsejada, 0f, dt * 0.9f);
        cam.transform.localPosition = posCam + Random.insideUnitSphere * sacsejada;
    }

    // ---------- Bucle de la "partida" ----------
    IEnumerator Bucle()
    {
        while (true)
        {
            Vector2Int inici = Preparar();
            Fase(0, "Un nou taulell a cada torn");
            yield return Esperar(1.3f);

            Fase(1, "Destapa caselles i suma punts");
            yield return Cascada(inici.y, inici.x);
            yield return Esperar(0.6f);
            for (int i = 0; i < 2; i++)
            {
                Vector2Int p;
                if (!Frontera(false, out p)) break;
                yield return Cascada(p.y, p.x);
                yield return Esperar(0.5f);
            }

            Fase(2, "Marca les mines amb banderes");
            for (int i = 0; i < 3; i++)
            {
                Vector2Int p;
                if (!Frontera(true, out p)) break;
                Bandera(p.y, p.x);
                yield return Esperar(0.55f);
            }
            yield return Esperar(0.6f);

            Fase(3, "Compte amb les mines!");
            yield return Esperar(0.7f);
            Vector2Int mina;
            if (Frontera(true, out mina) || QualsevolMina(out mina)) Explotar(mina.y, mina.x);
            yield return Esperar(2.4f);

            yield return Desapareixer();
        }
    }

    IEnumerator Esperar(float s)
    {
        for (float t = 0f; t < s; t += Time.unscaledDeltaTime) yield return null;
    }

    void Fase(int i, string text)
    {
        if (OnFase != null) OnFase(i, text);
    }

    Vector2Int Preparar()
    {
        foreach (GameObject g in temporals) if (g != null) Destroy(g);
        temporals.Clear();

        esMina = new bool[mida, mida];
        oberta = new bool[mida, mida];
        num = new int[mida, mida];
        int sf = Random.Range(2, mida - 2), sc = Random.Range(2, mida - 2);
        int posades = 0;
        while (posades < mines)
        {
            int f = Random.Range(0, mida), c = Random.Range(0, mida);
            if (esMina[f, c] || (Mathf.Abs(f - sf) <= 1 && Mathf.Abs(c - sc) <= 1)) continue;
            esMina[f, c] = true;
            posades++;
        }
        for (int f = 0; f < mida; f++)
            for (int c = 0; c < mida; c++)
            {
                int n = 0;
                for (int df = -1; df <= 1; df++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int ff = f + df, cc = c + dc;
                        if (ff >= 0 && ff < mida && cc >= 0 && cc < mida && esMina[ff, cc]) n++;
                    }
                num[f, c] = n;

                Cel cel = cels[f, c];
                if (cel.num != null) Destroy(cel.num.gameObject);
                cel.num = null;
                cel.extra = null;
                Color herba = (f + c) % 2 == 0 ? Herba1 : Herba2;
                cel.col = cel.colObj = herba;
                cel.h = cel.hObj = 0.5f;
                cel.y = -6f; cel.yObj = 0f;
                cel.s = 0f; cel.sObj = 1f;
                cel.retard = (f + c) * 0.045f;
            }
        return new Vector2Int(sc, sf);
    }

    void Obrir(int f, int c)
    {
        if (oberta[f, c]) return;
        oberta[f, c] = true;
        Cel cel = cels[f, c];
        cel.hObj = 0.3f;
        cel.colObj = (f + c) % 2 == 0 ? Terra1 : Terra2;
        if (num[f, c] > 0 && !esMina[f, c])
        {
            GameObject g = new GameObject("Num");
            g.transform.SetParent(pivot, false);
            g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro t = g.AddComponent<TextMeshPro>();
            t.text = num[f, c].ToString();
            t.fontSize = 7f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = ColorsNum[num[f, c]];
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(1f, 1f);
            cel.num = t;
        }
    }

    IEnumerator Cascada(int f0, int c0)
    {
        if (esMina[f0, c0] || oberta[f0, c0]) yield break;
        List<Vector2Int> capa = new List<Vector2Int> { new Vector2Int(c0, f0) };
        bool[,] vist = new bool[mida, mida];
        vist[f0, c0] = true;
        while (capa.Count > 0)
        {
            List<Vector2Int> seguent = new List<Vector2Int>();
            foreach (Vector2Int p in capa)
            {
                Obrir(p.y, p.x);
                if (num[p.y, p.x] != 0) continue;
                for (int df = -1; df <= 1; df++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int ff = p.y + df, cc = p.x + dc;
                        if (ff < 0 || ff >= mida || cc < 0 || cc >= mida || vist[ff, cc] || esMina[ff, cc]) continue;
                        vist[ff, cc] = true;
                        seguent.Add(new Vector2Int(cc, ff));
                    }
            }
            capa = seguent;
            yield return Esperar(0.06f);
        }
    }

    // Casella tapada al costat d'una de destapada: una mina (per a banderes) o una casella segura
    bool Frontera(bool volMina, out Vector2Int res)
    {
        List<Vector2Int> llista = new List<Vector2Int>();
        for (int f = 0; f < mida; f++)
            for (int c = 0; c < mida; c++)
            {
                if (oberta[f, c] || esMina[f, c] != volMina || cels[f, c].extra != null) continue;
                bool toca = false;
                for (int df = -1; df <= 1 && !toca; df++)
                    for (int dc = -1; dc <= 1 && !toca; dc++)
                    {
                        int ff = f + df, cc = c + dc;
                        if (ff >= 0 && ff < mida && cc >= 0 && cc < mida && oberta[ff, cc]) toca = true;
                    }
                if (toca) llista.Add(new Vector2Int(c, f));
            }
        res = llista.Count > 0 ? llista[Random.Range(0, llista.Count)] : Vector2Int.zero;
        return llista.Count > 0;
    }

    bool QualsevolMina(out Vector2Int res)
    {
        for (int f = 0; f < mida; f++)
            for (int c = 0; c < mida; c++)
                if (esMina[f, c] && cels[f, c].extra == null) { res = new Vector2Int(c, f); return true; }
        res = Vector2Int.zero;
        return false;
    }

    void Bandera(int f, int c)
    {
        BanderaAnimada b = BanderaAnimada.Crear(pivot, 0.5f);
        b.transform.localPosition = DaltCel(f, c);
        b.transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
        cels[f, c].extra = b.gameObject;
        temporals.Add(b.gameObject);
    }

    void Explotar(int f, int c)
    {
        Cel cel = cels[f, c];
        cel.hObj = 0.3f;
        cel.colObj = ColorMina;
        oberta[f, c] = true;

        GameObject mina = new GameObject("Mina");
        mina.transform.SetParent(pivot, false);
        mina.transform.localPosition = PosCel(f, c) + Vector3.up * 0.35f;
        EfectesJoc.Primitiva(PrimitiveType.Sphere, mina.transform, new Color32(30, 30, 34, 255), Vector3.zero, Vector3.one * 0.42f);
        for (int i = 0; i < 4; i++)
        {
            GameObject p = EfectesJoc.Primitiva(PrimitiveType.Cube, mina.transform, new Color32(30, 30, 34, 255), Vector3.zero, new Vector3(0.62f, 0.07f, 0.07f));
            p.transform.localRotation = Quaternion.Euler(0f, i * 45f, i % 2 == 0 ? 0f : 90f);
        }
        EfectesJoc.Pop(mina, 0.3f, 0.3f);
        cel.extra = mina;
        temporals.Add(mina);

        EfectesJoc.Explosio(this, pivot.TransformPoint(PosCel(f, c) + Vector3.up * 0.3f), 0.55f, (i, d) => sacsejada = Mathf.Max(sacsejada, 0.4f));
    }

    IEnumerator Desapareixer()
    {
        for (int f = 0; f < mida; f++)
            for (int c = 0; c < mida; c++)
            {
                Cel cel = cels[f, c];
                cel.retard = (mida * 2 - f - c) * 0.03f;
                cel.yObj = -6f;
                cel.sObj = 0f;
            }
        foreach (GameObject g in temporals)
        {
            if (g == null) continue;
            BanderaAnimada b = g.GetComponent<BanderaAnimada>();
            if (b != null) b.Treure(); else Destroy(g);
        }
        temporals.Clear();
        yield return Esperar(1.4f);
    }
}
