using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

// Eines per construir la interficie per codi (menu principal, pausa i HUD) amb un estil comu.
// Tots els sprites (cantonades arrodonides, icones...) es generen en temps d'execucio: no cal importar cap imatge.
public static class UIKit
{
    // ---------- Paleta ----------
    public static readonly Color Fosc = new Color32(17, 26, 43, 230);
    public static readonly Color FoscClar = new Color32(33, 48, 74, 235);
    public static readonly Color TextClar = Color.white;
    public static readonly Color TextSuau = new Color32(176, 194, 222, 255);
    public static readonly Color Verd = new Color32(92, 184, 72, 255);
    public static readonly Color VerdFosc = new Color32(52, 120, 40, 255);
    public static readonly Color Groc = new Color32(255, 176, 32, 255);
    public static readonly Color GrocFosc = new Color32(196, 120, 8, 255);
    public static readonly Color Vermell = new Color32(232, 72, 62, 255);
    public static readonly Color VermellFosc = new Color32(150, 34, 28, 255);
    public static readonly Color Blau = new Color32(52, 120, 220, 255);
    public static readonly Color BlauFosc = new Color32(28, 66, 140, 255);
    public static readonly Color Gris = new Color32(56, 74, 106, 255);
    public static readonly Color GrisFosc = new Color32(30, 41, 62, 255);
    public static readonly Color Jugador1 = new Color32(25, 118, 210, 255);   // mateixos colors que el GeneradorHUD
    public static readonly Color Jugador2 = new Color32(211, 47, 47, 255);

    public static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }
    public static Color ColorJugador(int j) { return j == 0 ? Jugador1 : Jugador2; }

    // ---------- Sprites generats ----------
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    static Sprite Obtenir(string clau, System.Func<Sprite> crear)
    {
        Sprite s;
        if (cache.TryGetValue(clau, out s) && s != null) return s;
        s = crear();
        cache[clau] = s;
        return s;
    }

    // Rectangle arrodonit per fer servir com a 9-slice (radi 24 px)
    public static Sprite Arrodonit { get { return Obtenir("arrodonit", () => Dibuixar(64, p => Arrodonida(p, 0.75f), 24f)); } }
    public static Sprite Cercle { get { return Obtenir("cercle", () => Dibuixar(128, p => p.sqrMagnitude < 0.96f)); } }
    public static Sprite Anell { get { return Obtenir("anell", () => Dibuixar(128, p => p.sqrMagnitude < 0.96f && p.sqrMagnitude > 0.62f)); } }
    public static Sprite Vinyeta { get { return Obtenir("vinyeta", () => DibuixarAlfa(128, p => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.4f, p.magnitude)))); } }

    // Nuvol esponjos (unio de cercles amb la base plana i la vora suau). Pensat per a un rectangle 2:1
    public static Sprite Nuvol
    {
        get
        {
            return Obtenir("nuvol", () => DibuixarAlfa(192, p =>
            {
                Vector2 q = new Vector2(p.x, p.y * 0.55f);
                Vector3[] discs =
                {
                    new Vector3(-0.55f, -0.08f, 0.26f), new Vector3(-0.22f, 0.06f, 0.36f), new Vector3(0.16f, 0.1f, 0.4f),
                    new Vector3(0.5f, -0.02f, 0.3f), new Vector3(0.78f, -0.14f, 0.18f), new Vector3(-0.8f, -0.16f, 0.15f)
                };
                float d = float.MaxValue;
                foreach (Vector3 c in discs) d = Mathf.Min(d, (q - new Vector2(c.x, c.y * 0.55f)).magnitude - c.z * 0.55f);
                d = Mathf.Max(d, -0.2f - q.y);   // base plana
                return Mathf.Clamp01(0.5f - d * 40f);
            }));
        }
    }

    // Resplendor radial (sol)
    public static Sprite Resplendor { get { return Obtenir("resplendor", () => DibuixarAlfa(128, p => Mathf.Pow(Mathf.Clamp01(1f - p.magnitude), 2.2f))); } }

    public static Sprite IconaRatoliBuit
    {
        get
        {
            return Obtenir("ratoliBuit", () => Dibuixar(96, p =>
            {
                float ex = Mathf.Abs(p.x) / 0.56f, ey = Mathf.Abs(p.y) / 0.86f;
                float ix = Mathf.Abs(p.x) / 0.42f, iy = Mathf.Abs(p.y) / 0.72f;
                bool cos = ex * ex * ex * ex + ey * ey * ey * ey < 1f;
                bool dins = ix * ix * ix * ix + iy * iy * iy * iy < 1f;
                bool roda = Caixa(p, -0.06f, 0.2f, 0.06f, 0.5f);
                return (cos && !dins) || roda;
            }));
        }
    }

    // Globus terraqui (online)
    public static Sprite IconaXarxa
    {
        get
        {
            return Obtenir("xarxa", () => Dibuixar(96, p =>
            {
                float r = p.magnitude;
                if (r > 0.9f) return false;
                if (r > 0.76f) return true;                                         // vora
                float e = (p.x / 0.4f) * (p.x / 0.4f) + p.y * p.y / (0.83f * 0.83f);
                bool meridia = e > 0.78f && e < 1.05f;                              // el
                bool centre = Mathf.Abs(p.x) < 0.06f;
                bool equador = Mathf.Abs(p.y) < 0.06f;
                bool paral = Mathf.Abs(Mathf.Abs(p.y) - 0.42f) < 0.055f;
                return meridia || centre || equador || paral;
            }));
        }
    }

    // Dues persones (2 jugadors al mateix ordinador)
    public static Sprite IconaDosJugadors
    {
        get
        {
            return Obtenir("dos", () => Dibuixar(96, p =>
            {
                bool a = Disc(p, new Vector2(-0.32f, 0.38f), 0.22f) || (Disc(p, new Vector2(-0.32f, -0.62f), 0.48f) && p.y > -0.66f);
                bool b = Disc(p, new Vector2(0.36f, 0.3f), 0.2f) || (Disc(p, new Vector2(0.36f, -0.66f), 0.44f) && p.y > -0.66f);
                return a || b;
            }));
        }
    }

    // Robot (contra el bot)
    public static Sprite IconaBot
    {
        get
        {
            return Obtenir("bot", () => Dibuixar(96, p =>
            {
                bool cap = Arrodonida(new Vector2(p.x / 0.72f, (p.y + 0.12f) / 0.58f), 0.35f);
                bool ulls = Disc(p, new Vector2(-0.3f, -0.08f), 0.13f) || Disc(p, new Vector2(0.3f, -0.08f), 0.13f);
                bool boca = Caixa(p, -0.3f, -0.45f, 0.3f, -0.36f);
                bool antena = Caixa(p, -0.05f, 0.46f, 0.05f, 0.72f) || Disc(p, new Vector2(0f, 0.78f), 0.11f);
                bool orelles = Caixa(p, -0.86f, -0.28f, -0.74f, 0.06f) || Caixa(p, 0.74f, -0.28f, 0.86f, 0.06f);
                return (cap && !ulls && !boca) || antena || orelles;
            }));
        }
    }

    public static Sprite IconaPlay { get { return Obtenir("play", () => Dibuixar(96, p => Triangle(p, new Vector2(-0.42f, -0.62f), new Vector2(-0.42f, 0.62f), new Vector2(0.66f, 0f)))); } }
    public static Sprite IconaPausa { get { return Obtenir("pausa", () => Dibuixar(96, p => Caixa(p, -0.5f, -0.7f, -0.15f, 0.7f) || Caixa(p, 0.15f, -0.7f, 0.5f, 0.7f))); } }

    public static Sprite IconaEngranatge
    {
        get
        {
            return Obtenir("engranatge", () => Dibuixar(96, p =>
            {
                float r = p.magnitude;
                float a = Mathf.Atan2(p.y, p.x);
                bool dent = Mathf.Repeat(a / (Mathf.PI * 2f) * 8f, 1f) < 0.5f;
                return (r < 0.6f || (dent && r < 0.86f)) && r > 0.26f;
            }));
        }
    }

    public static Sprite IconaPersona
    {
        get
        {
            return Obtenir("persona", () => Dibuixar(96, p =>
                Disc(p, new Vector2(0f, 0.42f), 0.3f) || (Disc(p, new Vector2(0f, -0.78f), 0.66f) && p.y > -0.8f)));
        }
    }

    public static Sprite IconaSortir
    {
        get
        {
            return Obtenir("sortir", () => Dibuixar(96, p =>
                (Caixa(p, -0.78f, -0.82f, 0.12f, 0.82f) && !Caixa(p, -0.62f, -0.66f, -0.04f, 0.66f))
                || Caixa(p, -0.28f, -0.1f, 0.55f, 0.1f)
                || Triangle(p, new Vector2(0.45f, -0.36f), new Vector2(0.45f, 0.36f), new Vector2(0.9f, 0f))));
        }
    }

    public static Sprite IconaEnrere
    {
        get
        {
            return Obtenir("enrere", () => Dibuixar(96, p =>
                Caixa(p, -0.35f, -0.13f, 0.78f, 0.13f)
                || Triangle(p, new Vector2(-0.82f, 0f), new Vector2(-0.2f, 0.56f), new Vector2(-0.2f, -0.56f))));
        }
    }

    public static Sprite IconaBandera
    {
        get
        {
            return Obtenir("bandera", () => Dibuixar(96, p =>
                Caixa(p, -0.58f, -0.78f, -0.44f, 0.86f)
                || Triangle(p, new Vector2(-0.44f, 0.86f), new Vector2(-0.44f, 0.02f), new Vector2(0.74f, 0.44f))
                || Caixa(p, -0.82f, -0.9f, -0.2f, -0.76f)));
        }
    }

    public static Sprite IconaMina
    {
        get
        {
            return Obtenir("mina", () => Dibuixar(96, p =>
            {
                Vector2 q = new Vector2((p.x + p.y) * 0.7071f, (p.y - p.x) * 0.7071f);
                bool cos = p.sqrMagnitude < 0.25f;
                bool punxes = (Mathf.Abs(p.x) < 0.09f && Mathf.Abs(p.y) < 0.86f) || (Mathf.Abs(p.y) < 0.09f && Mathf.Abs(p.x) < 0.86f)
                    || (Mathf.Abs(q.x) < 0.08f && Mathf.Abs(q.y) < 0.7f) || (Mathf.Abs(q.y) < 0.08f && Mathf.Abs(q.x) < 0.7f);
                bool brillantor = Disc(p, new Vector2(-0.18f, 0.18f), 0.11f);
                return (cos || punxes) && !brillantor;
            }));
        }
    }

    public static Sprite IconaCasa
    {
        get
        {
            return Obtenir("casa", () => Dibuixar(96, p =>
                Triangle(p, new Vector2(-0.86f, 0.08f), new Vector2(0.86f, 0.08f), new Vector2(0f, 0.84f))
                || (Caixa(p, -0.6f, -0.82f, 0.6f, 0.1f) && !Caixa(p, -0.17f, -0.82f, 0.17f, -0.25f))));
        }
    }

    public static Sprite IconaReiniciar
    {
        get
        {
            return Obtenir("reiniciar", () => Dibuixar(96, p =>
            {
                float r = p.magnitude;
                float a = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                bool arc = r > 0.46f && r < 0.7f && !(a > 40f && a < 110f);
                Vector2 c = new Vector2(Mathf.Cos(40f * Mathf.Deg2Rad), Mathf.Sin(40f * Mathf.Deg2Rad)) * 0.58f;
                Vector2 radial = c.normalized;
                Vector2 tangent = new Vector2(-radial.y, radial.x);
                bool punta = Triangle(p, c + radial * 0.3f, c - radial * 0.3f, c + tangent * 0.36f);
                return arc || punta;
            }));
        }
    }

    // Ratoli amb el boto esquerre (o dret) pintat
    public static Sprite IconaRatoli(bool esquerre)
    {
        return Obtenir(esquerre ? "ratoliE" : "ratoliD", () => Dibuixar(96, p =>
        {
            float ex = Mathf.Abs(p.x) / 0.56f, ey = Mathf.Abs(p.y) / 0.86f;
            float ix = Mathf.Abs(p.x) / 0.42f, iy = Mathf.Abs(p.y) / 0.72f;
            bool cos = ex * ex * ex * ex + ey * ey * ey * ey < 1f;
            bool dins = ix * ix * ix * ix + iy * iy * iy * iy < 1f;
            bool boto = dins && p.y > 0.06f && (esquerre ? p.x < -0.05f : p.x > 0.05f);
            return (cos && !dins) || boto;
        }));
    }

    // Graella de n x n caselles (icona de les targetes de nivell)
    public static Sprite Graella(int n)
    {
        return Obtenir("graella" + n, () => Dibuixar(128, p =>
        {
            float u = (p.x + 1f) * 0.5f * n, v = (p.y + 1f) * 0.5f * n;
            float fu = u - Mathf.Floor(u), fv = v - Mathf.Floor(v);
            const float g = 0.12f;
            return fu > g && fu < 1f - g && fv > g && fv < 1f - g;
        }));
    }

    // Textura amb un degradat vertical (fons dels menus)
    public static Texture2D Degradat(Color dalt, Color baix)
    {
        Texture2D t = new Texture2D(2, 256, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 256; y++)
        {
            Color c = Color.Lerp(baix, dalt, Mathf.SmoothStep(0f, 1f, y / 255f));
            t.SetPixel(0, y, c);
            t.SetPixel(1, y, c);
        }
        t.Apply();
        return t;
    }

    static Sprite Dibuixar(int n, System.Func<Vector2, bool> dins, float vora = 0f)
    {
        const int S = 4;   // supermostreig per suavitzar les vores
        return DibuixarAlfa(n, null, vora, (x, y) =>
        {
            int c = 0;
            for (int sy = 0; sy < S; sy++)
                for (int sx = 0; sx < S; sx++)
                {
                    Vector2 p = new Vector2((x + (sx + 0.5f) / S) / n * 2f - 1f, (y + (sy + 0.5f) / S) / n * 2f - 1f);
                    if (dins(p)) c++;
                }
            return c / (float)(S * S);
        });
    }

    static Sprite DibuixarAlfa(int n, System.Func<Vector2, float> alfa, float vora = 0f, System.Func<int, int, float> alfaPixel = null)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float a = alfaPixel != null ? alfaPixel(x, y) : alfa(new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f));
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
            }
        t.SetPixels32(px);
        t.Apply(false, true);
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(vora, vora, vora, vora));
    }

    static bool Arrodonida(Vector2 p, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x) - (1f - r), Mathf.Abs(p.y) - (1f - r));
        if (q.x <= 0f || q.y <= 0f) return Mathf.Abs(p.x) <= 1f && Mathf.Abs(p.y) <= 1f;
        return q.sqrMagnitude <= r * r;
    }

    static bool Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Creu(p, a, b), d2 = Creu(p, b, c), d3 = Creu(p, c, a);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }

    static float Creu(Vector2 p, Vector2 a, Vector2 b) { return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y); }
    static bool Caixa(Vector2 p, float x0, float y0, float x1, float y1) { return p.x >= x0 && p.x <= x1 && p.y >= y0 && p.y <= y1; }
    static bool Disc(Vector2 p, Vector2 c, float r) { return (p - c).sqrMagnitude <= r * r; }

    // ---------- Constructors d'elements ----------
    public static Canvas CrearCanvas(string nom, int ordre)
    {
        GameObject g = new GameObject(nom, typeof(RectTransform));
        Canvas c = g.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = ordre;
        CanvasScaler s = g.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920f, 1080f);
        s.matchWidthOrHeight = 0.5f;
        g.AddComponent<GraphicRaycaster>();
        AssegurarEventSystem();
        return c;
    }

    public static void AssegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject g = new GameObject("EventSystem");
        g.AddComponent<EventSystem>();
        InputSystemUIInputModule m = g.AddComponent<InputSystemUIInputModule>();
        m.AssignDefaultActions();
    }

    public static RectTransform Nou(string nom, Transform pare)
    {
        GameObject g = new GameObject(nom, typeof(RectTransform));
        g.transform.SetParent(pare, false);
        return (RectTransform)g.transform;
    }

    // Ancora i pivot al mateix punt (0..1): la posicio es mesura des d'aquell punt del pare
    public static RectTransform Posar(RectTransform rt, Vector2 ancora, Vector2 pos, Vector2 mida)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        rt.anchoredPosition = pos;
        rt.sizeDelta = mida;
        return rt;
    }

    public static RectTransform Omplir(RectTransform rt, float marge = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(marge, marge);
        rt.offsetMax = new Vector2(-marge, -marge);
        return rt;
    }

    public static Image Imatge(string nom, Transform pare, Sprite s, Color c, float radi = 0f)
    {
        RectTransform rt = Nou(nom, pare);
        Image im = rt.gameObject.AddComponent<Image>();
        im.sprite = s;
        im.color = c;
        im.raycastTarget = false;
        if (radi > 0f)
        {
            im.type = Image.Type.Sliced;
            im.pixelsPerUnitMultiplier = 24f / radi;
        }
        return im;
    }

    // Panell arrodonit amb una ombra suau a sota
    public static Image Panell(string nom, Transform pare, Color c, float radi = 28f, bool ombra = true)
    {
        Image im = Imatge(nom, pare, Arrodonit, c, radi);
        if (ombra)
        {
            Shadow sh = im.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.35f);
            sh.effectDistance = new Vector2(0f, -8f);
        }
        return im;
    }

    public static TextMeshProUGUI Text(string nom, Transform pare, string txt, float mida, Color c,
        TextAlignmentOptions al = TextAlignmentOptions.Center, FontStyles estil = FontStyles.Bold)
    {
        RectTransform rt = Nou(nom, pare);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = txt;
        t.fontSize = mida;
        t.color = c;
        t.alignment = al;
        t.fontStyle = estil;
        t.raycastTarget = false;
        t.richText = true;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    // Contorn fosc al voltant del text (perque es llegeixi sobre el mapa)
    public static void Contorn(TMP_Text t, float gruix)
    {
        t.outlineWidth = gruix;
        t.outlineColor = new Color32(0, 0, 0, 170);
    }

    // Boto amb estil 3D: ombra de color fosc a sota, cara de color i una lleugera brillantor a dalt
    public static Button Boto(Transform pare, string text, Sprite icona, Color color, Color colorOmbra, UnityAction accio,
        Vector2 mida, float midaText = 38f, float radi = 20f)
    {
        RectTransform arrel = Nou("Boto_" + text, pare);
        arrel.sizeDelta = mida;

        Image ombra = Imatge("Ombra", arrel, Arrodonit, colorOmbra, radi);
        Omplir(ombra.rectTransform);
        ombra.rectTransform.offsetMin = new Vector2(0f, -8f);
        ombra.rectTransform.offsetMax = new Vector2(0f, -8f);

        Image cara = Imatge("Cara", arrel, Arrodonit, color, radi);
        Omplir(cara.rectTransform);
        cara.raycastTarget = true;

        Image brillo = Imatge("Brillantor", cara.transform, Arrodonit, new Color(1f, 1f, 1f, 0.1f), radi * 0.8f);
        RectTransform rb = brillo.rectTransform;
        rb.anchorMin = new Vector2(0f, 0.5f);
        rb.anchorMax = Vector2.one;
        rb.offsetMin = new Vector2(5f, 0f);
        rb.offsetMax = new Vector2(-5f, -4f);

        float esquerra = 0f;
        if (icona != null)
        {
            float mi = Mathf.Min(mida.y * 0.46f, 50f);
            Image ic = Imatge("Icona", cara.transform, icona, Color.white);
            Posar(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(mida.y * 0.36f, 0f), new Vector2(mi, mi));
            esquerra = mida.y * 0.36f + mi + 18f;
        }

        TextMeshProUGUI t = Text("Text", cara.transform, text, midaText, Color.white,
            icona != null ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
        Omplir(t.rectTransform);
        t.rectTransform.offsetMin = new Vector2(esquerra, 0f);

        Button b = arrel.gameObject.AddComponent<Button>();
        b.targetGraphic = cara;
        b.transition = Selectable.Transition.None;
        if (accio != null) b.onClick.AddListener(accio);

        BotoAnimat an = arrel.gameObject.AddComponent<BotoAnimat>();
        an.cara = cara.rectTransform;
        an.imatgeCara = cara;
        an.color = color;
        return b;
    }

    // Slider amb etiqueta i valor
    public static Slider CrearSlider(Transform pare, string etiqueta, float min, float max, float valor,
        System.Func<float, string> format, UnityAction<float> canvi, Vector2 mida, Color colorFill)
    {
        RectTransform arrel = Nou("Slider_" + etiqueta, pare);
        arrel.sizeDelta = mida;

        TextMeshProUGUI tEt = Text("Etiqueta", arrel, etiqueta, 30f, TextClar, TextAlignmentOptions.Left);
        Posar(tEt.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(mida.x * 0.7f, 40f));
        TextMeshProUGUI tVal = Text("Valor", arrel, format(valor), 30f, TextSuau, TextAlignmentOptions.Right);
        Posar(tVal.rectTransform, new Vector2(1f, 1f), Vector2.zero, new Vector2(mida.x * 0.3f, 40f));

        RectTransform pista = Posar(Nou("Pista", arrel), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(mida.x, 36f));
        Image fons = Imatge("Fons", pista, Arrodonit, new Color(1f, 1f, 1f, 0.12f), 9f);
        RectTransform rf = fons.rectTransform;
        rf.anchorMin = new Vector2(0f, 0.25f); rf.anchorMax = new Vector2(1f, 0.75f);
        rf.offsetMin = rf.offsetMax = Vector2.zero;
        fons.raycastTarget = true;

        RectTransform zonaFill = Nou("Zona_Fill", pista);
        zonaFill.anchorMin = new Vector2(0f, 0.25f); zonaFill.anchorMax = new Vector2(1f, 0.75f);
        zonaFill.offsetMin = Vector2.zero; zonaFill.offsetMax = new Vector2(-18f, 0f);
        Image fill = Imatge("Fill", zonaFill, Arrodonit, colorFill, 9f);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = new Vector2(18f, 0f);

        RectTransform zonaMan = Nou("Zona_Maneta", pista);
        zonaMan.anchorMin = Vector2.zero; zonaMan.anchorMax = Vector2.one;
        zonaMan.offsetMin = new Vector2(18f, 0f); zonaMan.offsetMax = new Vector2(-18f, 0f);
        Image maneta = Imatge("Maneta", zonaMan, Cercle, Color.white);
        maneta.rectTransform.anchorMin = Vector2.zero; maneta.rectTransform.anchorMax = new Vector2(0f, 1f);
        maneta.rectTransform.sizeDelta = new Vector2(36f, 0f);
        maneta.raycastTarget = true;
        Shadow sh = maneta.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.35f);
        sh.effectDistance = new Vector2(0f, -3f);

        Slider s = pista.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = maneta.rectTransform;
        s.targetGraphic = maneta;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min;
        s.maxValue = max;
        s.value = valor;
        s.onValueChanged.AddListener(v => tVal.text = format(v));
        if (canvi != null) s.onValueChanged.AddListener(canvi);
        return s;
    }

    // Camp de text d'una linia (TMP_InputField fet per codi). Es crea desactivat i s'activa quan ja te totes les referencies
    public static TMP_InputField CampText(Transform pare, string placeholder, Vector2 mida, int limit, float midaText = 40f)
    {
        RectTransform caixa = Nou("Camp_" + placeholder, pare);
        caixa.sizeDelta = mida;
        caixa.gameObject.SetActive(false);
        Image fons = caixa.gameObject.AddComponent<Image>();
        fons.sprite = Arrodonit;
        fons.type = Image.Type.Sliced;
        fons.pixelsPerUnitMultiplier = 24f / 20f;
        fons.color = new Color(1f, 1f, 1f, 0.12f);
        RectTransform area = Omplir(Nou("Area", caixa));
        area.offsetMin = new Vector2(28f, 6f);
        area.offsetMax = new Vector2(-28f, -6f);
        area.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI ph = Text("Placeholder", area, placeholder, midaText, new Color(1f, 1f, 1f, 0.3f), TextAlignmentOptions.Center, FontStyles.Italic);
        Omplir(ph.rectTransform);
        TextMeshProUGUI tx = Text("Text", area, "", midaText, Color.white, TextAlignmentOptions.Center);
        Omplir(tx.rectTransform);
        TMP_InputField camp = caixa.gameObject.AddComponent<TMP_InputField>();
        camp.textViewport = area;
        camp.textComponent = tx;
        camp.placeholder = ph;
        camp.targetGraphic = fons;
        camp.characterLimit = limit;
        camp.lineType = TMP_InputField.LineType.SingleLine;
        camp.customCaretColor = true;
        camp.caretColor = Color.white;
        camp.caretWidth = 3;
        camp.selectionColor = new Color(1f, 0.7f, 0.15f, 0.45f);
        caixa.gameObject.SetActive(true);
        return camp;
    }

    // Tecla de teclat dibuixada (per als controls). Torna l'amplada que ocupa
    public static float Tecla(Transform pare, string text, Vector2 pos, float amplada = 44f)
    {
        Image ombra = Imatge("Tecla_" + text, pare, Arrodonit, new Color32(12, 18, 30, 255), 9f);
        Posar(ombra.rectTransform, new Vector2(0f, 1f), pos + new Vector2(0f, -4f), new Vector2(amplada, 42f));
        Image cara = Imatge("Cara", ombra.transform, Arrodonit, new Color32(232, 238, 248, 255), 9f);
        Omplir(cara.rectTransform);
        cara.rectTransform.offsetMin = new Vector2(0f, 4f);
        cara.rectTransform.offsetMax = new Vector2(0f, 4f);
        TextMeshProUGUI t = Text("Text", cara.transform, text, text.Length > 2 ? 18f : 22f, new Color32(30, 41, 62, 255));
        Omplir(t.rectTransform);
        return amplada;
    }

    // Fons fosc que tapa tota la pantalla i bloqueja els clics
    public static Image Enfosquir(Transform pare, float alfa)
    {
        Image im = Imatge("Enfosquit", pare, null, new Color(0.02f, 0.04f, 0.08f, alfa));
        Omplir(im.rectTransform);
        im.raycastTarget = true;
        return im;
    }
}
