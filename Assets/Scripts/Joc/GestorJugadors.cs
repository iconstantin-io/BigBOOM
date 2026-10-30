using UnityEngine;
using UnityEngine.SceneManagement;

// Mou els jugadors entre les tres zones de la partida i decideix on mira la camera:
//  - Preparacio (compte enrere): vista aeria. El jugador del torn apareix a la zona que s'obre sola del mapa
//    (o a la vora del tauler si no n'hi ha) i l'altre a l'illa d'espera.
//  - Torn: la camera segueix el jugador del torn (3a persona amb mira) i nomes ell es pot moure i picar caselles.
//  - Fi del torn: vista aeria per veure les mines. Fi de la partida: els dos jugadors tornen al tauler.
// Si a l'escena no hi ha cap GestorJugadors, se n'afegeix un sol (veure ArrencadaJoc, a sota).
public class GestorJugadors : MonoBehaviour
{
    public static GestorJugadors Instancia { get; private set; }

    [Header("Referencies (si es deixen buides, es busquen soles)")]
    public JocBuscamines joc;
    public CameraJoc camara;
    public PlayerController jugador1;
    [Tooltip("Si es buit, es fa una copia del jugador 1")]
    public PlayerController jugador2;

    [Header("Spawns (opcionals: si son buits es calculen amb el tauler i l'Illa_Espera)")]
    [Tooltip("El jugador del torn apareix a la zona que s'obre sola al principi de cada torn (canvia a cada mapa)")]
    public bool apareixerAZonaInicial = true;
    public Transform spawnTaulell;
    public Transform espera1;
    public Transform espera2;
    [Tooltip("Metres entre la vora del tauler i el punt on apareix el jugador del torn")]
    public float margeSpawn = 2.5f;
    [Tooltip("Separacio entre els dos jugadors a la zona d'espera i al final")]
    public float separacio = 3f;
    public string nomIllaEspera = "Illa_Espera";

    [Header("Objectes de l'escena que s'activen en comencar (a l'editor estan desactivats)")]
    public string[] activarAlComencar = { "Escenari_principal", "Taulell", "HUD", "Joc" };

    [Header("Jugabilitat")]
    [Tooltip("Distancia maxima (m) des del jugador fins a la casella que pica. 0 = sense limit")]
    public float abast = 12f;
    [Tooltip("Si el jugador cau per sota del tauler aquests metres, torna al punt d'inici")]
    public float alturaCaiguda = 20f;

    readonly PlayerController[] jugadors = new PlayerController[2];
    readonly MarcadorJugador[] marcadors = new MarcadorJugador[2];
    Transform illaEspera;
    JocBuscamines.Estat estatAnterior;
    int tornAnterior = -1;
    bool primerCop = true;

    // Online: posicio que arriba del jugador que es mou a l'altre ordinador
    readonly Vector3[] posRemota = new Vector3[2];
    readonly Quaternion[] rotRemota = new Quaternion[2];
    readonly bool[] caminaRemot = new bool[2];
    readonly bool[] teRemota = new bool[2];
    float tempsEnviarPos;
    BotBuscamines bot;

    public PlayerController Jugador(int i) { return jugadors[i]; }

    void Awake()
    {
        Instancia = this;
        ActivarObjectes();

        if (joc == null) joc = FindFirstObjectByType<JocBuscamines>(FindObjectsInactive.Include);
        if (joc == null) { Debug.LogError("GestorJugadors: no hi ha cap JocBuscamines a l'escena."); enabled = false; return; }
        if (!joc.gameObject.activeInHierarchy) joc.gameObject.SetActive(true);

        Camera cam = joc.camJoc != null ? joc.camJoc : Camera.main;
        if (camara == null && cam != null)
        {
            camara = cam.GetComponent<CameraJoc>();
            if (camara == null) camara = cam.gameObject.AddComponent<CameraJoc>();
        }

        if (FindFirstObjectByType<HudJugador>() == null) gameObject.AddComponent<HudJugador>().joc = joc;
        if (FindFirstObjectByType<PauseMenuController>() == null) gameObject.AddComponent<PauseMenuController>();
        UIKit.AssegurarEventSystem();

        joc.usarMira = true;
        joc.abast = abast;

        if (ConfigPartida.Mode == ConfigPartida.ModeJoc.Bot) bot = gameObject.AddComponent<BotBuscamines>();
        if (ConfigPartida.EsOnline && XarxaBigBOOM.Instancia != null)
        {
            XarxaBigBOOM.Instancia.OnMissatge += AlMissatge;
            XarxaBigBOOM.Instancia.OnDesconnectat += AlDesconnectar;
        }
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        if (XarxaBigBOOM.Instancia != null)
        {
            XarxaBigBOOM.Instancia.OnMissatge -= AlMissatge;
            XarxaBigBOOM.Instancia.OnDesconnectat -= AlDesconnectar;
        }
    }

    void Start()
    {
        PrepararJugadors();
        illaEspera = BuscarPerNom(nomIllaEspera);
        if (illaEspera == null) Debug.LogWarning("GestorJugadors: no trobo '" + nomIllaEspera + "', el jugador que espera es quedara al costat del tauler.");
        if (bot != null)
        {
            bot.joc = joc;
            bot.avatar = jugadors[1];
        }
    }

    // ---------- Online ----------
    void AlMissatge(string[] m)
    {
        if (m[0] != "POS" || m.Length < 7) return;
        int j = XarxaBigBOOM.I(m[1]);
        if (j < 0 || j > 1) return;
        posRemota[j] = new Vector3(XarxaBigBOOM.F(m[2]), XarxaBigBOOM.F(m[3]), XarxaBigBOOM.F(m[4]));
        rotRemota[j] = Quaternion.Euler(0f, XarxaBigBOOM.F(m[5]), 0f);
        caminaRemot[j] = m[6] == "1";
        teRemota[j] = true;
    }

    void AlDesconnectar(string motiu)
    {
        HudJugador hud = FindFirstObjectByType<HudJugador>();
        if (hud != null) hud.Avisar("Partida acabada", motiu + "  ·  Tornant al menú...", UIKit.Vermell, 4f);
        StartCoroutine(TornarAlMenu(3.5f));
    }

    System.Collections.IEnumerator TornarAlMenu(float segons)
    {
        yield return new WaitForSecondsRealtime(segons);
        Time.timeScale = 1f;
        XarxaBigBOOM.Destruir();
        SceneManager.LoadScene(PauseMenuController.Instancia != null ? PauseMenuController.Instancia.escenaMenu : "MainMenu");
    }

    // El jugador que es mou en aquest ordinador envia la seva posicio; el de l'altre ordinador la segueix suaument
    void SincronitzarPosicions()
    {
        if (!ConfigPartida.EsOnline || !XarxaBigBOOM.Connectat || joc.EstatActual != JocBuscamines.Estat.Jugant) return;
        int a = joc.JugadorActual;
        PlayerController pj = jugadors[a];
        if (ConfigPartida.JugadorLocal(a))
        {
            tempsEnviarPos += Time.deltaTime;
            if (tempsEnviarPos < 1f / 15f) return;
            tempsEnviarPos = 0f;
            Vector3 p = pj.transform.position;
            XarxaBigBOOM.Instancia.Enviar("POS", a, p.x, p.y, p.z, pj.transform.eulerAngles.y, pj.Caminant ? 1 : 0);
        }
        else if (teRemota[a])
        {
            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            pj.transform.SetPositionAndRotation(Vector3.Lerp(pj.transform.position, posRemota[a], k),
                Quaternion.Slerp(pj.transform.rotation, rotRemota[a], k));
            pj.PosarCaminant(caminaRemot[a]);
        }
    }

    void ActivarObjectes()
    {
        Scene escena = gameObject.scene;
        GameObject[] arrels = escena.GetRootGameObjects();
        foreach (string nom in activarAlComencar)
            foreach (GameObject g in arrels)
                if (g.name == nom && !g.activeSelf)
                {
                    g.SetActive(true);
                    Debug.Log("GestorJugadors: activat '" + nom + "' per poder jugar.");
                }
    }

    void PrepararJugadors()
    {
        if (jugador1 == null || jugador2 == null)
        {
            PlayerController[] tots = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            System.Array.Sort(tots, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (PlayerController p in tots)
            {
                if (jugador1 == null) jugador1 = p;
                else if (jugador2 == null && p != jugador1) jugador2 = p;
            }
        }
        if (jugador1 == null) { Debug.LogError("GestorJugadors: no hi ha cap jugador (PlayerController) a l'escena."); enabled = false; return; }
        if (jugador2 == null)
        {
            jugador2 = Instantiate(jugador1.gameObject, jugador1.transform.position + Vector3.right * separacio, jugador1.transform.rotation).GetComponent<PlayerController>();
            jugador2.name = "Player2";
        }
        jugadors[0] = jugador1;
        jugadors[1] = jugador2;

        for (int i = 0; i < 2; i++)
        {
            jugadors[i].gameObject.SetActive(true);
            // Controls de 3a persona: WASD segons la camera, el ratoli orbita (CameraJoc)
            jugadors[i].relatiuCamera = true;
            jugadors[i].referenciaCamera = camara != null ? camara.transform : null;
            marcadors[i] = jugadors[i].GetComponent<MarcadorJugador>();
            if (marcadors[i] == null) marcadors[i] = jugadors[i].gameObject.AddComponent<MarcadorJugador>();
            marcadors[i].color = UIKit.ColorJugador(i);
            marcadors[i].nom = joc.Nom(i);
            Control(i, false);
        }
    }

    void Update()
    {
        if (joc == null || jugadors[0] == null || !joc.TaulellPreparat) return;

        JocBuscamines.Estat e = joc.EstatActual;
        if (primerCop || e != estatAnterior || joc.Torn != tornAnterior)
        {
            CanviFase(e);
            estatAnterior = e;
            tornAnterior = joc.Torn;
            primerCop = false;
        }

        SincronitzarPosicions();

        // Si el jugador del torn (d'aquest ordinador) cau de l'illa, torna al punt d'inici
        if (e == JocBuscamines.Estat.Jugant && ConfigPartida.JugadorLocal(joc.JugadorActual))
        {
            PlayerController pj = jugadors[joc.JugadorActual];
            if (pj.transform.position.y < joc.LimitsTaulell.min.y - alturaCaiguda)
            {
                Quaternion r;
                Vector3 p = PuntTaulell(out r);
                pj.Teletransportar(p, r);
            }
        }
    }

    void CanviFase(JocBuscamines.Estat e)
    {
        int actiu = joc.JugadorActual, altre = 1 - actiu;
        Quaternion rot;
        Vector3 pos;

        switch (e)
        {
            case JocBuscamines.Estat.Preparacio:
                Control(0, false);
                Control(1, false);
                for (int i = 0; i < 2; i++) if (marcadors[i] != null) marcadors[i].PosarNom(joc.Nom(i));
                pos = PuntTaulell(out rot);
                jugadors[actiu].Teletransportar(pos, rot);
                pos = PuntEspera(altre, out rot);
                jugadors[altre].Teletransportar(pos, rot);
                VistaAeria(primerCop);
                BloquejarCursor(true);
                break;

            case JocBuscamines.Estat.Jugant:
                bool local = ConfigPartida.JugadorLocal(actiu);
                Control(actiu, local);
                Control(altre, false);
                joc.jugadorActiu = jugadors[actiu].transform;
                teRemota[actiu] = false;
                // El teu torn: camera darrere del personatge. Torn de l'altre (online o bot): la mateixa vista aeria fixa del compte enrere
                if (local)
                {
                    if (camara != null) camara.Seguir(jugadors[actiu].transform);
                    MostrarPantallaHud(true);
                }
                else VistaAeria(false);
                marcadors[actiu].Mostrar(!local);
                BloquejarCursor(true);
                break;

            case JocBuscamines.Estat.FiTorn:
                Control(0, false);
                Control(1, false);
                VistaAeria(false);
                break;

            case JocBuscamines.Estat.FiPartida:
                Control(0, false);
                Control(1, false);
                Vector3 centre = PuntTaulell(out rot);
                Vector3 costat = rot * Vector3.right * separacio * 0.5f;
                for (int i = 0; i < 2; i++)
                {
                    Vector3 p = centre + (i == 0 ? -costat : costat);
                    p.y = Terra(p, centre.y);
                    jugadors[i].Teletransportar(p, rot);
                }
                VistaAeria(false);
                BloquejarCursor(false);
                break;
        }
    }

    void Control(int i, bool actiu)
    {
        PlayerController p = jugadors[i];
        if (p == null) return;
        if (!actiu) p.Aturar();
        p.enabled = actiu;
        if (marcadors[i] != null) marcadors[i].Mostrar(true);
    }

    void VistaAeria(bool alMoment)
    {
        if (camara == null) return;
        Vector3 pos;
        Quaternion rot;
        joc.CalcularVistaAeria(out pos, out rot);
        camara.VistaAeria(pos, rot);
        if (alMoment) camara.SaltarTransicio();
        MostrarPantallaHud(!joc.vistaZenital);
    }

    // La pantalla flotant del HUD queda just sota la camera zenital: s'amaga mentre dura la vista aeria
    void MostrarPantallaHud(bool visible)
    {
        if (joc.hud != null && joc.hud.pantalla != null) joc.hud.pantalla.gameObject.SetActive(visible);
    }

    void BloquejarCursor(bool bloquejar)
    {
        if (PauseMenuController.EstaPausat) return;
        Cursor.lockState = bloquejar ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquejar;
    }

    // ---------- Spawns ----------
    // On apareix el jugador del torn: a la zona que s'obre al principi (si n'hi ha) o a la vora del tauler
    Vector3 PuntTaulell(out Quaternion rot)
    {
        Bounds b = joc.LimitsTaulell;
        Vector3 centre = b.center;
        if (spawnTaulell != null)
        {
            rot = spawnTaulell.rotation;
            return spawnTaulell.position;
        }

        Vector3 z;
        if (apareixerAZonaInicial && joc.PuntZonaInicial(out z))
        {
            z.y = Terra(z, z.y);
            Vector3 cap = centre - z;
            cap.y = 0f;
            rot = cap.sqrMagnitude > 9f ? Quaternion.LookRotation(cap) : Quaternion.LookRotation(Vector3.right);
            return z;
        }

        // Vora del tauler del costat de la camera aeria, mirant cap al centre
        Vector3 d = joc.posicioCamera;
        Vector3 p;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.z))
            p = new Vector3(d.x < 0f ? b.min.x - margeSpawn : b.max.x + margeSpawn, b.max.y, centre.z);
        else
            p = new Vector3(centre.x, b.max.y, d.z < 0f ? b.min.z - margeSpawn : b.max.z + margeSpawn);
        p.y = Terra(p, b.max.y);
        rot = MirarCap(p, centre);
        return p;
    }

    // Lloc de cada jugador a la zona d'espera (illa flotant), mirant cap al tauler
    Vector3 PuntEspera(int i, out Quaternion rot)
    {
        Transform t = i == 0 ? espera1 : espera2;
        if (t != null)
        {
            rot = t.rotation;
            return t.position;
        }

        Vector3 centreTaulell = joc.LimitsTaulell.center;
        if (illaEspera == null)
        {
            Quaternion r;
            Vector3 b = PuntTaulell(out r);
            Vector3 p0 = b - r * Vector3.forward * 4f + r * Vector3.right * (i == 0 ? -1f : 1f) * separacio;
            p0.y = Terra(p0, b.y);
            rot = r;
            return p0;
        }

        // Una mica cap a la vora de l'illa que dona al tauler, per poder-lo veure
        Vector3 c = illaEspera.position;
        Vector3 dir = DireccioTaulell(c);
        Vector3 dreta = Vector3.Cross(Vector3.up, dir);
        Vector3 p = c + dir * RadiIllaEspera() * 0.5f + dreta * (i == 0 ? -0.5f : 0.5f) * separacio;
        p.y = Terra(p, c.y);
        rot = MirarCap(p, centreTaulell);
        return p;
    }

    Vector3 DireccioTaulell(Vector3 des)
    {
        Vector3 dir = joc.LimitsTaulell.center - des;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
    }

    float RadiIllaEspera()
    {
        Renderer r = illaEspera != null ? illaEspera.GetComponent<Renderer>() : null;
        return r != null ? Mathf.Max(6f, Mathf.Min(r.bounds.extents.x, r.bounds.extents.z) * 0.9f) : 12f;
    }

    static Quaternion MirarCap(Vector3 des, Vector3 cap)
    {
        Vector3 d = cap - des;
        d.y = 0f;
        return d.sqrMagnitude > 0.001f ? Quaternion.LookRotation(d) : Quaternion.identity;
    }

    // Alcada del terra en aquest punt (ignora els jugadors i el que quedi molt per sobre, com les copes dels arbres).
    // Si no hi ha res, torna yPerDefecte
    float Terra(Vector3 p, float yPerDefecte)
    {
        RaycastHit[] cops = Physics.RaycastAll(new Vector3(p.x, yPerDefecte + 25f, p.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
        float millor = float.NegativeInfinity;
        foreach (RaycastHit c in cops)
        {
            if (c.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (c.point.y > yPerDefecte + 2f) continue;
            if (c.point.y > millor) millor = c.point.y;
        }
        return float.IsNegativeInfinity(millor) ? yPerDefecte : millor + 0.05f;
    }

    Transform BuscarPerNom(string nom)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == nom && t.gameObject.scene == gameObject.scene) return t;
        return null;
    }
}

// Afegeix el GestorJugadors (i amb ell el HUD del jugador, la pausa i la camera) a qualsevol escena
// que tingui un JocBuscamines, encara que el Joc estigui desactivat a l'editor.
static class ArrencadaJoc
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Inici()
    {
        SceneManager.sceneLoaded -= AlCarregar;
        SceneManager.sceneLoaded += AlCarregar;
        Preparar(SceneManager.GetActiveScene());
    }

    static void AlCarregar(Scene escena, LoadSceneMode mode)
    {
        Preparar(escena);
    }

    static void Preparar(Scene escena)
    {
        foreach (GestorJugadors g in Object.FindObjectsByType<GestorJugadors>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (g.gameObject.scene == escena) return;

        bool teJoc = false;
        foreach (JocBuscamines j in Object.FindObjectsByType<JocBuscamines>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (j.gameObject.scene == escena) teJoc = true;
        if (!teJoc) return;

        GameObject g2 = new GameObject("GestorPartida");
        SceneManager.MoveGameObjectToScene(g2, escena);
        g2.AddComponent<GestorJugadors>();
    }
}
