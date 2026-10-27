using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Logica de joc del Buscamines PvP per torns (BigBOOM).
// Afegeix aquest component a un objecte buit (p. ex. "Joc"), arrossega-hi el Taulell i el HUD, i prem Play.
// Clic esquerre: destapar casella. Clic dret: posar o treure bandera.
public class JocBuscamines : MonoBehaviour
{
    [Header("Referencies (si es deixen buides, es busquen soles)")]
    public GeneradorTaulell taulell;
    public GeneradorHUD hud;
    public Camera camJoc;

    [Header("Jugadors")]
    public string nomJugador1 = "PLAYER_1";
    public string nomJugador2 = "PLAYER_2";

    [Header("Mapa")]
    public int columnes = 16;
    public int files = 16;
    public int mines = 40;

    [Header("Torns (segons)")]
    public int tornsTotals = 10;
    public float tempsVistaAeria = 0f;
    public float tempsCompteEnrere = 3f;
    public float tempsTorn = 15f;
    public float tempsFiTorn = 3f;

    [Header("Puntuacio (GDD)")]
    public int puntsMina = -5;
    public int puntsBanderaBona = 1;
    public int puntsBanderaDolenta = -1;
    [Tooltip("Bonus per cada casella segura destapada (0 = com diu el GDD)")]
    public int puntsPerCasella = 0;

    [Header("Controls")]
    [Tooltip("Desactivat: es pica on hi ha el ratoli. Activat: es pica al centre de la pantalla (mira, 1a persona)")]
    public bool usarMira = false;

    [Header("Camera aeria")]
    public bool colocarCamera = true;
    [Tooltip("Posicio de la camera respecte al centre del tauler. Per defecte des d'un costat (-X): a -Z hi ha l'illa d'espera i a +Z el HUD")]
    public Vector3 posicioCamera = new Vector3(-60f, 50f, 15f);
    [Tooltip("Si no hi ha HUD, alcada del punt on mira la camera. Si hi ha HUD, mira al punt mig entre el tauler i el HUD")]
    public float alturaMirada = 12f;

    [Header("Bandera (opcional: si es buit, se'n crea una de provisional)")]
    public GameObject prefabBandera;

    enum Estat { Preparacio, Jugant, FiTorn, FiPartida }
    Estat estat = Estat.Preparacio;

    // Estat del mapa
    int F, C;
    bool[,] esMina;
    int[,] num;
    bool[,] oberta;
    GameObject[,] caselles;
    GameObject[,] banderes;
    Transform[] filesObj;
    readonly Dictionary<Transform, Vector2Int> mapa = new Dictionary<Transform, Vector2Int>();
    Bounds limitsTaulell;

    // Estat de la partida
    int torn = 0;
    int jugador = 0;            // 0 = jugador 1, 1 = jugador 2
    readonly int[] punts = new int[2];
    int puntsTorn = 0;
    float tempsRestant = 0f;
    bool explotat = false;
    bool netejat = false;
    string textFinal = "";
    bool avisFalten = false;

    // ---------------------------------------------------------------
    void Start()
    {
        if (taulell == null) taulell = FindFirstObjectByType<GeneradorTaulell>();
        if (hud == null) hud = FindFirstObjectByType<GeneradorHUD>();
        if (camJoc == null) camJoc = Camera.main;

        if (taulell == null) { Debug.LogError("JocBuscamines: no trobo cap GeneradorTaulell a l'escena."); enabled = false; return; }
        if (camJoc == null) { Debug.LogError("JocBuscamines: no hi ha cap camera amb l'etiqueta MainCamera."); enabled = false; return; }
        if (hud == null) Debug.LogWarning("JocBuscamines: no he trobat el HUD, el joc funcionara igualment pero sense marcador.");
        else hud.PosarNoms(nomJugador1, nomJugador2);

        StartCoroutine(Partida());
    }

    IEnumerator Partida()
    {
        for (torn = 1; torn <= tornsTotals; torn++)
        {
            jugador = (torn - 1) % 2;
            GenerarMapa();
            if (torn == 1 && colocarCamera) ColocarCamera();

            // Vista aeria + compte enrere (no es pot picar)
            estat = Estat.Preparacio;
            puntsTorn = 0;
            explotat = false;
            netejat = false;
            tempsRestant = tempsVistaAeria + tempsCompteEnrere;
            while (tempsRestant > 0f) { tempsRestant -= Time.deltaTime; yield return null; }

            // Torn
            estat = Estat.Jugant;
            tempsRestant = tempsTorn;
            while (tempsRestant > 0f && !explotat && !netejat) { tempsRestant -= Time.deltaTime; yield return null; }
            tempsRestant = 0f;

            // Fi del torn: es compten les banderes i es mostren les mines
            estat = Estat.FiTorn;
            TancarTorn();
            yield return new WaitForSeconds(tempsFiTorn);
        }

        estat = Estat.FiPartida;
        if (punts[0] > punts[1]) textFinal = "GUANYA " + nomJugador1;
        else if (punts[1] > punts[0]) textFinal = "GUANYA " + nomJugador2;
        else textFinal = "EMPAT";
        Debug.Log("Fi de la partida: " + textFinal + " (" + punts[0] + " - " + punts[1] + ")");
    }

    void Update()
    {
        if (hud != null)
        {
            string nom = estat == Estat.FiPartida ? textFinal : (jugador == 0 ? nomJugador1 : nomJugador2);
            hud.ActualitzarHUD(Mathf.Clamp(torn, 0, tornsTotals), nom, tempsRestant, puntsTorn, punts[0], punts[1]);
        }

        if (estat != Estat.Jugant) return;
        Mouse ratoli = Mouse.current;
        if (ratoli == null) return;
        bool esquerre = ratoli.leftButton.wasPressedThisFrame;
        bool dret = ratoli.rightButton.wasPressedThisFrame;
        if (!esquerre && !dret) return;

        Vector2 punt = usarMira ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) : ratoli.position.ReadValue();
        Ray raig = camJoc.ScreenPointToRay(punt);
        RaycastHit[] cops = Physics.RaycastAll(raig, 1000f);
        System.Array.Sort(cops, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit cop in cops)
        {
            Vector2Int p;
            if (!TrobarCasella(cop.collider.transform, out p)) continue;
            if (esquerre) Destapar(p.y, p.x);
            else CanviarBandera(p.y, p.x);
            break;
        }
    }

    void OnGUI()
    {
        if (!usarMira) return;
        GUIStyle st = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width * 0.5f - 25f, Screen.height * 0.5f - 25f, 50f, 50f), "+", st);
    }

    // ---------------------------------------------------------------
    // Generacio del mapa (mateixes regles que el GeneradorTaulell, pero en temps de joc)
    void GenerarMapa()
    {
        F = files; C = columnes;
        avisFalten = false;
        System.Random rnd = taulell.llavor == 0 ? new System.Random() : new System.Random(taulell.llavor + torn);

        int sf = rnd.Next(1, F - 1);
        int sc = rnd.Next(1, C - 1);

        List<Vector2Int> candidats = new List<Vector2Int>();
        for (int f = 0; f < F; f++)
            for (int c = 0; c < C; c++)
            {
                bool prop = Mathf.Abs(f - sf) <= 1 && Mathf.Abs(c - sc) <= 1;
                if (taulell.obrirZonaInicial && prop) continue;
                candidats.Add(new Vector2Int(c, f));
            }
        for (int i = candidats.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            Vector2Int t = candidats[i]; candidats[i] = candidats[j]; candidats[j] = t;
        }
        int nMines = Mathf.Min(mines, candidats.Count);

        esMina = new bool[F, C];
        for (int i = 0; i < nMines; i++) esMina[candidats[i].y, candidats[i].x] = true;

        num = new int[F, C];
        for (int f = 0; f < F; f++)
            for (int c = 0; c < C; c++)
                num[f, c] = ComptarVeines(f, c);

        // Esborrar el taulell anterior
        mapa.Clear();
        for (int i = taulell.transform.childCount - 1; i >= 0; i--)
            Destroy(taulell.transform.GetChild(i).gameObject);

        oberta = new bool[F, C];
        caselles = new GameObject[F, C];
        banderes = new GameObject[F, C];
        filesObj = new Transform[F];
        for (int f = 0; f < F; f++)
        {
            GameObject fila = new GameObject("Fila_" + (f + 1).ToString("00"));
            fila.transform.SetParent(taulell.transform, false);
            filesObj[f] = fila.transform;
            for (int c = 0; c < C; c++) Posar(f, c, false);
        }

        // Zona inicial oberta (com el primer clic de Google)
        if (taulell.obrirZonaInicial) ObrirCascada(sf, sc, false);

        // Limits del tauler (per a la camera)
        Renderer[] rends = taulell.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            limitsTaulell = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) limitsTaulell.Encapsulate(rends[i].bounds);
        }
    }

    int ComptarVeines(int f, int c)
    {
        int n = 0;
        for (int df = -1; df <= 1; df++)
            for (int dc = -1; dc <= 1; dc++)
            {
                int ff = f + df, cc = c + dc;
                if ((df != 0 || dc != 0) && ff >= 0 && ff < F && cc >= 0 && cc < C && esMina[ff, cc]) n++;
            }
        return n;
    }

    // Crea (o substitueix) la casella f,c amb el prefab tapat o descobert
    void Posar(int f, int c, bool descoberta)
    {
        if (caselles[f, c] != null)
        {
            mapa.Remove(caselles[f, c].transform);
            Destroy(caselles[f, c]);
        }
        GameObject prefab = Triar(descoberta, f, c);
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, filesObj[f]);
        float mida = taulell.mida;
        float px = c * mida;
        float pz = (F - 1 - f) * mida;
        if (taulell.girar180)
        {
            px = (C - 1) * mida - px;
            pz = (F - 1) * mida - pz;
        }
        go.transform.localPosition = new Vector3(px, 0f, pz);
        go.transform.localRotation = taulell.girar180 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        go.name = "Casella_" + (f + 1).ToString("00") + "_" + (c + 1).ToString("00");
        caselles[f, c] = go;
        mapa[go.transform] = new Vector2Int(c, f);
    }

    GameObject Triar(bool descoberta, int f, int c)
    {
        bool model1 = (f + c) % 2 == 0;
        JocCaselles joc = descoberta
            ? (model1 ? taulell.descobertesModel1 : taulell.descobertesModel2)
            : (model1 ? taulell.tapadesModel1 : taulell.tapadesModel2);
        if (esMina[f, c]) return joc.mina;
        int n = num[f, c];
        for (int i = n; i >= 0; i--)
        {
            if (i < joc.numeros.Length && joc.numeros[i] != null)
            {
                if (i != n && !avisFalten) { avisFalten = true; Debug.LogWarning("Falten prefabs de numeros al Taulell: s'ha fet servir el mes alt que hi ha."); }
                return joc.numeros[i];
            }
        }
        return null;
    }

    bool TrobarCasella(Transform t, out Vector2Int p)
    {
        while (t != null)
        {
            if (mapa.TryGetValue(t, out p)) return true;
            t = t.parent;
        }
        p = Vector2Int.zero;
        return false;
    }

    // ---------------------------------------------------------------
    // Accions del jugador
    void Destapar(int f, int c)
    {
        if (oberta[f, c] || banderes[f, c] != null) return;

        if (esMina[f, c])
        {
            oberta[f, c] = true;
            Posar(f, c, true);
            puntsTorn += puntsMina;
            explotat = true;
            StartCoroutine(Explosio(caselles[f, c] != null ? caselles[f, c].transform.position : Vector3.zero));
            return;
        }

        ObrirCascada(f, c, true);

        int obertes = 0;
        for (int ff = 0; ff < F; ff++)
            for (int cc = 0; cc < C; cc++)
                if (oberta[ff, cc]) obertes++;
        if (obertes >= F * C - Mathf.Min(mines, F * C)) netejat = true;
    }

    void ObrirCascada(int f, int c, bool sumarPunts)
    {
        Queue<Vector2Int> cua = new Queue<Vector2Int>();
        Obrir(f, c, sumarPunts);
        cua.Enqueue(new Vector2Int(c, f));
        while (cua.Count > 0)
        {
            Vector2Int p = cua.Dequeue();
            if (num[p.y, p.x] != 0) continue;
            for (int df = -1; df <= 1; df++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int ff = p.y + df, cc = p.x + dc;
                    if (ff < 0 || ff >= F || cc < 0 || cc >= C) continue;
                    if (oberta[ff, cc] || esMina[ff, cc] || banderes[ff, cc] != null) continue;
                    Obrir(ff, cc, sumarPunts);
                    cua.Enqueue(new Vector2Int(cc, ff));
                }
        }
    }

    void Obrir(int f, int c, bool sumarPunts)
    {
        oberta[f, c] = true;
        Posar(f, c, true);
        if (sumarPunts) puntsTorn += puntsPerCasella;
    }

    void CanviarBandera(int f, int c)
    {
        if (oberta[f, c]) return;
        if (banderes[f, c] != null)
        {
            Destroy(banderes[f, c]);
            banderes[f, c] = null;
            return;
        }
        GameObject cas = caselles[f, c];
        if (cas == null) return;

        Bounds b = new Bounds(cas.transform.position, Vector3.zero);
        Renderer[] rs = cas.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0) { b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); }

        GameObject band = prefabBandera != null ? Instantiate(prefabBandera) : CrearBanderaProvisional();
        band.name = "Bandera_" + (f + 1).ToString("00") + "_" + (c + 1).ToString("00");
        band.transform.SetParent(filesObj[f], true);
        band.transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
        if (prefabBandera == null) band.transform.localScale = Vector3.one * (taulell.mida * 0.5f);
        banderes[f, c] = band;
    }

    GameObject CrearBanderaProvisional()
    {
        GameObject arrel = new GameObject("Bandera");
        GameObject pal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(pal.GetComponent<Collider>());
        pal.transform.SetParent(arrel.transform, false);
        pal.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        pal.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
        pal.GetComponent<Renderer>().material.color = new Color(0.25f, 0.18f, 0.1f);

        GameObject tela = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(tela.GetComponent<Collider>());
        tela.transform.SetParent(arrel.transform, false);
        tela.transform.localPosition = new Vector3(0.3f, 1.0f, 0f);
        tela.transform.localScale = new Vector3(0.6f, 0.4f, 0.05f);
        tela.GetComponent<Renderer>().material.color = new Color(0.83f, 0.18f, 0.18f);
        return arrel;
    }

    // ---------------------------------------------------------------
    void TancarTorn()
    {
        int bones = 0, dolentes = 0;
        for (int f = 0; f < F; f++)
            for (int c = 0; c < C; c++)
            {
                if (banderes[f, c] != null)
                {
                    if (esMina[f, c]) bones++; else dolentes++;
                }
                if (esMina[f, c] && !oberta[f, c]) { oberta[f, c] = true; Posar(f, c, true); }   // es mostren totes les mines
            }
        puntsTorn += bones * puntsBanderaBona + dolentes * puntsBanderaDolenta;
        punts[jugador] += puntsTorn;
        Debug.Log("Fi del torn " + torn + " (" + (jugador == 0 ? nomJugador1 : nomJugador2) + "): banderes bones " + bones
            + ", dolentes " + dolentes + (explotat ? ", ha explotat" : "") + " -> " + puntsTorn + " punts.");
    }

    IEnumerator Explosio(Vector3 pos)
    {
        GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(bola.GetComponent<Collider>());
        bola.name = "Explosio";
        bola.transform.position = pos + Vector3.up * 0.5f;
        Renderer r = bola.GetComponent<Renderer>();
        r.material.color = new Color(1f, 0.55f, 0.1f);

        Vector3 posCam = camJoc.transform.position;
        float durada = 0.6f;
        for (float t = 0f; t < durada; t += Time.deltaTime)
        {
            float k = t / durada;
            bola.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, taulell.mida * 4f, k);
            r.material.color = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.3f, 0.3f), k);
            camJoc.transform.position = posCam + Random.insideUnitSphere * 0.4f * (1f - k);
            yield return null;
        }
        camJoc.transform.position = posCam;
        Destroy(bola);
    }

    void ColocarCamera()
    {
        Vector3 centre = limitsTaulell.center;
        Vector3 objectiu = centre + Vector3.up * alturaMirada;
        if (hud != null && hud.pantalla != null) objectiu = Vector3.Lerp(centre, hud.pantalla.position, 0.5f);
        camJoc.transform.position = centre + posicioCamera;
        camJoc.transform.LookAt(objectiu);
    }

#if UNITY_EDITOR
    [Header("Neteja de decoracio (menu ⋮)")]
    [Tooltip("Metres de marge al voltant del tauler")]
    public float margeNeteja = 1f;

    // Esborra arbres, arbustos, pedres... que toquen la zona del tauler amb la mida de Columnes x Files
    [ContextMenu("Esborrar decoracio que toca el tauler")]
    void EsborrarDecoracio()
    {
        if (taulell == null) taulell = FindFirstObjectByType<GeneradorTaulell>();
        if (hud == null) hud = FindFirstObjectByType<GeneradorHUD>();
        if (taulell == null) { Debug.LogError("No trobo el Taulell."); return; }

        Transform t = taulell.transform;
        float m = taulell.mida;
        float x0 = -m * 0.5f - margeNeteja, x1 = (columnes - 1) * m + m * 0.5f + margeNeteja;
        float z0 = -m * 0.5f - margeNeteja, z1 = (files - 1) * m + m * 0.5f + margeNeteja;
        Bounds zona = new Bounds(t.TransformPoint(new Vector3(x0, 0f, z0)), Vector3.zero);
        zona.Encapsulate(t.TransformPoint(new Vector3(x1, 0f, z0)));
        zona.Encapsulate(t.TransformPoint(new Vector3(x0, 0f, z1)));
        zona.Encapsulate(t.TransformPoint(new Vector3(x1, 0f, z1)));
        float yTauler = t.position.y;
        string mida = columnes + "x" + files;

        // Si el Taulell ja te caselles a l'escena (p. ex. generat en DIFICIL), es fa servir la seva mida real
        Renderer[] rt = t.GetComponentsInChildren<Renderer>();
        if (rt.Length > 0)
        {
            Bounds bt = rt[0].bounds;
            for (int i = 1; i < rt.Length; i++) bt.Encapsulate(rt[i].bounds);
            zona = new Bounds(new Vector3(bt.center.x, 0f, bt.center.z), new Vector3(bt.size.x + 2f * margeNeteja, 0f, bt.size.z + 2f * margeNeteja));
            mida = "el que hi ha a l'escena";
        }

        List<GameObject> esborrar = new List<GameObject>();
        foreach (Transform tr in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            GameObject go = tr.gameObject;
            if (tr.IsChildOf(t) || tr.IsChildOf(transform) || (hud != null && tr.IsChildOf(hud.transform))) continue;
            if (PrefabUtility.GetNearestPrefabInstanceRoot(go) != go) continue;          // nomes models sencers (arbre, arbust, pedra...)
            string n = go.name.ToLower();
            if (n.Contains("illa") || n.Contains("escenari") || n.Contains("taulell") || n.Contains("casella")) continue;

            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.x > 25f || b.size.z > 25f) continue;                              // res massa gran (illes, castell...)
            if (b.min.y > yTauler + 20f || b.max.y < yTauler - 3f) continue;               // ni les illes flotants ni les roques de sota
            if (b.max.x < zona.min.x || b.min.x > zona.max.x || b.max.z < zona.min.z || b.min.z > zona.max.z) continue;
            esborrar.Add(go);
        }

        int fets = 0, amagats = 0;
        foreach (GameObject go in esborrar)
        {
            if (go == null) continue;
            try { Undo.DestroyObjectImmediate(go); fets++; }
            catch { Undo.RecordObject(go, "Amagar decoracio"); go.SetActive(false); amagats++; }
        }
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("Decoracio sobre el tauler (" + mida + "): " + fets + " esborrats" + (amagats > 0 ? ", " + amagats + " desactivats" : "") + ". Ctrl+Z per desfer.");
    }
#endif
}
