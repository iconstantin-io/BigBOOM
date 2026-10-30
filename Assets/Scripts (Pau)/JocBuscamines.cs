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
// Clic esquerre: destapar casella. Clic dret (o F): posar o treure bandera.
// Si s'entra des del menu principal, la mida del mapa i les mines venen del nivell triat (ConfigPartida).
public class JocBuscamines : MonoBehaviour
{
    [Header("Referencies (si es deixen buides, es busquen soles)")]
    public GeneradorTaulell taulell;
    public GeneradorHUD hud;
    public Camera camJoc;

    [Header("Jugadors")]
    public string nomJugador1 = "PLAYER_1";
    public string nomJugador2 = "PLAYER_2";

    [Header("Mapa (mida de disseny: el tauler es centra sobre aquesta mida)")]
    public int columnes = 16;
    public int files = 16;
    public int mines = 40;
    [Tooltip("Centra els taulells mes petits o mes grans sobre la zona de Columnes x Files")]
    public bool centrarTaulell = true;
    [Tooltip("Amaga arbres, arbustos, pedres... que quedin a sobre del tauler (per si l'escena no s'ha netejat amb BigBOOM > Preparar escena del mapa)")]
    public bool amagarDecoracio = true;

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
    [Tooltip("Desactivat: es pica on hi ha el ratoli. Activat: es pica al centre de la pantalla (mira, 1a/3a persona)")]
    public bool usarMira = false;
    [Tooltip("Distancia maxima (m) entre el jugador i la casella per poder-la picar. 0 = sense limit")]
    public float abast = 0f;
    [HideInInspector] public Transform jugadorActiu;

    [Header("Camera aeria")]
    public bool colocarCamera = true;
    [Tooltip("Activat: la vista aeria mira el tauler des de dalt, en vertical. Desactivat: des d'un costat (Posicio Camera)")]
    public bool vistaZenital = true;
    [Tooltip("Espai lliure al voltant del tauler a la vista zenital (1 = el tauler ocupa tota la pantalla)")]
    public float margeZenital = 1.2f;
    [Tooltip("Posicio de la camera respecte al centre del tauler. Per defecte des d'un costat (-X): a -Z hi ha l'illa d'espera i a +Z el HUD")]
    public Vector3 posicioCamera = new Vector3(-60f, 50f, 15f);
    [Tooltip("Si no hi ha HUD, alcada del punt on mira la camera. Si hi ha HUD, mira al punt mig entre el tauler i el HUD")]
    public float alturaMirada = 12f;

    [Header("Bandera (opcional: si es buit, se'n crea una d'animada per codi)")]
    public GameObject prefabBandera;

    public enum Estat { Preparacio, Jugant, FiTorn, FiPartida }
    Estat estat = Estat.Preparacio;

    // Estat del mapa
    int F, C;
    int columnesDisseny, filesDisseny;
    bool[,] esMina;
    int[,] num;
    bool[,] oberta;
    GameObject[,] caselles;
    GameObject[,] banderes;
    Transform[] filesObj;
    readonly Dictionary<Transform, Vector2Int> mapa = new Dictionary<Transform, Vector2Int>();
    Bounds limitsTaulell;
    bool decoracioAmagada = false;
    int filaInicial = -1, columnaInicial = -1;   // casella des d'on s'obre la zona inicial

    // Estat de la partida
    int torn = 0;
    int jugador = 0;            // 0 = jugador 1, 1 = jugador 2
    readonly int[] punts = new int[2];
    int puntsTorn = 0;
    int nBanderes = 0;
    float tempsRestant = 0f;
    float duradaFase = 1f;
    bool explotat = false;
    bool netejat = false;
    string textFinal = "";
    bool avisFalten = false;
    bool hiHaHudJugador = false;

    // ---------- Dades per al HUD i el GestorJugadors ----------
    public Estat EstatActual { get { return estat; } }
    public int Torn { get { return Mathf.Clamp(torn, 0, tornsTotals); } }
    public int JugadorActual { get { return jugador; } }
    public int PuntsTorn { get { return puntsTorn; } }
    public float TempsRestant { get { return tempsRestant; } }
    public float DuradaFase { get { return duradaFase; } }
    public string TextFinal { get { return textFinal; } }
    public int Mines { get { return F * C > 0 ? Mathf.Min(mines, F * C) : mines; } }
    public int Banderes { get { return nBanderes; } }
    public Bounds LimitsTaulell { get { return limitsTaulell; } }
    public bool TaulellPreparat { get { return caselles != null && limitsTaulell.size.sqrMagnitude > 0f; } }
    // Nomes es pot picar si es el torn d'un jugador que es juga des d'aquest ordinador
    public bool PotPicar { get { return estat == Estat.Jugant && !PauseMenuController.EstaPausat && ConfigPartida.JugadorLocal(jugador); } }
    public int Punts(int j) { return punts[j]; }
    public string Nom(int j) { return j == 0 ? nomJugador1 : nomJugador2; }
    public int FilesActuals { get { return F; } }
    public int ColumnesActuals { get { return C; } }
    public bool EsperantRival { get; private set; }

    // Online: aquest ordinador fa de host (decideix el mapa i les fases) o de client (les rep)
    bool esHost, esClient;
    float tempsSincronitzacio;

    public event System.Action<Vector3> OnExplosio;
    public event System.Action<int, int, int, bool> OnTornAcabat;   // punts del torn, banderes bones, dolentes, ha explotat
    public event System.Action<bool> OnBandera;                     // true = posada, false = treta

    // ---------------------------------------------------------------
    void Start()
    {
        if (taulell == null) taulell = FindFirstObjectByType<GeneradorTaulell>();
        if (hud == null) hud = FindFirstObjectByType<GeneradorHUD>();
        if (camJoc == null) camJoc = Camera.main;

        if (taulell == null) { Debug.LogError("JocBuscamines: no trobo cap GeneradorTaulell a l'escena (esta desactivat?)."); enabled = false; return; }
        if (camJoc == null) { Debug.LogError("JocBuscamines: no hi ha cap camera amb l'etiqueta MainCamera."); enabled = false; return; }

        columnesDisseny = columnes;
        filesDisseny = files;
        if (ConfigPartida.Definida)
        {
            // Tots els nivells es centren sobre la zona de l'inspector (Columnes x Files)
            columnes = ConfigPartida.NivellActual.columnes;
            files = ConfigPartida.NivellActual.files;
            mines = ConfigPartida.NivellActual.mines;
        }

        // Noms dels jugadors: els de la partida (online / bot) o el que s'ha escrit al menu principal
        if (ConfigPartida.NomsPartida != null)
        {
            if (!string.IsNullOrEmpty(ConfigPartida.NomsPartida[0])) nomJugador1 = ConfigPartida.NomsPartida[0];
            if (!string.IsNullOrEmpty(ConfigPartida.NomsPartida[1])) nomJugador2 = ConfigPartida.NomsPartida[1];
        }
        else if (!string.IsNullOrEmpty(ConfigPartida.NomJugador1)) nomJugador1 = ConfigPartida.NomJugador1;

        if (hud == null) Debug.LogWarning("JocBuscamines: no he trobat el HUD, el joc funcionara igualment pero sense marcador.");
        else hud.PosarNoms(nomJugador1, nomJugador2);
        hiHaHudJugador = FindFirstObjectByType<HudJugador>() != null;

        esHost = ConfigPartida.Mode == ConfigPartida.ModeJoc.OnlineHost && XarxaBigBOOM.Connectat;
        esClient = ConfigPartida.Mode == ConfigPartida.ModeJoc.OnlineClient && XarxaBigBOOM.Connectat;
        if (esHost || esClient) XarxaBigBOOM.Instancia.OnMissatge += AlMissatge;

        // El client no porta la partida: espera els missatges del host
        if (esClient) XarxaBigBOOM.Instancia.Enviar("LLEST");
        else StartCoroutine(Partida());
    }

    void OnDestroy()
    {
        if (XarxaBigBOOM.Instancia != null) XarxaBigBOOM.Instancia.OnMissatge -= AlMissatge;
    }

    IEnumerator Partida()
    {
        // Online: no es comenca fins que el rival ha carregat l'escena
        if (esHost)
        {
            EsperantRival = true;
            while (!XarxaBigBOOM.Instancia.RivalLlest)
            {
                if (!XarxaBigBOOM.Connectat) yield break;
                yield return null;
            }
            EsperantRival = false;
        }

        for (torn = 1; torn <= tornsTotals; torn++)
        {
            jugador = (torn - 1) % 2;
            int llavor = taulell.llavor != 0 ? taulell.llavor + torn : new System.Random().Next();
            ComencarTorn(llavor);
            if (esHost) Enviar("TORN", torn, jugador, llavor);

            // Vista aeria + compte enrere (no es pot picar)
            while (tempsRestant > 0f) { tempsRestant -= Time.deltaTime; yield return null; }

            // Torn
            estat = Estat.Jugant;
            tempsRestant = duradaFase = tempsTorn;
            if (esHost) Enviar("JUGAR");
            while (tempsRestant > 0f && !explotat && !netejat) { tempsRestant -= Time.deltaTime; yield return null; }
            tempsRestant = 0f;

            // Fi del torn: es compten les banderes i es mostren les mines
            estat = Estat.FiTorn;
            TancarTorn();
            if (esHost) Enviar("FITORN", punts[0], punts[1], puntsTorn);
            yield return new WaitForSeconds(tempsFiTorn);
        }

        AcabarPartida();
        if (esHost) Enviar("FI", punts[0], punts[1]);
    }

    // Prepara el mapa d'un torn (el host i el client el generen igual amb la mateixa llavor)
    void ComencarTorn(int llavor)
    {
        GenerarMapa(llavor);
        if (torn == 1 && colocarCamera) ColocarCamera();
        if (!decoracioAmagada && amagarDecoracio) { AmagarDecoracioSobreTaulell(); decoracioAmagada = true; }
        estat = Estat.Preparacio;
        puntsTorn = 0;
        explotat = false;
        netejat = false;
        tempsRestant = duradaFase = tempsVistaAeria + tempsCompteEnrere;
    }

    void AcabarPartida()
    {
        estat = Estat.FiPartida;
        if (punts[0] > punts[1]) textFinal = "GUANYA " + nomJugador1;
        else if (punts[1] > punts[0]) textFinal = "GUANYA " + nomJugador2;
        else textFinal = "EMPAT";
        Debug.Log("Fi de la partida: " + textFinal + " (" + punts[0] + " - " + punts[1] + ")");
    }

    void Enviar(params object[] parts)
    {
        if (XarxaBigBOOM.Instancia != null) XarxaBigBOOM.Instancia.Enviar(parts);
    }

    // ---------- Online ----------
    void AlMissatge(string[] m)
    {
        if (esHost)
        {
            // El client demana fer una accio en el seu torn: es valida, s'aplica i es reenvia
            if (m[0] == "PETICIO" && m.Length >= 4 && estat == Estat.Jugant && !ConfigPartida.JugadorLocal(jugador))
            {
                bool destapar = m[1] == "D";
                int f = XarxaBigBOOM.I(m[2]), c = XarxaBigBOOM.I(m[3]);
                AplicarAccio(destapar, f, c);
                Enviar("ACCIO", m[1], f, c);
            }
            return;
        }

        switch (m[0])
        {
            case "TORN":
                torn = XarxaBigBOOM.I(m[1]);
                jugador = XarxaBigBOOM.I(m[2]);
                ComencarTorn(XarxaBigBOOM.I(m[3]));
                break;
            case "JUGAR":
                estat = Estat.Jugant;
                tempsRestant = duradaFase = tempsTorn;
                break;
            case "TEMPS":
                tempsRestant = XarxaBigBOOM.F(m[1]);
                break;
            case "ACCIO":
                AplicarAccio(m[1] == "D", XarxaBigBOOM.I(m[2]), XarxaBigBOOM.I(m[3]));
                break;
            case "FITORN":
                estat = Estat.FiTorn;
                tempsRestant = 0f;
                TancarTorn();
                // els punts bons son els del host
                punts[0] = XarxaBigBOOM.I(m[1]);
                punts[1] = XarxaBigBOOM.I(m[2]);
                puntsTorn = XarxaBigBOOM.I(m[3]);
                break;
            case "FI":
                punts[0] = XarxaBigBOOM.I(m[1]);
                punts[1] = XarxaBigBOOM.I(m[2]);
                AcabarPartida();
                break;
        }
    }

    // Accio d'un jugador d'aquest ordinador o del bot. El client nomes l'envia: l'aplica quan el host la confirma
    public void ExecutarAccio(bool destapar, int f, int c)
    {
        if (esClient) { Enviar("PETICIO", destapar ? "D" : "B", f, c); return; }
        AplicarAccio(destapar, f, c);
        if (esHost) Enviar("ACCIO", destapar ? "D" : "B", f, c);
    }

    void AplicarAccio(bool destapar, int f, int c)
    {
        if (oberta == null || f < 0 || f >= F || c < 0 || c >= C) return;
        if (destapar) Destapar(f, c);
        else CanviarBandera(f, c);
    }

    void Update()
    {
        if (hud != null)
        {
            string nom = estat == Estat.FiPartida ? textFinal : (jugador == 0 ? nomJugador1 : nomJugador2);
            hud.ActualitzarHUD(Mathf.Clamp(torn, 0, tornsTotals), nom, tempsRestant, puntsTorn, punts[0], punts[1]);
        }

        // Online: el host envia el temps cada mig segon; el client el va descomptant mentrestant
        if (esHost && (estat == Estat.Preparacio || estat == Estat.Jugant) && caselles != null)
        {
            tempsSincronitzacio += Time.deltaTime;
            if (tempsSincronitzacio >= 0.5f) { tempsSincronitzacio = 0f; Enviar("TEMPS", tempsRestant); }
        }
        if (esClient && (estat == Estat.Preparacio || estat == Estat.Jugant))
            tempsRestant = Mathf.Max(0f, tempsRestant - Time.deltaTime);

        if (!PotPicar) return;
        Mouse ratoli = Mouse.current;
        Keyboard teclat = Keyboard.current;
        bool esquerre = ratoli != null && ratoli.leftButton.wasPressedThisFrame;
        bool dret = (ratoli != null && ratoli.rightButton.wasPressedThisFrame) || (teclat != null && teclat.fKey.wasPressedThisFrame);
        if (!esquerre && !dret) return;

        Vector2Int p;
        Vector3 punt;
        if (!CasellaApuntada(out p, out punt) || !DinsAbast(punt)) return;

        // El personatge es gira cap a la casella que pica
        if (jugadorActiu != null)
        {
            PlayerController pc = jugadorActiu.GetComponent<PlayerController>();
            if (pc != null) pc.MirarCap(punt);
        }

        ExecutarAccio(esquerre, p.y, p.x);
    }

    // Casella que hi ha sota la mira (o sota el ratoli). p = (columna, fila)
    public bool CasellaApuntada(out Vector2Int p, out Vector3 punt)
    {
        p = Vector2Int.zero;
        punt = Vector3.zero;
        if (camJoc == null || caselles == null) return false;

        Vector2 pantalla;
        if (usarMira) pantalla = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        else if (Mouse.current != null) pantalla = Mouse.current.position.ReadValue();
        else return false;

        Ray raig = camJoc.ScreenPointToRay(pantalla);
        RaycastHit[] cops = Physics.RaycastAll(raig, 1000f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(cops, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit cop in cops)
        {
            if (!TrobarCasella(cop.collider.transform, out p)) continue;
            punt = cop.point;
            return true;
        }
        return false;
    }

    public bool DinsAbast(Vector3 punt)
    {
        if (abast <= 0f || jugadorActiu == null) return true;
        Vector3 d = punt - jugadorActiu.position;
        d.y = 0f;
        return d.magnitude <= abast;
    }

    // Centre (a dalt) de la casella on s'ha obert la zona inicial del torn: alla hi apareix el jugador
    public bool PuntZonaInicial(out Vector3 pos)
    {
        pos = limitsTaulell.center;
        if (!taulell.obrirZonaInicial || caselles == null || filaInicial < 0) return false;
        Bounds b;
        if (!LimitsCasella(new Vector2Int(columnaInicial, filaInicial), out b)) return false;
        pos = new Vector3(b.center.x, b.max.y, b.center.z);
        return true;
    }

    // Limits reals de la casella segons els seus colliders. No es fan servir els models: les caselles tapades
    // amb mina porten la mina a dins, desplacada, i el centre quedaria mogut (i delataria la mina)
    public bool LimitsCasella(Vector2Int p, out Bounds b)
    {
        b = new Bounds();
        if (caselles == null) return false;
        GameObject cas = caselles[p.y, p.x];
        if (cas == null) return false;
        Collider[] cs = cas.GetComponentsInChildren<Collider>();
        if (cs.Length == 0)
        {
            float m = taulell.mida;
            b = new Bounds(cas.transform.position + Vector3.up * m * 0.5f, new Vector3(m, m, m));
            return true;
        }
        b = cs[0].bounds;
        for (int i = 1; i < cs.Length; i++) b.Encapsulate(cs[i].bounds);
        return true;
    }

    public float MidaCasella { get { return taulell != null ? taulell.mida : 2f; } }

    public bool EsOberta(Vector2Int p) { return oberta != null && oberta[p.y, p.x]; }

    // El que veu un jugador d'una casella: -1 tapada, -2 mina destapada, 0..8 el numero (el bot nomes fa servir aixo)
    public int NumeroVisible(int f, int c)
    {
        if (oberta == null || !oberta[f, c]) return -1;
        return esMina[f, c] ? -2 : num[f, c];
    }
    public bool TeBandera(Vector2Int p) { return banderes != null && banderes[p.y, p.x] != null; }

    void OnGUI()
    {
        if (!usarMira || hiHaHudJugador) return;
        GUIStyle st = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width * 0.5f - 25f, Screen.height * 0.5f - 25f, 50f, 50f), "+", st);
    }

    // ---------------------------------------------------------------
    // Generacio del mapa (mateixes regles que el GeneradorTaulell, pero en temps de joc)
    void GenerarMapa(int llavor)
    {
        F = files; C = columnes;
        avisFalten = false;
        nBanderes = 0;
        System.Random rnd = new System.Random(llavor);

        int sf = rnd.Next(1, F - 1);
        int sc = rnd.Next(1, C - 1);
        filaInicial = sf;
        columnaInicial = sc;

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

        // Limits del tauler (per a la camera i els spawns)
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
        if (centrarTaulell)
        {
            px += (columnesDisseny - C) * mida * 0.5f;
            pz += (filesDisseny - F) * mida * 0.5f;
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
            Vector3 pos = caselles[f, c] != null ? caselles[f, c].transform.position : limitsTaulell.center;
            EfectesJoc.Pop(caselles[f, c], 0.4f, 0.3f);
            Explosio(pos);
            if (OnExplosio != null) OnExplosio(pos);
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
        if (sumarPunts)
        {
            puntsTorn += puntsPerCasella;
            EfectesJoc.Pop(caselles[f, c]);
        }
    }

    void CanviarBandera(int f, int c)
    {
        if (oberta[f, c]) return;
        if (banderes[f, c] != null)
        {
            BanderaAnimada ba = banderes[f, c].GetComponent<BanderaAnimada>();
            if (ba != null) ba.Treure();
            else Destroy(banderes[f, c]);
            banderes[f, c] = null;
            nBanderes--;
            if (OnBandera != null) OnBandera(false);
            return;
        }
        Bounds b;
        if (!LimitsCasella(new Vector2Int(c, f), out b)) return;

        GameObject band;
        if (prefabBandera != null)
        {
            band = Instantiate(prefabBandera);
            band.transform.SetParent(filesObj[f], true);
        }
        else
        {
            band = BanderaAnimada.Crear(filesObj[f], taulell.mida * 0.55f).gameObject;
            band.transform.rotation = Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f);
        }
        band.name = "Bandera_" + (f + 1).ToString("00") + "_" + (c + 1).ToString("00");
        band.transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
        banderes[f, c] = band;
        nBanderes++;
        if (OnBandera != null) OnBandera(true);
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
                if (esMina[f, c] && !oberta[f, c])   // es mostren totes les mines
                {
                    oberta[f, c] = true;
                    Posar(f, c, true);
                    EfectesJoc.Pop(caselles[f, c], 0.5f, 0.3f);
                }
            }
        puntsTorn += bones * puntsBanderaBona + dolentes * puntsBanderaDolenta;
        punts[jugador] += puntsTorn;
        Debug.Log("Fi del torn " + torn + " (" + (jugador == 0 ? nomJugador1 : nomJugador2) + "): banderes bones " + bones
            + ", dolentes " + dolentes + (explotat ? ", ha explotat" : "") + " -> " + puntsTorn + " punts.");
        if (OnTornAcabat != null) OnTornAcabat(puntsTorn, bones, dolentes, explotat);
    }

    void Explosio(Vector3 pos)
    {
        CameraJoc cj = camJoc.GetComponent<CameraJoc>();
        System.Action<float, float> sacsejar;
        if (cj != null) sacsejar = cj.Sacsejar;
        else sacsejar = (intensitat, durada) => StartCoroutine(SacsejarCamera(intensitat, durada));
        EfectesJoc.Explosio(this, pos, taulell.mida * 0.9f, sacsejar);
    }

    IEnumerator SacsejarCamera(float intensitat, float durada)
    {
        Vector3 posCam = camJoc.transform.position;
        for (float t = 0f; t < durada; t += Time.deltaTime)
        {
            float k = 1f - t / durada;
            camJoc.transform.position = posCam + Random.insideUnitSphere * intensitat * k * k;
            yield return null;
        }
        camJoc.transform.position = posCam;
    }

    // Posicio i rotacio de la vista aeria
    public void CalcularVistaAeria(out Vector3 pos, out Quaternion rot)
    {
        Vector3 centre = limitsTaulell.center;
        if (vistaZenital)
        {
            // Des de dalt, amb les columnes (eix X) en horitzontal; alcada justa perque hi capiga tot el tauler
            CameraJoc cj = camJoc.GetComponent<CameraJoc>();
            float fov = cj != null ? cj.FovBase : camJoc.fieldOfView;
            float tanV = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * Mathf.Max(0.1f, camJoc.aspect);
            float alcada = Mathf.Max(limitsTaulell.extents.z / tanV, limitsTaulell.extents.x / tanH) * margeZenital;
            pos = new Vector3(centre.x, limitsTaulell.max.y + alcada, centre.z);
            rot = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            return;
        }
        Vector3 objectiu = centre + Vector3.up * alturaMirada;
        if (hud != null && hud.pantalla != null) objectiu = Vector3.Lerp(centre, hud.pantalla.position, 0.5f);
        pos = centre + posicioCamera;
        rot = Quaternion.LookRotation(objectiu - pos);
    }

    void ColocarCamera()
    {
        Vector3 pos;
        Quaternion rot;
        CalcularVistaAeria(out pos, out rot);
        CameraJoc cj = camJoc.GetComponent<CameraJoc>();
        if (cj != null)
        {
            cj.VistaAeria(pos, rot);
            cj.SaltarTransicio();
            return;
        }
        camJoc.transform.SetPositionAndRotation(pos, rot);
    }

    // Versio en temps de joc de "Esborrar decoracio": nomes desactiva el que toca el tauler (els nivells grans en surten)
    static readonly string[] paraulesDecoracio = { "tree", "bush", "grass", "rock", "flower", "fern", "clover", "pebble",
        "mushroom", "plant", "pine", "spruce", "oak", "birch", "ash", "stump", "foilage", "foliage" };

    void AmagarDecoracioSobreTaulell()
    {
        if (!TaulellPreparat) return;
        Bounds zona = limitsTaulell;
        float yTauler = zona.max.y;
        zona.Expand(new Vector3(4f, 0f, 4f));   // una mica de marge perque res toqui la vora
        HashSet<Transform> vistos = new HashSet<Transform>();
        int amagats = 0;
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            Transform arrel = ArrelDecoracio(r.transform);
            if (arrel == null || !vistos.Add(arrel)) continue;
            if (arrel.IsChildOf(taulell.transform) || arrel.GetComponentInParent<PlayerController>() != null) continue;

            Renderer[] rs = arrel.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.x > 25f || b.size.z > 25f) continue;
            if (b.min.y > yTauler + 20f || b.max.y < yTauler - 3f) continue;
            if (b.max.x < zona.min.x || b.min.x > zona.max.x || b.max.z < zona.min.z || b.min.z > zona.max.z) continue;
            arrel.gameObject.SetActive(false);
            amagats++;
        }
        if (amagats > 0) Debug.Log("JocBuscamines: " + amagats + " elements de decoracio amagats perque tocaven el tauler.");
    }

    // L'avantpassat mes alt que te nom de decoracio (arbre, arbust, pedra...), o null si no n'es
    static Transform ArrelDecoracio(Transform t)
    {
        Transform trobat = null;
        while (t != null)
        {
            string n = t.name.ToLower();
            if (n.Contains("illa") || n.Contains("escenari") || n.Contains("taulell") || n.Contains("casella")) break;
            foreach (string p in paraulesDecoracio)
                if (n.Contains(p)) { trobat = t; break; }
            t = t.parent;
        }
        return trobat;
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
