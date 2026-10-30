using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// HUD en pantalla del jugador que juga (POV): marcador, temps del torn, mira sobre les caselles,
// avisos del torn i pantalla final. Els controls es mostren al menu de pausa (ESC).
// Llegeix l'estat del JocBuscamines; els clics a les caselles els fa el mateix JocBuscamines.
public class HudJugador : MonoBehaviour
{
    public JocBuscamines joc;

    Canvas canvas;
    // Marcador
    TextMeshProUGUI[] txtNom = new TextMeshProUGUI[2], txtPunts = new TextMeshProUGUI[2];
    Image[] resaltat = new Image[2];
    // Temps i torn
    Image anellTemps;
    TextMeshProUGUI txtTemps, txtTorn, txtPuntsTorn, txtMines, txtBanderes;
    // Mira
    RectTransform mira;
    Image[] barresMira;
    TextMeshProUGUI txtCasella;
    float copMira;
    Transform selector;
    Renderer[] barresSelector;
    GameObject panellEspectador;
    TextMeshProUGUI txtEspectador;
    // Avisos
    CanvasGroup grupAvis;
    TextMeshProUGUI txtAvis, txtAvisSub;
    float tempsAvis;
    Image vinyeta;
    float flaixVinyeta;
    // Final
    GameObject pantallaFinal;
    TextMeshProUGUI txtGuanyador, txtResultat;

    JocBuscamines.Estat estatAnterior = JocBuscamines.Estat.FiPartida;
    int compteAnterior = -1;

    void Start()
    {
        if (joc == null) joc = FindFirstObjectByType<JocBuscamines>();
        if (joc == null) { enabled = false; return; }
        joc.OnExplosio += AlExplotar;
        joc.OnTornAcabat += AlAcabarTorn;
        Construir();
    }

    void OnDestroy()
    {
        if (joc == null) return;
        joc.OnExplosio -= AlExplotar;
        joc.OnTornAcabat -= AlAcabarTorn;
    }

    // ---------------------------------------------------------------
    void Construir()
    {
        canvas = UIKit.CrearCanvas("HUD_Jugador", 20);
        canvas.transform.SetParent(transform, false);
        Transform a = canvas.transform;

        vinyeta = UIKit.Imatge("Vinyeta", a, UIKit.Vinyeta, new Color(0.9f, 0.1f, 0.05f, 0f));
        UIKit.Omplir(vinyeta.rectTransform);

        // Marcador dels dos jugadors
        for (int j = 0; j < 2; j++)
        {
            Vector2 anc = j == 0 ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            Vector2 pos = j == 0 ? new Vector2(32f, -32f) : new Vector2(-32f, -32f);
            Color col = UIKit.ColorJugador(j);

            resaltat[j] = UIKit.Imatge("Resaltat_J" + (j + 1), a, UIKit.Arrodonit, col, 30f);
            UIKit.Posar(resaltat[j].rectTransform, anc, pos + new Vector2(j == 0 ? -6f : 6f, 6f), new Vector2(392f, 132f));
            resaltat[j].gameObject.AddComponent<Parpelleig>().velocitat = 4f;

            Image tg = UIKit.Panell("Jugador_" + (j + 1), a, UIKit.Fosc, 26f);
            UIKit.Posar(tg.rectTransform, anc, pos, new Vector2(380f, 120f));
            Image av = UIKit.Imatge("Avatar", tg.transform, UIKit.Cercle, col);
            UIKit.Posar(av.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(84f, 84f));
            TextMeshProUGUI ini = UIKit.Text("Inicial", av.transform, "J" + (j + 1), 34f, Color.white);
            UIKit.Omplir(ini.rectTransform);
            txtNom[j] = UIKit.Text("Nom", tg.transform, joc.Nom(j), 28f, UIKit.TextSuau, TextAlignmentOptions.Left);
            UIKit.Posar(txtNom[j].rectTransform, new Vector2(0f, 1f), new Vector2(120f, -16f), new Vector2(240f, 38f));
            txtPunts[j] = UIKit.Text("Punts", tg.transform, "0 PTS", 44f, Color.white, TextAlignmentOptions.Left);
            UIKit.Posar(txtPunts[j].rectTransform, new Vector2(0f, 0f), new Vector2(120f, 12f), new Vector2(240f, 56f));
        }

        // Temps (anell) i informacio del torn
        Image fonsAnell = UIKit.Imatge("Fons_Temps", a, UIKit.Cercle, UIKit.Fosc);
        UIKit.Posar(fonsAnell.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(136f, 136f));
        Image anellBuit = UIKit.Imatge("Anell_Buit", fonsAnell.transform, UIKit.Anell, new Color(1f, 1f, 1f, 0.12f));
        UIKit.Omplir(anellBuit.rectTransform, 8f);
        anellTemps = UIKit.Imatge("Anell_Temps", fonsAnell.transform, UIKit.Anell, Color.white);
        UIKit.Omplir(anellTemps.rectTransform, 8f);
        anellTemps.type = Image.Type.Filled;
        anellTemps.fillMethod = Image.FillMethod.Radial360;
        anellTemps.fillOrigin = (int)Image.Origin360.Top;
        anellTemps.fillClockwise = false;
        txtTemps = UIKit.Text("Temps", fonsAnell.transform, "15", 56f, Color.white);
        UIKit.Omplir(txtTemps.rectTransform);

        Image pindola = UIKit.Panell("Torn", a, UIKit.Fosc, 24f);
        UIKit.Posar(pindola.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(480f, 56f));
        txtTorn = UIKit.Text("Text", pindola.transform, "", 28f, Color.white);
        UIKit.Omplir(txtTorn.rectTransform);
        txtPuntsTorn = UIKit.Text("Punts_Torn", a, "+0 PTS", 34f, Color.white);
        UIKit.Posar(txtPuntsTorn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(300f, 44f));
        UIKit.Contorn(txtPuntsTorn, 0.2f);

        // Mines i banderes
        Image info = UIKit.Panell("Mines", a, UIKit.Fosc, 24f);
        UIKit.Posar(info.rectTransform, new Vector2(1f, 0f), new Vector2(-32f, 32f), new Vector2(330f, 72f));
        Image im = UIKit.Imatge("Icona_Mina", info.transform, UIKit.IconaMina, Color.white);
        UIKit.Posar(im.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(40f, 40f));
        txtMines = UIKit.Text("Mines", info.transform, "40", 32f, Color.white, TextAlignmentOptions.Left);
        UIKit.Posar(txtMines.rectTransform, new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(90f, 44f));
        Image ib = UIKit.Imatge("Icona_Bandera", info.transform, UIKit.IconaBandera, UIKit.Vermell);
        UIKit.Posar(ib.rectTransform, new Vector2(0f, 0.5f), new Vector2(172f, 0f), new Vector2(40f, 40f));
        txtBanderes = UIKit.Text("Banderes", info.transform, "0", 32f, Color.white, TextAlignmentOptions.Left);
        UIKit.Posar(txtBanderes.rectTransform, new Vector2(0f, 0.5f), new Vector2(222f, 0f), new Vector2(90f, 44f));

        // Mira
        mira = UIKit.Posar(UIKit.Nou("Mira", a), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
        barresMira = new Image[5];
        Vector2[] posB = { new Vector2(0f, 17f), new Vector2(0f, -17f), new Vector2(17f, 0f), new Vector2(-17f, 0f), Vector2.zero };
        Vector2[] midB = { new Vector2(4f, 14f), new Vector2(4f, 14f), new Vector2(14f, 4f), new Vector2(14f, 4f), new Vector2(7f, 7f) };
        for (int i = 0; i < barresMira.Length; i++)
        {
            barresMira[i] = UIKit.Imatge("Barra", mira, i == 4 ? UIKit.Cercle : UIKit.Arrodonit, Color.white, i == 4 ? 0f : 2f);
            UIKit.Posar(barresMira[i].rectTransform, new Vector2(0.5f, 0.5f), posB[i], midB[i]);
            Shadow s = barresMira[i].gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.5f);
            s.effectDistance = new Vector2(1f, -1f);
        }
        txtCasella = UIKit.Text("Casella", a, "", 22f, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
        UIKit.Posar(txtCasella.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(400f, 30f));
        UIKit.Contorn(txtCasella, 0.25f);

        // Els controls es mostren al menu de pausa (ESC), no a la pantalla de joc

        // Avisos al centre
        RectTransform ra = UIKit.Posar(UIKit.Nou("Avis", a), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 220f));
        grupAvis = ra.gameObject.AddComponent<CanvasGroup>();
        grupAvis.alpha = 0f;
        grupAvis.blocksRaycasts = false;
        txtAvis = UIKit.Text("Titol", ra, "", 110f, Color.white);
        UIKit.Posar(txtAvis.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400f, 140f));
        UIKit.Contorn(txtAvis, 0.18f);
        txtAvisSub = UIKit.Text("Subtitol", ra, "", 38f, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
        UIKit.Posar(txtAvisSub.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1400f, 60f));
        UIKit.Contorn(txtAvisSub, 0.22f);

        // Avis per a qui mira el torn de l'altre (online o bot) i per quan s'espera el rival
        Image pe = UIKit.Panell("Espectador", a, UIKit.Fosc, 24f);
        UIKit.Posar(pe.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(640f, 64f));
        txtEspectador = UIKit.Text("Text", pe.transform, "", 28f, Color.white);
        UIKit.Omplir(txtEspectador.rectTransform);
        panellEspectador = pe.gameObject;
        panellEspectador.SetActive(false);

        ConstruirPantallaFinal(a);
    }

    void ConstruirPantallaFinal(Transform a)
    {
        RectTransform f = UIKit.Omplir(UIKit.Nou("Pantalla_Final", a));
        pantallaFinal = f.gameObject;
        UIKit.Enfosquir(f, 0.55f);
        Image p = UIKit.Panell("Panell", f, UIKit.Fosc, 36f);
        UIKit.Posar(p.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 560f));
        p.raycastTarget = true;
        p.gameObject.AddComponent<PanellAnimat>();

        TextMeshProUGUI t = UIKit.Text("Titol", p.transform, "FI DE LA PARTIDA", 40f, UIKit.TextSuau);
        UIKit.Posar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 50f));
        txtGuanyador = UIKit.Text("Guanyador", p.transform, "", 84f, Color.white);
        UIKit.Posar(txtGuanyador.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(800f, 110f));
        txtResultat = UIKit.Text("Resultat", p.transform, "", 46f, Color.white);
        UIKit.Posar(txtResultat.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(800f, 70f));

        // En online no es pot tornar a jugar directament (cal tornar a connectar des del menu)
        bool online = ConfigPartida.EsOnline;
        if (!online)
        {
            Button b1 = UIKit.Boto(p.transform, "Tornar a jugar", UIKit.IconaReiniciar, UIKit.Verd, UIKit.VerdFosc, TornarAJugar, new Vector2(360f, 96f), 32f);
            UIKit.Posar((RectTransform)b1.transform, new Vector2(0.5f, 0f), new Vector2(-195f, 56f), new Vector2(360f, 96f));
        }
        Button b2 = UIKit.Boto(p.transform, "Menú principal", UIKit.IconaCasa, UIKit.Gris, UIKit.GrisFosc, AnarAlMenu, new Vector2(360f, 96f), 32f);
        UIKit.Posar((RectTransform)b2.transform, new Vector2(0.5f, 0f), new Vector2(online ? 0f : 195f, 56f), new Vector2(360f, 96f));
        pantallaFinal.SetActive(false);
    }

    // ---------------------------------------------------------------
    void Update()
    {
        if (canvas == null) return;
        float dt = Time.unscaledDeltaTime;
        JocBuscamines.Estat e = joc.EstatActual;
        int jug = joc.JugadorActual;
        Color colJ = UIKit.ColorJugador(jug);

        // Esperant el rival (online) o mirant el torn de l'altre des de l'illa d'espera
        string espectador = null;
        if (joc.EsperantRival) espectador = "Esperant que el rival carregui la partida...";
        else if (e == JocBuscamines.Estat.Jugant && !ConfigPartida.JugadorLocal(jug))
            espectador = "Torn de <color=" + UIKit.Hex(Color.Lerp(colJ, Color.white, 0.35f)) + ">" + joc.Nom(jug) + "</color>  ·  ets a la zona d'espera";
        panellEspectador.SetActive(espectador != null);
        if (espectador != null) txtEspectador.text = espectador;

        // Marcador
        for (int j = 0; j < 2; j++)
        {
            txtNom[j].text = joc.Nom(j);
            txtPunts[j].text = joc.Punts(j) + " PTS";
            resaltat[j].gameObject.SetActive(j == jug && e != JocBuscamines.Estat.FiPartida);
        }

        // Temps
        bool jugant = e == JocBuscamines.Estat.Jugant;
        float temps = joc.TempsRestant;
        float durada = Mathf.Max(0.01f, joc.DuradaFase);
        anellTemps.fillAmount = jugant ? Mathf.Clamp01(temps / durada) : (e == JocBuscamines.Estat.Preparacio ? 1f : 0f);
        bool poc = jugant && temps <= 5f;
        anellTemps.color = poc ? Color.Lerp(UIKit.Vermell, Color.white, (Mathf.Sin(Time.unscaledTime * 10f) + 1f) * 0.25f) : colJ;
        txtTemps.text = jugant ? Mathf.CeilToInt(Mathf.Max(0f, temps)).ToString() : (e == JocBuscamines.Estat.Preparacio ? "-" : "0");
        txtTemps.color = poc ? UIKit.Vermell : Color.white;
        txtTorn.text = "<color=" + UIKit.Hex(UIKit.TextSuau) + ">TORN</color> " + joc.Torn + "/" + joc.tornsTotals
            + "   <color=" + UIKit.Hex(UIKit.TextSuau) + ">·</color>   <color=" + UIKit.Hex(Color.Lerp(colJ, Color.white, 0.35f)) + ">" + joc.Nom(jug) + "</color>";
        int pt = joc.PuntsTorn;
        txtPuntsTorn.text = (pt >= 0 ? "+" : "") + pt + " PTS";
        txtPuntsTorn.color = pt > 0 ? UIKit.Verd : (pt < 0 ? UIKit.Vermell : Color.white);
        txtMines.text = joc.Mines.ToString();
        txtBanderes.text = joc.Banderes.ToString();

        // Canvis d'estat: avisos
        if (e != estatAnterior)
        {
            if (e == JocBuscamines.Estat.Jugant) Avisar("JA!", "Torn de " + joc.Nom(jug), colJ, 0.9f);
            if (e == JocBuscamines.Estat.FiPartida) MostrarFinal();
            estatAnterior = e;
            compteAnterior = -1;
        }
        if (e == JocBuscamines.Estat.Preparacio)
        {
            int compte = Mathf.CeilToInt(temps);
            if (compte != compteAnterior && compte > 0)
            {
                compteAnterior = compte;
                Avisar(compte.ToString(), "Torn " + joc.Torn + "  ·  Ara juga " + joc.Nom(jug), colJ, 1.1f);
            }
        }

        // Mira (s'obre una mica en clicar)
        bool potPicar = joc.PotPicar;
        mira.gameObject.SetActive(potPicar && joc.usarMira);
        if (!potPicar && selector != null) selector.gameObject.SetActive(false);
        if (potPicar)
        {
            Mouse m = Mouse.current;
            Keyboard kb = Keyboard.current;
            if ((m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)) || (kb != null && kb.fKey.wasPressedThisFrame))
                copMira = 1f;
            ActualitzarMira(dt);
        }
        else txtCasella.text = "";

        // Avis i vinyeta
        if (tempsAvis > 0f)
        {
            tempsAvis -= dt;
            grupAvis.alpha = Mathf.Clamp01(tempsAvis / 0.25f);
            float s = 1f + Mathf.Max(0f, tempsAvis - 0.6f) * 0.25f;
            grupAvis.transform.localScale = Vector3.one * Mathf.Min(s, 1.2f);
        }
        else grupAvis.alpha = 0f;
        flaixVinyeta = Mathf.MoveTowards(flaixVinyeta, 0f, dt * 1.2f);
        Color cv = vinyeta.color;
        cv.a = flaixVinyeta * 0.85f;
        vinyeta.color = cv;
    }

    void ActualitzarMira(float dt)
    {
        Vector2Int p;
        Vector3 punt;
        Color c = Color.white;
        float obertura = 17f;
        txtCasella.text = "";
        bool marcar = false;
        if (joc.CasellaApuntada(out p, out punt))
        {
            bool aprop = joc.DinsAbast(punt);
            bool oberta = joc.EsOberta(p);
            if (!aprop) { c = new Color(1f, 1f, 1f, 0.35f); txtCasella.text = "Massa lluny"; }
            else if (oberta) { c = new Color(1f, 1f, 1f, 0.5f); }
            else if (joc.TeBandera(p)) { c = UIKit.Vermell; obertura = 12f; marcar = true; txtCasella.text = "Bandera  ·  fila " + (p.y + 1) + ", col " + (p.x + 1); }
            else { c = UIKit.Verd; obertura = 12f; marcar = true; txtCasella.text = "Fila " + (p.y + 1) + ", col " + (p.x + 1); }
        }
        else c = new Color(1f, 1f, 1f, 0.6f);
        ActualitzarSelector(marcar, p, c, dt);

        copMira = Mathf.MoveTowards(copMira, 0f, dt * 5f);
        obertura += copMira * 8f;
        Vector2[] pos = { new Vector2(0f, obertura), new Vector2(0f, -obertura), new Vector2(obertura, 0f), new Vector2(-obertura, 0f) };
        for (int i = 0; i < barresMira.Length; i++)
        {
            barresMira[i].color = Color.Lerp(barresMira[i].color, c, 1f - Mathf.Exp(-20f * dt));
            if (i < 4) barresMira[i].rectTransform.anchoredPosition = Vector2.Lerp(barresMira[i].rectTransform.anchoredPosition, pos[i], 1f - Mathf.Exp(-20f * dt));
        }
    }

    // Marc al mon damunt de la casella que s'esta apuntant (la que es destapara o on anira la bandera)
    void ActualitzarSelector(bool visible, Vector2Int p, Color color, float dt)
    {
        Bounds b = default(Bounds);
        if (visible && !joc.LimitsCasella(p, out b)) visible = false;
        if (selector == null)
        {
            if (!visible) return;
            selector = new GameObject("Selector_Casella").transform;
            barresSelector = new Renderer[4];
            for (int i = 0; i < 4; i++)
                barresSelector[i] = EfectesJoc.Primitiva(PrimitiveType.Cube, selector, Color.white, Vector3.zero, Vector3.one).GetComponent<Renderer>();
        }
        selector.gameObject.SetActive(visible);
        if (!visible) return;

        float m = Mathf.Max(b.size.x, b.size.z);
        float g = m * 0.07f;                                  // gruix del marc
        float puls = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.025f;
        selector.position = new Vector3(b.center.x, b.max.y + 0.03f, b.center.z);
        selector.localScale = Vector3.one * puls;
        Vector3[] pos = { new Vector3(0f, 0f, m * 0.5f), new Vector3(0f, 0f, -m * 0.5f), new Vector3(m * 0.5f, 0f, 0f), new Vector3(-m * 0.5f, 0f, 0f) };
        Vector3[] esc = { new Vector3(m + g, g * 0.5f, g), new Vector3(m + g, g * 0.5f, g), new Vector3(g, g * 0.5f, m + g), new Vector3(g, g * 0.5f, m + g) };
        for (int i = 0; i < 4; i++)
        {
            barresSelector[i].transform.localPosition = pos[i];
            barresSelector[i].transform.localScale = esc[i];
            barresSelector[i].sharedMaterial.color = Color.Lerp(barresSelector[i].sharedMaterial.color, color, 1f - Mathf.Exp(-20f * dt));
        }
    }

    // ---------------------------------------------------------------
    public void Avisar(string titol, string sub, Color color, float durada)
    {
        txtAvis.text = titol;
        txtAvis.color = color;
        txtAvisSub.text = sub;
        tempsAvis = durada;
    }

    void AlExplotar(Vector3 pos)
    {
        flaixVinyeta = 1f;
        Avisar("BOOM!", joc.puntsMina + " punts per " + joc.Nom(joc.JugadorActual), UIKit.Groc, 1.6f);
    }

    void AlAcabarTorn(int puntsTorn, int bones, int dolentes, bool explotat)
    {
        string sub = "Banderes bones: " + bones + "   ·   Errònies: " + dolentes;
        Avisar((puntsTorn >= 0 ? "+" : "") + puntsTorn + " punts", sub, puntsTorn >= 0 ? UIKit.Verd : UIKit.Vermell, joc.tempsFiTorn * 0.9f);
    }

    void MostrarFinal()
    {
        int p1 = joc.Punts(0), p2 = joc.Punts(1);
        if (p1 == p2)
        {
            txtGuanyador.text = "EMPAT";
            txtGuanyador.color = UIKit.Groc;
        }
        else
        {
            int g = p1 > p2 ? 0 : 1;
            txtGuanyador.text = "GUANYA " + joc.Nom(g);
            txtGuanyador.color = Color.Lerp(UIKit.ColorJugador(g), Color.white, 0.25f);
        }
        txtResultat.text = "<color=" + UIKit.Hex(Color.Lerp(UIKit.Jugador1, Color.white, 0.3f)) + ">" + p1 + "</color>  -  <color="
            + UIKit.Hex(Color.Lerp(UIKit.Jugador2, Color.white, 0.3f)) + ">" + p2 + "</color>";
        pantallaFinal.SetActive(true);
    }

    void TornarAJugar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void AnarAlMenu()
    {
        Time.timeScale = 1f;
        XarxaBigBOOM.Destruir();
        string escena = PauseMenuController.Instancia != null ? PauseMenuController.Instancia.escenaMenu : "MainMenu";
        SceneManager.LoadScene(escena);
    }
}
