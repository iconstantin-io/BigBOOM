using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class GeneradorIlla : MonoBehaviour
{
    [Header("Obligatori: arrossega aqui el Taulell")]
    public Transform taulell;

    [Header("Illa principal")]
    public float escalaIlla = 1.8f;
    public float marge = 7f;
    public float profunditat = 40f;
    public float gruixHerba = 0.8f;
    public float gruixTerra = 2.5f;
    [Tooltip("Ajust fi: positiu puja l'illa, negatiu la baixa")]
    public float alturaSuperficie = 0f;
    public int llavor = 7;

    [Header("Decoracio de l'illa (fora del tauler)")]
    public int arbres = 60;
    public int arbustos = 45;
    public int herba = 300;
    public int flors = 140;
    public int pedres = 20;
    public float escalaDecoracio = 1f;

    [Header("Roques de sota i flotants")]
    public int roquesPenjades = 28;
    public int roquesFlotants = 16;
    public float escalaRoques = 4.5f;

    [Header("Illa d'espera (espectador)")]
    public bool crearIllaEspera = true;
    public float radiEspera = 16f;
    public float alturaEspera = 40f;
    [Tooltip("Direccio on cau l'illa: 0 = +X, 90 = +Z, 180 = -X, 270 = -Z")]
    public float anglEspera = 270f;

    [Header("Illes del fons")]
    public int illesLluny = 12;
    public float escalaIllesFons = 1.5f;
    public float distanciaMin = 130f;
    public float distanciaMax = 300f;

#if UNITY_EDITOR
    const string DirMat = "Assets/Illa_Generada/Materials/";
    const string DirMesh = "Assets/Illa_Generada/Meshes/";

    class Forma
    {
        public float R, p1, p2;
        public float Radi(float a)
        {
            return R * (0.92f + 0.05f * Mathf.Sin(3f * a + p1) + 0.03f * Mathf.Sin(5f * a + p2));
        }
    }

    System.Random rnd;
    List<string> mancants = new List<string>();
    Dictionary<string, Material> cacheMat = new Dictionary<string, Material>();
    Material matHerba, matTerra, matRoca;

    float Rn() { return (float)rnd.NextDouble(); }
    float Rn(float a, float b) { return a + (b - a) * Rn(); }

    [ContextMenu("Generar illa completa")]
    void GenerarTot()
    {
        if (taulell == null) { Debug.LogError("Arrossega el Taulell al camp 'Taulell'."); return; }
        Renderer[] rends = taulell.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { Debug.LogError("El Taulell no te caselles. Genera'l primer."); return; }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        // Alcada de la part de dalt dels cubs DESTAPATS = superficie de l'illa
        Dictionary<GameObject, float> minArrel = new Dictionary<GameObject, float>();
        float minTotal = float.MaxValue;
        foreach (Renderer r in rends)
        {
            float t = r.bounds.max.y;
            if (t < minTotal) minTotal = t;
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
            if (root == null) continue;
            string pp = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject);
            if (string.IsNullOrEmpty(pp)) continue;
            string lp = pp.ToLower();
            if (!lp.Contains("descobert") && !lp.Contains("destapad")) continue;
            float cur;
            if (minArrel.TryGetValue(root, out cur)) { if (t < cur) minArrel[root] = t; }
            else minArrel[root] = t;
        }
        float topDesc = float.MinValue;
        foreach (KeyValuePair<GameObject, float> kv in minArrel)
            if (kv.Value > topDesc) topDesc = kv.Value;
        float baseY = topDesc > float.MinValue ? topDesc : minTotal;
        float surfY = baseY - 0.03f + alturaSuperficie;

        rnd = new System.Random(llavor);
        mancants.Clear();
        cacheMat.Clear();
        Carpetes();
        for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);

        matHerba = CrearMat("Illa_Herba", new Color32(112, 184, 62, 255));
        matTerra = CrearMat("Illa_Terra", new Color32(139, 94, 52, 255));
        matRoca = CrearMat("Illa_Roca", new Color32(122, 128, 140, 255));

        Vector3 centre = new Vector3(b.center.x, surfY, b.center.z);
        GameObject arrel = new GameObject("Illa_Joc");
        arrel.transform.SetParent(transform, false);
        arrel.transform.position = centre;

        float halfDiag = Mathf.Sqrt(b.extents.x * b.extents.x + b.extents.z * b.extents.z);
        Forma forma = new Forma();
        forma.R = (halfDiag + marge) * escalaIlla;
        forma.p1 = Rn(0f, 6.28f);
        forma.p2 = Rn(0f, 6.28f);
        List<Vector3> punts;
        Mesh m = ConstruirIlla(forma, gruixHerba, gruixTerra, profunditat, 32, out punts);
        CrearObjMalla("Illa_Base", arrel.transform, m, true);

        List<GameObject> llArbres = Carregar("CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5", "Pine_1", "Pine_2", "TwistedTree_1", "TwistedTree_2");
        List<GameObject> llArbustos = Carregar("Bush_Common", "Bush_Common_Flowers");
        List<GameObject> llHerba = Carregar("Grass_Common_Short", "Grass_Common_Tall", "Grass_Wispy_Short", "Clover_1", "Clover_2", "Fern_1");
        List<GameObject> llFlors = Carregar("Flower_3_Group", "Flower_3_Single", "Flower_4_Group", "Flower_4_Single", "Plant_1", "Mushroom_Common");
        List<GameObject> llPedres = Carregar("Pebble_Round_1", "Pebble_Round_2", "Pebble_Round_3", "Pebble_Square_1", "Pebble_Square_2", "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3");
        List<GameObject> llRoques = Carregar("Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3");

        Vector2 excl = new Vector2(b.extents.x + 1.5f, b.extents.z + 1.5f);
        Transform deco = new GameObject("Decoracio").transform;
        deco.SetParent(arrel.transform, false);
        Escampar(deco, llArbres, arbres, 0.65f, 0.95f, forma, excl, 0.9f, 1.4f);
        Escampar(deco, llArbustos, arbustos, 0.55f, 0.95f, forma, excl, 0.8f, 1.3f);
        Escampar(deco, llHerba, herba, 0.3f, 0.97f, forma, excl, 0.8f, 1.4f);
        Escampar(deco, llFlors, flors, 0.3f, 0.97f, forma, excl, 0.8f, 1.3f);
        Escampar(deco, llPedres, pedres, 0.5f, 0.95f, forma, excl, 0.6f, 1.5f);

        Transform pen = new GameObject("Roques_Penjades").transform;
        pen.SetParent(arrel.transform, false);
        for (int i = 0; llRoques.Count > 0 && i < roquesPenjades; i++)
        {
            Vector3 p = punts[rnd.Next(punts.Count)];
            Posar(llRoques[rnd.Next(llRoques.Count)], pen, p, Quaternion.Euler(Rn(0f, 360f), Rn(0f, 360f), Rn(0f, 360f)), Rn(0.8f, 1.6f) * escalaRoques);
        }

        Transform flot = new GameObject("Roques_Flotants").transform;
        flot.SetParent(arrel.transform, false);
        for (int i = 0; llRoques.Count > 0 && i < roquesFlotants; i++)
        {
            float a = Rn(0f, Mathf.PI * 2f);
            float d = forma.R * Rn(1.15f, 1.7f);
            Vector3 p = new Vector3(Mathf.Cos(a) * d, Rn(-profunditat * 0.9f, 4f), Mathf.Sin(a) * d);
            Posar(llRoques[rnd.Next(llRoques.Count)], flot, p, Quaternion.Euler(Rn(0f, 360f), Rn(0f, 360f), Rn(0f, 360f)), Rn(0.5f, 1.2f) * escalaRoques);
        }

        // ---------- Illa d'espera ----------
        if (crearIllaEspera)
        {
            Forma fe = new Forma();
            fe.R = radiEspera;
            fe.p1 = Rn(0f, 6.28f);
            fe.p2 = Rn(0f, 6.28f);
            List<Vector3> pe;
            Mesh me = ConstruirIlla(fe, gruixHerba, gruixTerra, Mathf.Max(8f, radiEspera * 0.9f), 24, out pe);
            GameObject esp = CrearObjMalla("Illa_Espera", arrel.transform, me, true);
            float anRad = anglEspera * Mathf.Deg2Rad;
            float dEsp = halfDiag + radiEspera * 0.5f;
            esp.transform.localPosition = new Vector3(Mathf.Cos(anRad) * dEsp, alturaEspera, Mathf.Sin(anRad) * dEsp);

            Escampar(esp.transform, llArbres, 8, 0.7f, 0.92f, fe, Vector2.zero, 0.9f, 1.3f);
            Escampar(esp.transform, llArbustos, 16, 0.82f, 0.95f, fe, Vector2.zero, 0.8f, 1.2f);
            Escampar(esp.transform, llFlors, 40, 0.3f, 0.9f, fe, Vector2.zero, 0.8f, 1.2f);
            Escampar(esp.transform, llHerba, 50, 0.3f, 0.95f, fe, Vector2.zero, 0.8f, 1.3f);
            Escampar(esp.transform, llPedres, 6, 0.4f, 0.9f, fe, Vector2.zero, 0.6f, 1.2f);
            for (int i = 0; llRoques.Count > 0 && i < 8; i++)
            {
                Vector3 p = pe[rnd.Next(pe.Count)];
                Posar(llRoques[rnd.Next(llRoques.Count)], esp.transform, p, Quaternion.Euler(Rn(0f, 360f), Rn(0f, 360f), Rn(0f, 360f)), Rn(0.6f, 1.2f) * escalaRoques);
            }

            Transform lim = new GameObject("Limits_Invisibles").transform;
            lim.SetParent(esp.transform, false);
            int nl = 24;
            for (int i = 0; i < nl; i++)
            {
                float a = i * Mathf.PI * 2f / nl;
                float rr = fe.Radi(a) * 0.97f;
                GameObject g = new GameObject("Limit_" + (i + 1).ToString("00"));
                g.transform.SetParent(lim, false);
                g.transform.localPosition = new Vector3(Mathf.Cos(a) * rr, 1.5f, Mathf.Sin(a) * rr);
                g.transform.localRotation = Quaternion.Euler(0f, -(a * Mathf.Rad2Deg + 90f), 0f);
                BoxCollider bc = g.AddComponent<BoxCollider>();
                bc.size = new Vector3(2f * Mathf.PI * rr / nl * 1.3f, 3f, 0.5f);
            }

            GameObject cam = new GameObject("Punt_Camera_Espectador");
            cam.transform.SetParent(esp.transform, false);
            cam.transform.position = esp.transform.position + Vector3.up * 1.8f;
            cam.transform.rotation = Quaternion.LookRotation(arrel.transform.position - cam.transform.position);
        }

        // ---------- Illes del fons ----------
        Transform lluny = new GameObject("Illes_Fons").transform;
        lluny.SetParent(arrel.transform, false);
        for (int i = 0; i < illesLluny; i++)
        {
            float ang = (i + Rn(0f, 0.8f)) / Mathf.Max(1, illesLluny) * Mathf.PI * 2f;
            float dist = Rn(distanciaMin, distanciaMax);
            float r = Rn(9f, 22f) * escalaIllesFons;
            Forma f = new Forma();
            f.R = r;
            f.p1 = Rn(0f, 6.28f);
            f.p2 = Rn(0f, 6.28f);
            List<Vector3> pp;
            Mesh mi = ConstruirIlla(f, 0.6f, 1.6f, Rn(7f, 16f) * escalaIllesFons, 20, out pp);
            GameObject go = CrearObjMalla("Illa_Fons_" + (i + 1).ToString("00"), lluny, mi, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(ang) * dist, Rn(-20f, 35f) * escalaIllesFons, Mathf.Sin(ang) * dist);
            go.transform.localRotation = Quaternion.Euler(Rn(-4f, 4f), Rn(0f, 360f), Rn(-4f, 4f));
            Escampar(go.transform, llArbres, Mathf.RoundToInt(r * 0.45f), 0.3f, 0.85f, f, Vector2.zero, 0.8f, 1.3f);
            Escampar(go.transform, llArbustos, Mathf.RoundToInt(r * 0.3f), 0.3f, 0.9f, f, Vector2.zero, 0.8f, 1.2f);
            Escampar(go.transform, llFlors, Mathf.RoundToInt(r * 0.8f), 0.2f, 0.9f, f, Vector2.zero, 0.8f, 1.2f);
            Escampar(go.transform, llPedres, 3, 0.5f, 0.9f, f, Vector2.zero, 0.8f, 1.5f);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("Illa generada. Radi: " + forma.R.ToString("0.0") + " m. Superficie a Y = " + surfY.ToString("0.00"));
        if (mancants.Count > 0) Debug.LogWarning("No he trobat aquests models: " + string.Join(", ", mancants));
    }

    [ContextMenu("Aplicar ambient (llum i boira)")]
    void Ambient()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.80f, 0.90f, 1f);
        RenderSettings.fogDensity = 0.004f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.78f, 0.98f);
        RenderSettings.ambientEquatorColor = new Color(0.75f, 0.80f, 0.78f);
        RenderSettings.ambientGroundColor = new Color(0.45f, 0.42f, 0.35f);
        Light sol = RenderSettings.sun;
        if (sol == null)
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sol = l; break; }
        }
        if (sol != null)
        {
            sol.color = new Color(1f, 0.96f, 0.86f);
            sol.intensity = 1.2f;
            sol.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sol.shadows = LightShadows.Soft;
        }
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("Ambient aplicat.");
    }

    // ---------- Malla de l'illa ----------
    Mesh ConstruirIlla(Forma f, float gH, float gT, float prof, int N, out List<Vector3> punts)
    {
        const int K = 5;
        float h1 = -gH;
        float h2 = -(gH + gT);
        Vector3[] top = new Vector3[N];
        Vector3[] herbaB = new Vector3[N];
        Vector3[][] anells = new Vector3[K][];
        for (int j = 0; j < K; j++) anells[j] = new Vector3[N];

        for (int i = 0; i < N; i++)
        {
            float a = i * Mathf.PI * 2f / N;
            float rad = f.Radi(a);
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            top[i] = new Vector3(c * rad, 0f, s * rad);
            herbaB[i] = new Vector3(c * rad * 0.99f, h1, s * rad * 0.99f);
            for (int j = 0; j < K; j++)
            {
                float t = j / (float)K;
                float rj = rad * 0.94f * Mathf.Pow(1f - t, 0.85f);
                float yj = h2 - t * prof;
                if (j > 0)
                {
                    rj *= 1f + Rn(-0.18f, 0.18f);
                    yj += Rn(-0.06f, 0.06f) * prof;
                }
                anells[j][i] = new Vector3(c * rj, yj, s * rj);
            }
        }
        Vector3 punta = new Vector3(Rn(-0.1f, 0.1f) * f.R, h2 - prof, Rn(-0.1f, 0.1f) * f.R);

        List<Vector3> v = new List<Vector3>();
        List<int> s0 = new List<int>();
        List<int> s1 = new List<int>();
        List<int> s2 = new List<int>();
        for (int i = 0; i < N; i++)
        {
            int n = (i + 1) % N;
            Tri(v, s0, Vector3.zero, top[i], top[n], Vector3.up);
            Banda(v, s0, top[i], top[n], herbaB[i], herbaB[n]);
            Banda(v, s1, herbaB[i], herbaB[n], anells[0][i], anells[0][n]);
            for (int j = 0; j < K - 1; j++)
                Banda(v, s2, anells[j][i], anells[j][n], anells[j + 1][i], anells[j + 1][n]);
            Vector3 mid = (anells[K - 1][i] + anells[K - 1][n] + punta) / 3f;
            Tri(v, s2, anells[K - 1][i], anells[K - 1][n], punta, new Vector3(mid.x, 0f, mid.z) + Vector3.down * 0.5f);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(v);
        mesh.subMeshCount = 3;
        mesh.SetTriangles(s0, 0);
        mesh.SetTriangles(s1, 1);
        mesh.SetTriangles(s2, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        punts = new List<Vector3>();
        for (int j = 0; j < K; j++)
            for (int i = 0; i < N; i++) punts.Add(anells[j][i]);
        return mesh;
    }

    static void Tri(List<Vector3> v, List<int> sub, Vector3 a, Vector3 b, Vector3 c, Vector3 hint)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (Vector3.Dot(n, hint) < 0f) { Vector3 t = b; b = c; c = t; }
        int s = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        sub.Add(s); sub.Add(s + 1); sub.Add(s + 2);
    }

    static void Banda(List<Vector3> v, List<int> sub, Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
    {
        Vector3 m = (a0 + a1 + b0 + b1) * 0.25f;
        Vector3 hint = new Vector3(m.x, 0f, m.z).normalized + Vector3.down * 0.3f;
        Tri(v, sub, a0, b0, b1, hint);
        Tri(v, sub, a0, b1, a1, hint);
    }

    GameObject CrearObjMalla(string nom, Transform pare, Mesh m, bool collider)
    {
        string path = DirMesh + nom + ".asset";
        AssetDatabase.DeleteAsset(path);
        m.name = nom;
        AssetDatabase.CreateAsset(m, path);
        GameObject go = new GameObject(nom);
        go.transform.SetParent(pare, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new Material[] { matHerba, matTerra, matRoca };
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = m;
        return go;
    }

    // ---------- Decoracio ----------
    void Escampar(Transform pare, List<GameObject> llista, int quant, float fMin, float fMax, Forma f, Vector2 excl, float eMin, float eMax)
    {
        if (llista.Count == 0 || quant <= 0) return;
        int fets = 0;
        int intents = 0;
        while (fets < quant && intents < quant * 40)
        {
            intents++;
            float a = Rn(0f, Mathf.PI * 2f);
            float rr = f.Radi(a) * Rn(fMin, fMax);
            float x = Mathf.Cos(a) * rr;
            float z = Mathf.Sin(a) * rr;
            if (Mathf.Abs(x) < excl.x && Mathf.Abs(z) < excl.y) continue;
            Posar(llista[rnd.Next(llista.Count)], pare, new Vector3(x, 0f, z), Quaternion.Euler(0f, Rn(0f, 360f), 0f), Rn(eMin, eMax) * escalaDecoracio);
            fets++;
        }
    }

    GameObject Posar(GameObject prefab, Transform pare, Vector3 pos, Quaternion rot, float esc)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pare);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one * esc;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++) ms[i] = MatQ(ms[i]);
            r.sharedMaterials = ms;
        }
        return go;
    }

    List<GameObject> Carregar(params string[] noms)
    {
        List<GameObject> l = new List<GameObject>();
        foreach (string n in noms)
        {
            GameObject g = Cercar(n);
            if (g != null) l.Add(g);
            else if (!mancants.Contains(n)) mancants.Add(n);
        }
        return l;
    }

    GameObject Cercar(string nom)
    {
        string[] guids = AssetDatabase.FindAssets(nom + " t:GameObject");
        string millor = null;
        int puntMillor = -1;
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (Path.GetFileNameWithoutExtension(p) != nom) continue;
            string ext = Path.GetExtension(p).ToLower();
            int punt = 0;
            if (ext == ".fbx") punt += 10;
            else if (ext != ".prefab" && ext != ".obj") continue;
            if (p.Contains("(Unity)")) punt += 5;
            if (punt > puntMillor) { puntMillor = punt; millor = p; }
        }
        return millor == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(millor);
    }

    // ---------- Materials ----------
    void Carpetes()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Illa_Generada")) AssetDatabase.CreateFolder("Assets", "Illa_Generada");
        if (!AssetDatabase.IsValidFolder("Assets/Illa_Generada/Materials")) AssetDatabase.CreateFolder("Assets/Illa_Generada", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Illa_Generada/Meshes")) AssetDatabase.CreateFolder("Assets/Illa_Generada", "Meshes");
    }

    Shader TriarShader()
    {
        UnityEngine.Rendering.RenderPipelineAsset rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        string tipus = rp != null ? rp.GetType().Name : "";
        Shader sh = null;
        if (rp == null) sh = Shader.Find("Standard");
        else if (tipus.Contains("HDRender") || tipus.Contains("HDRP")) sh = Shader.Find("HDRP/Lit");
        else sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null && rp != null) sh = rp.defaultShader;
        if (sh == null) sh = Shader.Find("Standard");
        Debug.Log("Pipeline: " + (rp != null ? tipus : "Built-in") + " | Shader: " + sh.name);
        return sh;
    }

    Material CrearMat(string nom, Color c)
    {
        string path = DirMat + nom + ".mat";
        Material ex = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (ex != null) return ex;
        Material m = new Material(TriarShader());
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.1f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    Material MatQ(Material orig)
    {
        if (orig == null) return null;
        string n = orig.name;
        Material r;
        if (cacheMat.TryGetValue(n, out r)) return r;
        Material res = orig;
        bool tintar = (n == "Leaves_TwistedTree");   // la textura _C es vermella: fem servir la blanca i la tenyim de verd
        Texture2D tex = CercarTextura(n, tintar);
        if (tex != null)
        {
            string path = DirMat + "Q_" + n + ".mat";
            bool existia = AssetDatabase.LoadAssetAtPath<Material>(path) != null;
            res = CrearMat("Q_" + n, Color.white);
            if (!existia)
            {
                if (res.HasProperty("_BaseMap")) res.SetTexture("_BaseMap", tex);
                if (res.HasProperty("_MainTex")) res.SetTexture("_MainTex", tex);
                if (res.HasProperty("_BaseColorMap")) res.SetTexture("_BaseColorMap", tex);
                if (tintar)
                {
                    Color verd = new Color(0.42f, 0.68f, 0.22f, 1f);
                    if (res.HasProperty("_BaseColor")) res.SetColor("_BaseColor", verd);
                    if (res.HasProperty("_Color")) res.SetColor("_Color", verd);
                }
                bool fulla = n.Contains("Leaf") || n.Contains("Leaves") || n.Contains("Grass") || n.Contains("Flower")
                    || n.Contains("Petal") || n.Contains("Plant") || n.Contains("Clover") || n.Contains("Fern");
                if (fulla)
                {
                    if (res.HasProperty("_AlphaClip")) res.SetFloat("_AlphaClip", 1f);
                    if (res.HasProperty("_Cutoff")) res.SetFloat("_Cutoff", 0.5f);
                    if (res.HasProperty("_Mode")) res.SetFloat("_Mode", 1f);
                    if (res.HasProperty("_Cull")) res.SetFloat("_Cull", 0f);
                    if (res.HasProperty("_AlphaCutoffEnable")) res.SetFloat("_AlphaCutoffEnable", 1f);
                    if (res.HasProperty("_AlphaCutoff")) res.SetFloat("_AlphaCutoff", 0.5f);
                    if (res.HasProperty("_DoubleSidedEnable")) res.SetFloat("_DoubleSidedEnable", 1f);
                    res.EnableKeyword("_ALPHATEST_ON");
                    res.renderQueue = 2450;
                }
                EditorUtility.SetDirty(res);
            }
        }
        cacheMat[n] = res;
        return res;
    }

    Texture2D CercarTextura(string nom, bool blanca)
    {
        string[] candidats = blanca
            ? new string[] { nom }
            : new string[] { nom + "_C", nom + "_Diffuse", nom };
        foreach (string c in candidats)
        {
            foreach (string g in AssetDatabase.FindAssets(c + " t:Texture2D"))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == c)
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
        }
        return null;
    }
#endif
}