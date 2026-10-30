using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Menu principal. Si "usarMenuNou" esta activat, amaga els panells antics de l'escena i construeix per codi
// el menu nou: botons a l'esquerra, vista previa animada del joc a la dreta i finestres de nivell, opcions i credits.
public class MainMenuController : MonoBehaviour
{
    [Header("Panels (menu antic)")]
    public GameObject panelPrincipal;
    public GameObject panelPersonajes;
    public GameObject panelConfiguracion;

    [Header("Audio Settings")]
    public Slider volumeSlider;

    [Header("Scene Config")]
    public string nombreEscenaJuego = "EscenaMapa";

    [Header("Menu nou")]
    public bool usarMenuNou = true;

    public static int personajeSeleccionado = 1;

    Canvas canvas;
    GameObject finestraNivell, finestraOpcions, finestraCredits, finestraNom, finestraMode, finestraOnline, finestraEspera;
    TMP_InputField entradaNom, entradaIP;
    TextMeshProUGUI txtBotoNom, txtEstatOnline, txtEspera;
    Image fos;
    RectTransform[] nuvols;
    float[] velNuvols;
    bool carregant;

    const string ClauIP = "BigBOOM_UltimaIP";

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Si es torna d'una partida online, es tanca la connexio
        XarxaBigBOOM.Destruir();
        ConfigPartida.Mode = ConfigPartida.ModeJoc.Local;
        ConfigPartida.NomsPartida = null;

        if (!usarMenuNou)
        {
            // El canvas antic esta desactivat a l'escena: es torna a encendre
            if (panelPrincipal != null) panelPrincipal.transform.root.gameObject.SetActive(true);
            MostrarPanelPrincipal();
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
            return;
        }

        AmagarMenuAntic();
        Construir();
    }

    void Update()
    {
        if (!usarMenuNou || canvas == null) return;

        // Els nuvols es desplacen a poc a poc cap a la dreta i tornen a entrar per l'esquerra
        if (nuvols != null)
        {
            float ample = ((RectTransform)canvas.transform).rect.width;
            for (int i = 0; i < nuvols.Length; i++)
            {
                Vector2 p = nuvols[i].anchoredPosition + Vector2.right * velNuvols[i] * Time.unscaledDeltaTime;
                if (p.x > ample + nuvols[i].sizeDelta.x * 0.5f) p.x = -nuvols[i].sizeDelta.x * 0.5f;
                nuvols[i].anchoredPosition = p;
            }
        }

        // ESC tanca les finestres (menys la del nom, que s'ha de confirmar)
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && (finestraNom == null || !finestraNom.activeSelf)) TancarFinestres();
    }

    // ---------------------------------------------------------------
    void AmagarMenuAntic()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas) continue;
            if (transform.IsChildOf(c.transform))
            {
                // Aquest controlador es dins del canvas antic: nomes s'amaguen els altres fills
                foreach (Transform fill in c.transform)
                    if (!transform.IsChildOf(fill)) fill.gameObject.SetActive(false);
            }
            else c.gameObject.SetActive(false);
        }
    }

    void Construir()
    {
        canvas = UIKit.CrearCanvas("Menu_Principal", 10);
        Transform arrel = canvas.transform;

        // Fons: cel amb sol i nuvols
        RawImage fons = UIKit.Nou("Cel", arrel).gameObject.AddComponent<RawImage>();
        UIKit.Omplir(fons.rectTransform);
        fons.texture = UIKit.Degradat(new Color32(70, 150, 228, 255), new Color32(196, 228, 250, 255));
        Image sol = UIKit.Imatge("Sol", arrel, UIKit.Resplendor, new Color(1f, 0.98f, 0.85f, 0.75f));
        UIKit.Posar(sol.rectTransform, new Vector2(1f, 1f), new Vector2(120f, 160f), new Vector2(900f, 900f));
        CrearNuvols(arrel);

        ConstruirLogo(arrel);
        ConstruirBotons(arrel);
        ConstruirPreview(arrel);

        finestraMode = ConstruirFinestraMode(arrel);
        finestraOnline = ConstruirFinestraOnline(arrel);
        finestraEspera = ConstruirFinestraEspera(arrel);
        finestraNivell = ConstruirFinestraNivell(arrel);
        finestraOpcions = ConstruirFinestraOpcions(arrel);
        finestraCredits = ConstruirFinestraCredits(arrel);
        finestraNom = ConstruirFinestraNom(arrel);
        TancarFinestres();

        fos = UIKit.Imatge("Fos", arrel, null, new Color(0f, 0f, 0f, 0f));
        UIKit.Omplir(fos.rectTransform);

        // El primer cop que s'obre el joc es demana el nom del jugador 1
        if (!ConfigPartida.NomDemanat) ObrirFinestraNom();
    }

    void CrearNuvols(Transform arrel)
    {
        const int n = 11;
        nuvols = new RectTransform[n];
        velNuvols = new float[n];
        for (int i = 0; i < n; i++)
        {
            // Els del fons son mes petits, mes transparents i mes lents (profunditat)
            float k = i / (float)(n - 1);
            float ample = Mathf.Lerp(260f, 720f, k) * Random.Range(0.85f, 1.15f);
            Vector2 mida = new Vector2(ample, ample * 0.5f);
            Vector2 pos = new Vector2(Random.Range(0f, 1920f), Random.Range(420f, 1040f) - k * 260f);

            RectTransform nuvol = UIKit.Posar(UIKit.Nou("Nuvol", arrel), Vector2.zero, pos, mida);
            nuvol.pivot = new Vector2(0.5f, 0.5f);
            Image ombra = UIKit.Imatge("Ombra", nuvol, UIKit.Nuvol, new Color(0.78f, 0.85f, 0.94f, Mathf.Lerp(0.45f, 0.8f, k)));
            UIKit.Omplir(ombra.rectTransform);
            ombra.rectTransform.offsetMin = ombra.rectTransform.offsetMax = new Vector2(0f, -ample * 0.035f);
            Image blanc = UIKit.Imatge("Blanc", nuvol, UIKit.Nuvol, new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 0.97f, k)));
            UIKit.Omplir(blanc.rectTransform);

            nuvols[i] = nuvol;
            velNuvols[i] = Mathf.Lerp(5f, 18f, k);
        }
    }

    // Alcada (respecte al centre de la pantalla) on queden centrats els botons i l'animacio, per sota del logo
    const float AlcadaContingut = -140f;

    // Logo a dalt al centre (Assets/Resources/UI/Logo_BigBOOM.png)
    void ConstruirLogo(Transform arrel)
    {
        Texture2D logo = Resources.Load<Texture2D>("UI/Logo_BigBOOM");
        if (logo != null)
        {
            RawImage img = UIKit.Nou("Logo", arrel).gameObject.AddComponent<RawImage>();
            img.texture = logo;
            img.raycastTarget = false;
            float alt = 250f;
            UIKit.Posar(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(alt * logo.width / logo.height, alt));
            return;
        }
        // Si no hi ha la imatge, es posa el nom en text
        TextMeshProUGUI titol = UIKit.Text("Titol", arrel, "BigBOOM! 3D", 110f, new Color32(20, 20, 24, 255));
        UIKit.Posar(titol.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(900f, 140f));
    }

    // Botons a l'esquerra, a la mateixa alcada que l'animacio i sense cap panell al darrere
    void ConstruirBotons(Transform arrel)
    {
        RectTransform grup = UIKit.Posar(UIKit.Nou("Botons", arrel), new Vector2(0f, 0.5f), new Vector2(150f, AlcadaContingut), new Vector2(500f, 484f));

        Button jugar = UIKit.Boto(grup, "Jugar", UIKit.IconaPlay, UIKit.Verd, UIKit.VerdFosc, Jugar, new Vector2(500f, 116f), 48f, 24f);
        UIKit.Posar((RectTransform)jugar.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(500f, 116f));

        string[] noms = { "Opcions", "Crèdits", "Sortir" };
        Sprite[] icones = { UIKit.IconaEngranatge, UIKit.IconaPersona, UIKit.IconaSortir };
        UnityEngine.Events.UnityAction[] accions = { AbrirConfiguracion, ObrirCredits, SalirDelJuego };
        for (int i = 0; i < noms.Length; i++)
        {
            Button b = UIKit.Boto(grup, noms[i], icones[i], UIKit.Gris, UIKit.GrisFosc, accions[i], new Vector2(500f, 96f), 38f);
            UIKit.Posar((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0f, -152f - i * 116f), new Vector2(500f, 96f));
        }
    }

    // Nomes l'animacio del mini taulell, sense marc ni etiquetes (el fons de la textura es transparent)
    void ConstruirPreview(Transform arrel)
    {
        GameObject gp = new GameObject("Preview");
        MenuPreviewTaulell preview = gp.AddComponent<MenuPreviewTaulell>();
        preview.Inicialitzar(1200, 840);

        RawImage img = UIKit.Nou("Preview_Taulell", arrel).gameObject.AddComponent<RawImage>();
        UIKit.Posar(img.rectTransform, new Vector2(1f, 0.5f), new Vector2(-90f, AlcadaContingut), new Vector2(1060f, 742f));
        img.texture = preview.Textura;
        img.raycastTarget = false;
    }

    // ---------- Finestres ----------
    GameObject Finestra(Transform arrel, string nom, string titol, string subtitol, Vector2 mida, out Transform contingut)
    {
        RectTransform f = UIKit.Nou(nom, arrel);
        UIKit.Omplir(f);
        UIKit.Enfosquir(f, 0.55f);
        Image panell = UIKit.Panell("Panell", f, UIKit.Fosc, 36f);
        UIKit.Posar(panell.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, mida);
        panell.raycastTarget = true;
        panell.gameObject.AddComponent<PanellAnimat>();

        TextMeshProUGUI t = UIKit.Text("Titol", panell.transform, titol, 60f, Color.white);
        UIKit.Posar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(mida.x - 80f, 80f));
        if (!string.IsNullOrEmpty(subtitol))
        {
            TextMeshProUGUI s = UIKit.Text("Subtitol", panell.transform, subtitol, 28f, UIKit.TextSuau, TextAlignmentOptions.Center, FontStyles.Normal);
            UIKit.Posar(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(mida.x - 80f, 40f));
        }

        Button tornar = UIKit.Boto(panell.transform, "Tornar", UIKit.IconaEnrere, UIKit.Gris, UIKit.GrisFosc, TancarFinestres, new Vector2(250f, 80f), 32f);
        UIKit.Posar((RectTransform)tornar.transform, new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(250f, 80f));

        contingut = panell.transform;
        return f.gameObject;
    }

    // ---------- Mode de joc: 2 jugadors, bot o online ----------
    GameObject ConstruirFinestraMode(Transform arrel)
    {
        Transform p;
        GameObject f = Finestra(arrel, "Finestra_Mode", "Com vols jugar?", "Tria contra qui jugaràs la partida", new Vector2(1240f, 720f), out p);
        string[] titols = { "2 jugadors", "Contra el bot", "Online" };
        string[] descs = { "Al mateix ordinador,\nper torns", "Tu contra l'ordinador", "Cadascú des del\nseu ordinador" };
        Sprite[] icones = { UIKit.IconaDosJugadors, UIKit.IconaBot, UIKit.IconaXarxa };
        Color[] colors = { UIKit.Blau, UIKit.Verd, UIKit.Groc };
        Color[] ombres = { UIKit.BlauFosc, UIKit.VerdFosc, UIKit.GrocFosc };
        UnityEngine.Events.UnityAction[] accions =
        {
            () => TriarMode(ConfigPartida.ModeJoc.Local),
            () => TriarMode(ConfigPartida.ModeJoc.Bot),
            () => Obrir(finestraOnline),
        };
        for (int i = 0; i < 3; i++)
        {
            Button b = TargetaMode(p, titols[i], descs[i], icones[i], colors[i], ombres[i], accions[i]);
            UIKit.Posar((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 380f, -10f), new Vector2(340f, 400f));
        }
        return f;
    }

    Button TargetaMode(Transform pare, string titol, string desc, Sprite icona, Color color, Color ombra, UnityEngine.Events.UnityAction accio)
    {
        Button b = UIKit.Boto(pare, "", null, color, ombra, accio, new Vector2(340f, 400f), 10f, 30f);
        BotoAnimat an = b.GetComponent<BotoAnimat>();
        an.escalaHover = 1.06f;
        an.enfonsar = 8f;
        Transform cara = an.cara;

        Image cercle = UIKit.Imatge("Cercle", cara, UIKit.Cercle, new Color(0f, 0f, 0f, 0.14f));
        UIKit.Posar(cercle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(176f, 176f));
        Image ic = UIKit.Imatge("Icona", cercle.transform, icona, Color.white);
        UIKit.Posar(ic.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));

        TextMeshProUGUI t = UIKit.Text("Nom", cara, titol, 42f, Color.white);
        UIKit.Posar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(320f, 56f));
        TextMeshProUGUI d = UIKit.Text("Descripcio", cara, desc, 26f, new Color(1f, 1f, 1f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);
        d.textWrappingMode = TextWrappingModes.Normal;
        UIKit.Posar(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -288f), new Vector2(300f, 80f));
        return b;
    }

    void TriarMode(ConfigPartida.ModeJoc mode)
    {
        ConfigPartida.Mode = mode;
        Obrir(finestraNivell);
    }

    // ---------- Online ----------
    GameObject ConstruirFinestraOnline(Transform arrel)
    {
        Transform p;
        GameObject f = Finestra(arrel, "Finestra_Online", "Partida online", "Els dos ordinadors han d'estar a la mateixa xarxa (o fer servir una VPN com Radmin VPN o ZeroTier)", new Vector2(1240f, 720f), out p);

        // Esquerra: crear partida
        Image e = UIKit.Panell("Crear", p, new Color(1f, 1f, 1f, 0.05f), 28f, false);
        UIKit.Posar(e.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-290f, -20f), new Vector2(520f, 360f));
        TextMeshProUGUI te = UIKit.Text("Titol", e.transform, "Crear partida", 40f, Color.white);
        UIKit.Posar(te.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(480f, 50f));
        TextMeshProUGUI de = UIKit.Text("Text", e.transform, "Tu tries el nivell i jugues com a Jugador 1. L'altre s'hi uneix amb la teva IP.", 25f, UIKit.TextSuau, TextAlignmentOptions.Center, FontStyles.Normal);
        de.textWrappingMode = TextWrappingModes.Normal;
        UIKit.Posar(de.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(440f, 110f));
        Button bc = UIKit.Boto(e.transform, "Crear", UIKit.IconaPlay, UIKit.Verd, UIKit.VerdFosc, () => TriarMode(ConfigPartida.ModeJoc.OnlineHost), new Vector2(360f, 92f), 36f);
        UIKit.Posar((RectTransform)bc.transform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(360f, 92f));

        // Dreta: unir-se amb la IP
        Image u = UIKit.Panell("Unir", p, new Color(1f, 1f, 1f, 0.05f), 28f, false);
        UIKit.Posar(u.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(290f, -20f), new Vector2(520f, 360f));
        TextMeshProUGUI tu = UIKit.Text("Titol", u.transform, "Unir-se a una partida", 40f, Color.white);
        UIKit.Posar(tu.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(480f, 50f));
        entradaIP = UIKit.CampText(u.transform, "IP de l'altre ordinador", new Vector2(440f, 84f), 40, 32f);
        UIKit.Posar((RectTransform)entradaIP.transform, new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(440f, 84f));
        entradaIP.text = PlayerPrefs.GetString(ClauIP, "");
        entradaIP.onSubmit.AddListener(v => UnirSe());
        Button bu = UIKit.Boto(u.transform, "Unir-se", UIKit.IconaXarxa, UIKit.Blau, UIKit.BlauFosc, UnirSe, new Vector2(360f, 92f), 36f);
        UIKit.Posar((RectTransform)bu.transform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(360f, 92f));

        txtEstatOnline = UIKit.Text("Estat", p, "", 26f, UIKit.Groc, TextAlignmentOptions.Center, FontStyles.Normal);
        UIKit.Posar(txtEstatOnline.rectTransform, new Vector2(0.5f, 0f), new Vector2(130f, 64f), new Vector2(860f, 40f));
        return f;
    }

    GameObject ConstruirFinestraEspera(Transform arrel)
    {
        RectTransform f = UIKit.Omplir(UIKit.Nou("Finestra_Espera", arrel));
        UIKit.Enfosquir(f, 0.55f);
        Image panell = UIKit.Panell("Panell", f, UIKit.Fosc, 36f);
        UIKit.Posar(panell.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 560f));
        panell.raycastTarget = true;
        panell.gameObject.AddComponent<PanellAnimat>();
        Transform p = panell.transform;

        Image ic = UIKit.Imatge("Icona", p, UIKit.IconaXarxa, UIKit.Groc);
        UIKit.Posar(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(76f, 76f));
        ic.gameObject.AddComponent<Parpelleig>().velocitat = 4f;
        TextMeshProUGUI t = UIKit.Text("Titol", p, "Esperant el rival...", 52f, Color.white);
        UIKit.Posar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(820f, 70f));
        txtEspera = UIKit.Text("Text", p, "", 30f, UIKit.TextSuau, TextAlignmentOptions.Center, FontStyles.Normal);
        txtEspera.textWrappingMode = TextWrappingModes.Normal;
        UIKit.Posar(txtEspera.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(820f, 180f));

        Button bc = UIKit.Boto(p, "Cancel·lar", UIKit.IconaEnrere, UIKit.Gris, UIKit.GrisFosc, TancarFinestres, new Vector2(320f, 88f), 34f);
        UIKit.Posar((RectTransform)bc.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 88f));
        return f.gameObject;
    }

    string NomPropi()
    {
        string n = ConfigPartida.NomJugador1;
        if (!string.IsNullOrEmpty(n)) return n;
        return ConfigPartida.Mode == ConfigPartida.ModeJoc.OnlineClient ? "PLAYER_2" : "PLAYER_1";
    }

    // Host: ja s'ha triat el nivell, s'obre el port i s'espera que algu s'hi connecti
    void CrearPartidaOnline()
    {
        Obrir(finestraEspera);
        XarxaBigBOOM x = XarxaBigBOOM.Crear();
        x.OnConnectat += AlConnectarXarxa;
        x.OnMissatge += AlMissatgeXarxa;
        x.OnDesconnectat += AlDesconnectarXarxa;
        try
        {
            x.Allotjar();
            txtEspera.text = "Digues a l'altre jugador que s'uneixi amb aquesta IP:\n<color=" + UIKit.Hex(UIKit.Groc) + "><size=40><b>"
                + string.Join("   ", XarxaBigBOOM.IPsLocals()) + "</b></size></color>\n\nNivell: " + ConfigPartida.NivellActual.nom
                + "   ·   Port " + XarxaBigBOOM.Port;
        }
        catch (System.Exception ex)
        {
            XarxaBigBOOM.Destruir();
            txtEspera.text = "No s'ha pogut crear la partida:\n" + ex.Message;
        }
    }

    void UnirSe()
    {
        string ip = entradaIP.text.Trim();
        if (ip.Length == 0) { txtEstatOnline.text = "Escriu la IP de l'ordinador que ha creat la partida"; return; }
        PlayerPrefs.SetString(ClauIP, ip);
        ConfigPartida.Mode = ConfigPartida.ModeJoc.OnlineClient;
        XarxaBigBOOM x = XarxaBigBOOM.Crear();
        x.OnConnectat += AlConnectarXarxa;
        x.OnMissatge += AlMissatgeXarxa;
        x.OnDesconnectat += AlDesconnectarXarxa;
        txtEstatOnline.text = "Connectant amb " + ip + "...";
        x.Connectar(ip);
    }

    void AlConnectarXarxa()
    {
        // El client es presenta; el host espera la presentacio
        if (!XarxaBigBOOM.Instancia.EsHost) XarxaBigBOOM.Instancia.Enviar("HOLA", NomPropi(), XarxaBigBOOM.Versio);
        else if (txtEspera != null) txtEspera.text = "Rival connectat! Preparant la partida...";
    }

    void AlMissatgeXarxa(string[] m)
    {
        XarxaBigBOOM x = XarxaBigBOOM.Instancia;
        if (x.EsHost && m[0] == "HOLA" && m.Length >= 3)
        {
            if (m[2] != XarxaBigBOOM.Versio)
            {
                txtEspera.text = "L'altre jugador té una versió diferent del joc";
                XarxaBigBOOM.Destruir();
                return;
            }
            ConfigPartida.NomsPartida = new string[] { NomPropi(), m[1] };
            x.Enviar("CONFIG", NomPropi(), ConfigPartida.IndexNivell);
            CarregarJoc();
        }
        else if (!x.EsHost && m[0] == "CONFIG" && m.Length >= 3)
        {
            ConfigPartida.TriarNivell(XarxaBigBOOM.I(m[2]));
            ConfigPartida.NomsPartida = new string[] { m[1], NomPropi() };
            CarregarJoc();
        }
    }

    void AlDesconnectarXarxa(string motiu)
    {
        XarxaBigBOOM.Destruir();
        if (txtEstatOnline != null) txtEstatOnline.text = motiu;
        if (txtEspera != null && finestraEspera != null && finestraEspera.activeSelf) txtEspera.text = motiu;
    }

    void CancelarXarxa()
    {
        if (carregant || XarxaBigBOOM.Instancia == null) return;
        XarxaBigBOOM.Destruir();
        if (txtEstatOnline != null) txtEstatOnline.text = "";
    }

    void OnDestroy()
    {
        // La connexio passa a l'escena de joc: el menu deixa d'escoltar-la
        if (XarxaBigBOOM.Instancia == null) return;
        XarxaBigBOOM.Instancia.OnConnectat -= AlConnectarXarxa;
        XarxaBigBOOM.Instancia.OnMissatge -= AlMissatgeXarxa;
        XarxaBigBOOM.Instancia.OnDesconnectat -= AlDesconnectarXarxa;
    }

    GameObject ConstruirFinestraNivell(Transform arrel)
    {
        Transform p;
        GameObject f = Finestra(arrel, "Finestra_Nivell", "Selecciona el nivell", "Com més gran el taulell, més mines hi ha amagades", new Vector2(1240f, 720f), out p);

        Color[] colors = { UIKit.Verd, UIKit.Groc, UIKit.Vermell };
        Color[] ombres = { UIKit.VerdFosc, UIKit.GrocFosc, UIKit.VermellFosc };
        int[] graella = { 4, 6, 8 };
        for (int i = 0; i < ConfigPartida.Nivells.Length; i++)
        {
            ConfigPartida.Nivell n = ConfigPartida.Nivells[i];
            int index = i;
            Button b = Targeta(p, n, colors[i], ombres[i], graella[i], i == 1, () => SeleccionarNivell(index));
            UIKit.Posar((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 380f, -10f), new Vector2(340f, 400f));
        }
        return f;
    }

    Button Targeta(Transform pare, ConfigPartida.Nivell n, Color color, Color ombra, int cel, bool destacat, UnityEngine.Events.UnityAction accio)
    {
        Button b = UIKit.Boto(pare, "", null, color, ombra, accio, new Vector2(340f, 400f), 10f, 30f);
        BotoAnimat an = b.GetComponent<BotoAnimat>();
        an.escalaHover = 1.06f;
        an.enfonsar = 8f;
        Transform cara = an.cara;

        // Vora fosca al voltant de la targeta destacada (Intermedi)
        if (destacat)
        {
            Image vora = UIKit.Imatge("Vora_Destacada", b.transform, UIKit.Arrodonit, new Color32(16, 20, 30, 255), 38f);
            UIKit.Omplir(vora.rectTransform);
            vora.rectTransform.offsetMin = new Vector2(-9f, -17f);
            vora.rectTransform.offsetMax = new Vector2(9f, 9f);
            vora.transform.SetAsFirstSibling();
        }

        Image cercle = UIKit.Imatge("Cercle", cara, UIKit.Cercle, new Color(0f, 0f, 0f, 0.14f));
        UIKit.Posar(cercle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(176f, 176f));
        Image g = UIKit.Imatge("Graella", cercle.transform, UIKit.Graella(cel), new Color(1f, 1f, 1f, 0.92f));
        UIKit.Posar(g.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));
        Image band = UIKit.Imatge("Bandera", cercle.transform, UIKit.IconaBandera, Color.white);
        UIKit.Posar(band.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(58f, 56f), new Vector2(54f, 54f));
        Shadow sh = band.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.3f);

        TextMeshProUGUI nom = UIKit.Text("Nom", cara, n.nom, 44f, Color.white);
        UIKit.Posar(nom.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -224f), new Vector2(320f, 56f));
        TextMeshProUGUI mida = UIKit.Text("Mida", cara, n.columnes + " × " + n.files, 34f, new Color(1f, 1f, 1f, 0.9f), TextAlignmentOptions.Center, FontStyles.Normal);
        UIKit.Posar(mida.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(320f, 44f));

        Image xip = UIKit.Imatge("Xip_Mines", cara, UIKit.Arrodonit, new Color(0f, 0f, 0f, 0.2f), 22f);
        UIKit.Posar(xip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(210f, 52f));
        Image im = UIKit.Imatge("Icona", xip.transform, UIKit.IconaMina, Color.white);
        UIKit.Posar(im.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(32f, 32f));
        TextMeshProUGUI tm = UIKit.Text("Text", xip.transform, n.mines + " mines", 28f, Color.white, TextAlignmentOptions.Left);
        UIKit.Posar(tm.rectTransform, new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(140f, 40f));
        return b;
    }

    GameObject ConstruirFinestraOpcions(Transform arrel)
    {
        Transform p;
        GameObject f = Finestra(arrel, "Finestra_Opcions", "Opcions", null, new Vector2(900f, 740f), out p);

        Slider vol = UIKit.CrearSlider(p, "Volum", 0f, 1f, ConfigPartida.Volum, v => Mathf.RoundToInt(v * 100f) + "%",
            v => ConfigPartida.Volum = v, new Vector2(680f, 90f), UIKit.Verd);
        UIKit.Posar((RectTransform)vol.transform.parent, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(680f, 90f));

        Slider sens = UIKit.CrearSlider(p, "Sensibilitat del ratolí", 0.2f, 3f, ConfigPartida.Sensibilitat, v => v.ToString("0.0") + "x",
            v => ConfigPartida.Sensibilitat = v, new Vector2(680f, 90f), UIKit.Blau);
        UIKit.Posar((RectTransform)sens.transform.parent, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(680f, 90f));

        Button pc = null;
        pc = UIKit.Boto(p, TextPantalla(), null, UIKit.Gris, UIKit.GrisFosc, () =>
        {
            Screen.fullScreen = !Screen.fullScreen;
            pc.GetComponentInChildren<TextMeshProUGUI>().text = TextPantalla(!Screen.fullScreen);
        }, new Vector2(680f, 84f), 32f);
        UIKit.Posar((RectTransform)pc.transform, new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(680f, 84f));

        Button bn = UIKit.Boto(p, TextBotoNom(), UIKit.IconaPersona, UIKit.Blau, UIKit.BlauFosc, ObrirFinestraNom, new Vector2(680f, 84f), 32f);
        UIKit.Posar((RectTransform)bn.transform, new Vector2(0.5f, 1f), new Vector2(0f, -396f), new Vector2(680f, 84f));
        txtBotoNom = bn.GetComponentInChildren<TextMeshProUGUI>();
        return f;
    }

    string TextBotoNom()
    {
        string nom = ConfigPartida.NomJugador1;
        return "Nom del jugador: " + (string.IsNullOrEmpty(nom) ? "PLAYER_1" : nom);
    }

    // Finestra que demana el nom del jugador 1 (en obrir el joc i des d'Opcions)
    GameObject ConstruirFinestraNom(Transform arrel)
    {
        RectTransform f = UIKit.Omplir(UIKit.Nou("Finestra_Nom", arrel));
        UIKit.Enfosquir(f, 0.55f);
        Image panell = UIKit.Panell("Panell", f, UIKit.Fosc, 36f);
        UIKit.Posar(panell.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 500f));
        panell.raycastTarget = true;
        panell.gameObject.AddComponent<PanellAnimat>();
        Transform p = panell.transform;

        Image ic = UIKit.Imatge("Icona", p, UIKit.IconaPersona, UIKit.Groc);
        UIKit.Posar(ic.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(70f, 70f));
        TextMeshProUGUI t = UIKit.Text("Titol", p, "Com et dius?", 56f, Color.white);
        UIKit.Posar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(780f, 70f));
        TextMeshProUGUI s = UIKit.Text("Subtitol", p, "Aquest serà el nom del Jugador 1", 28f, UIKit.TextSuau, TextAlignmentOptions.Center, FontStyles.Normal);
        UIKit.Posar(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(780f, 40f));

        entradaNom = UIKit.CampText(p, "PLAYER_1", new Vector2(620f, 90f), 14);
        UIKit.Posar((RectTransform)entradaNom.transform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(620f, 90f));
        entradaNom.onSubmit.AddListener(v => ConfirmarNom());

        Button ok = UIKit.Boto(p, "Endavant", UIKit.IconaPlay, UIKit.Verd, UIKit.VerdFosc, ConfirmarNom, new Vector2(360f, 96f), 38f, 24f);
        UIKit.Posar((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(360f, 96f));
        return f.gameObject;
    }

    void ObrirFinestraNom()
    {
        if (finestraNom == null) return;
        TancarFinestres();
        finestraNom.SetActive(true);
        entradaNom.text = ConfigPartida.NomJugador1;
        entradaNom.Select();
        entradaNom.ActivateInputField();
    }

    void ConfirmarNom()
    {
        if (finestraNom == null || !finestraNom.activeSelf) return;
        string nom = entradaNom.text.Trim();
        ConfigPartida.NomJugador1 = nom;
        ConfigPartida.NomDemanat = true;
        if (txtBotoNom != null) txtBotoNom.text = TextBotoNom();
        finestraNom.SetActive(false);
    }

    // Screen.fullScreen no canvia fins al frame seguent, per aixo es pot passar el valor nou
    string TextPantalla() { return TextPantalla(Screen.fullScreen); }
    string TextPantalla(bool completa) { return "Pantalla completa: " + (completa ? "Sí" : "No"); }

    GameObject ConstruirFinestraCredits(Transform arrel)
    {
        Transform p;
        GameObject f = Finestra(arrel, "Finestra_Credits", "Crèdits", "Projecte de 2n de DAM", new Vector2(900f, 660f), out p);
        string text =
            "<b>BigBOOM</b>  ·  Buscamines 3D PvP per torns\n" +
            "Programació, disseny i mapes: equip BigBOOM\n\n" +
            "<color=" + UIKit.Hex(UIKit.TextSuau) + "><size=24>Recursos: Mixamo (animacions), Floreswa (personatges),\n" +
            "Bublik (castell), Darth_Artisan i Nebula (arbres i natura),\n" +
            "Stylized Nature MegaKit, Fantasy Skybox FREE,\n" +
            "Yughues (materials) i ithappy (animals)</size></color>";
        TextMeshProUGUI t = UIKit.Text("Text", p, text, 30f, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.lineSpacing = 12f;
        UIKit.Posar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(800f, 340f));
        return f;
    }

    void TancarFinestres()
    {
        if (finestraNivell != null) finestraNivell.SetActive(false);
        if (finestraOpcions != null) finestraOpcions.SetActive(false);
        if (finestraCredits != null) finestraCredits.SetActive(false);
        if (finestraNom != null) finestraNom.SetActive(false);
        if (finestraMode != null) finestraMode.SetActive(false);
        if (finestraOnline != null) finestraOnline.SetActive(false);
        if (finestraEspera != null) finestraEspera.SetActive(false);
        CancelarXarxa();   // si s'estava esperant o connectant, es deixa estar
    }

    void Obrir(GameObject f)
    {
        TancarFinestres();
        if (f != null) f.SetActive(true);
    }

    void ObrirCredits() { Obrir(finestraCredits); }

    void SeleccionarNivell(int i)
    {
        ConfigPartida.TriarNivell(i);
        switch (ConfigPartida.Mode)
        {
            case ConfigPartida.ModeJoc.OnlineHost:
                CrearPartidaOnline();
                return;
            case ConfigPartida.ModeJoc.Bot:
                ConfigPartida.NomsPartida = new string[] { NomPropi(), "BOT" };
                break;
            default:
                ConfigPartida.Mode = ConfigPartida.ModeJoc.Local;
                ConfigPartida.NomsPartida = null;
                break;
        }
        CarregarJoc();
    }

    void CarregarJoc()
    {
        if (carregant) return;
        carregant = true;
        if (fos != null) StartCoroutine(FosICarrega());
        else SceneManager.LoadScene(nombreEscenaJuego);
    }

    IEnumerator FosICarrega()
    {
        fos.raycastTarget = true;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            fos.color = new Color(0f, 0f, 0f, t / 0.35f);
            yield return null;
        }
        fos.color = Color.black;
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // --- NAVEGACIÓ DE PANELLS (es mantenen per als botons del menu antic) ---

    public void MostrarPanelPrincipal()
    {
        if (usarMenuNou) { TancarFinestres(); return; }
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelPersonajes != null) panelPersonajes.SetActive(false);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
    }

    public void AbrirSeleccionPersonaje()
    {
        if (usarMenuNou) { Obrir(finestraMode); return; }
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelPersonajes != null) panelPersonajes.SetActive(true);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
    }

    public void AbrirConfiguracion()
    {
        if (usarMenuNou) { Obrir(finestraOpcions); return; }
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelPersonajes != null) panelPersonajes.SetActive(false);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(true);
    }

    // --- ACCIONS DE JOC I CONFIGURACIÓ ---

    public void SetVolume(float volume)
    {
        ConfigPartida.Volum = volume;
    }

    public void SeleccionarPersonajeYJugar(int idPersonaje)
    {
        personajeSeleccionado = idPersonaje;
        CarregarJoc();
    }

    public void Jugar()
    {
        if (usarMenuNou) Obrir(finestraMode);
        else SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
