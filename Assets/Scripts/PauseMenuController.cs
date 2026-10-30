using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Menu de pausa: s'obre i es tanca amb ESC (o des del boto "Menu" del HUD).
// Si "usarMenuNou" esta activat amaga el panell antic de l'escena i en construeix un per codi amb l'estil del menu principal.
public class PauseMenuController : MonoBehaviour
{
    [Header("Panell de Pausa (antic)")]
    public GameObject panelPausa;

    [Header("Menu nou")]
    public bool usarMenuNou = true;
    public string escenaMenu = "MainMenu";

    public static bool EstaPausat { get; private set; }
    public static PauseMenuController Instancia { get; private set; }

    GameObject panellNou;
    CursorLockMode cursorAbans;
    bool cursorVisibleAbans;

    void Awake()
    {
        Instancia = this;
        EstaPausat = false;
    }

    void Start()
    {
        if (!usarMenuNou) return;
        if (panelPausa != null) panelPausa.SetActive(false);
        Construir();
        panellNou.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instancia != this) return;
        Instancia = null;
        EstaPausat = false;
    }

    void Update()
    {
        // Si premes la tecla ESC, s'obre o es tanca la pausa
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (EstaPausat) Continuar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        if (EstaPausat) return;
        cursorAbans = Cursor.lockState;
        cursorVisibleAbans = Cursor.visible;
        if (usarMenuNou && panellNou != null) panellNou.SetActive(true);
        else if (panelPausa != null) panelPausa.SetActive(true);
        // Congela el joc (en online no: l'altre ordinador continua i es desincronitzaria)
        if (!ConfigPartida.EsOnline) Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EstaPausat = true;
    }

    public void Continuar()
    {
        if (!EstaPausat) return;
        if (panellNou != null) panellNou.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(false);
        Time.timeScale = 1f; // Reprèn el joc
        Cursor.lockState = cursorAbans;
        Cursor.visible = cursorVisibleAbans;
        EstaPausat = false;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        EstaPausat = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AnarAlMenuPrincipal()
    {
        Time.timeScale = 1f; // Torna a activar el temps abans de canviar d'escena
        EstaPausat = false;
        XarxaBigBOOM.Destruir();   // en online, l'altre jugador veura que t'has desconnectat
        SceneManager.LoadScene(escenaMenu);
    }

    public void Sortir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    // ---------------------------------------------------------------
    void Construir()
    {
        Canvas canvas = UIKit.CrearCanvas("Menu_Pausa", 50);
        canvas.transform.SetParent(transform, false);
        panellNou = canvas.gameObject;
        Transform arrel = canvas.transform;
        UIKit.Enfosquir(arrel, 0.6f);

        Image panell = UIKit.Panell("Panell", arrel, UIKit.Fosc, 36f);
        UIKit.Posar(panell.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160f, 790f));
        panell.raycastTarget = true;
        panell.gameObject.AddComponent<PanellAnimat>();
        Transform p = panell.transform;

        Image ic = UIKit.Imatge("Icona", p, UIKit.IconaPausa, UIKit.Groc);
        UIKit.Posar(ic.rectTransform, new Vector2(0f, 1f), new Vector2(60f, -56f), new Vector2(54f, 54f));
        TextMeshProUGUI titol = UIKit.Text("Titol", p, "PAUSA", 64f, Color.white, TextAlignmentOptions.Left);
        UIKit.Posar(titol.rectTransform, new Vector2(0f, 1f), new Vector2(132f, -46f), new Vector2(400f, 76f));

        // Columna esquerra: botons (en online no es pot reiniciar la partida)
        string[] noms = { "Continuar", "Reiniciar partida", "Menú principal", "Sortir del joc" };
        Sprite[] icones = { UIKit.IconaPlay, UIKit.IconaReiniciar, UIKit.IconaCasa, UIKit.IconaSortir };
        Color[] colors = { UIKit.Verd, UIKit.Blau, UIKit.Gris, UIKit.Gris };
        Color[] ombres = { UIKit.VerdFosc, UIKit.BlauFosc, UIKit.GrisFosc, UIKit.GrisFosc };
        UnityEngine.Events.UnityAction[] accions = { Continuar, Reiniciar, AnarAlMenuPrincipal, Sortir };
        int fila = 0;
        for (int i = 0; i < noms.Length; i++)
        {
            if (i == 1 && ConfigPartida.EsOnline) continue;
            if (i == 2 && ConfigPartida.EsOnline) noms[i] = "Sortir al menú";
            Button b = UIKit.Boto(p, noms[i], icones[i], colors[i], ombres[i], accions[i], new Vector2(500f, 96f), 36f);
            UIKit.Posar((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(60f, -170f - fila * 116f), new Vector2(500f, 96f));
            fila++;
        }

        // Columna dreta: controls (amb icones i tecles dibuixades) i opcions
        Image caixa = UIKit.Panell("Controls", p, new Color(1f, 1f, 1f, 0.05f), 26f, false);
        UIKit.Posar(caixa.rectTransform, new Vector2(1f, 1f), new Vector2(-60f, -150f), new Vector2(500f, 392f));
        TextMeshProUGUI tc = UIKit.Text("Titol", caixa.transform, "Controls", 32f, Color.white, TextAlignmentOptions.Left);
        UIKit.Posar(tc.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -16f), new Vector2(440f, 44f));
        Transform c = caixa.transform;
        FilaControl(c, 0, "Destapar casella", (t, y) => Icona(t, UIKit.IconaRatoli(true), UIKit.Verd, 24f, y));
        FilaControl(c, 1, "Posar / treure bandera", (t, y) =>
        {
            Icona(t, UIKit.IconaRatoli(false), UIKit.Vermell, 24f, y);
            TextMeshProUGUI o = UIKit.Text("o", t, "/", 24f, UIKit.TextSuau);
            UIKit.Posar(o.rectTransform, new Vector2(0f, 1f), new Vector2(70f, y), new Vector2(20f, 42f));
            UIKit.Tecla(t, "F", new Vector2(96f, y));
        });
        FilaControl(c, 2, "Caminar", (t, y) =>
        {
            string[] w = { "W", "A", "S", "D" };
            for (int i = 0; i < w.Length; i++) UIKit.Tecla(t, w[i], new Vector2(24f + i * 48f, y));
        });
        FilaControl(c, 3, "Saltar", (t, y) => UIKit.Tecla(t, "ESPAI", new Vector2(24f, y), 140f));
        FilaControl(c, 4, "Mirar i apuntar", (t, y) => Icona(t, UIKit.IconaRatoliBuit, Color.white, 24f, y));
        FilaControl(c, 5, "Pausa / tornar", (t, y) => UIKit.Tecla(t, "ESC", new Vector2(24f, y), 64f));

        Slider vol = UIKit.CrearSlider(p, "Volum", 0f, 1f, ConfigPartida.Volum, v => Mathf.RoundToInt(v * 100f) + "%",
            v => ConfigPartida.Volum = v, new Vector2(500f, 80f), UIKit.Verd);
        UIKit.Posar((RectTransform)vol.transform.parent, new Vector2(1f, 1f), new Vector2(-60f, -566f), new Vector2(500f, 80f));
        Slider sens = UIKit.CrearSlider(p, "Sensibilitat", 0.2f, 3f, ConfigPartida.Sensibilitat, v => v.ToString("0.0") + "x",
            v => ConfigPartida.Sensibilitat = v, new Vector2(500f, 80f), UIKit.Blau);
        UIKit.Posar((RectTransform)sens.transform.parent, new Vector2(1f, 1f), new Vector2(-60f, -664f), new Vector2(500f, 80f));
    }

    // Una fila de la llista de controls: a l'esquerra el que s'ha de premer, a la dreta que fa
    void FilaControl(Transform caixa, int i, string accio, System.Action<Transform, float> dibuixar)
    {
        float y = -70f - i * 52f;
        dibuixar(caixa, y);
        TextMeshProUGUI a = UIKit.Text("Accio", caixa, accio, 25f, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
        UIKit.Posar(a.rectTransform, new Vector2(0f, 1f), new Vector2(238f, y), new Vector2(250f, 42f));
    }

    static void Icona(Transform pare, Sprite s, Color c, float x, float y)
    {
        Image im = UIKit.Imatge("Icona", pare, s, c);
        UIKit.Posar(im.rectTransform, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(42f, 42f));
    }
}
