using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class GeneradorHUD : MonoBehaviour
{
    [Header("Arrossega aqui el Taulell")]
    public Transform taulell;

    [Header("Posicio i mida de la pantalla")]
    [Tooltip("Metres per sobre de la superficie del tauler")]
    public float alturaPantalla = 28f;
    [Tooltip("Desplacament horitzontal respecte al centre del tauler (X, Z)")]
    public Vector2 desplacament = Vector2.zero;
    [Tooltip("Mida del panell en unitats de Canvas")]
    public Vector2 midaPanell = new Vector2(1600f, 600f);
    [Tooltip("Metres per unitat de Canvas. 0.018 -> panell d'uns 29 m d'ample")]
    public float escalaPantalla = 0.018f;
    [Tooltip("Gir fix (Y) si no es fa servir Mirar camera")]
    public float girY = 0f;
    [Tooltip("Si esta activat, la pantalla sempre mira la camera (nomes eix Y)")]
    public bool mirarCamera = true;
    [Tooltip("Gruix del marc en unitats de Canvas")]
    public float gruixMarc = 16f;

    [Header("Colors (agafats de les caselles tapades)")]
    public Color colorPanell = new Color32(170, 215, 81, 150);   // #AAD751 semitransparent
    public Color colorMarc = new Color32(74, 117, 44, 255);      // #4A752C
    public Color colorText = new Color32(30, 58, 15, 255);       // verd molt fosc
    public Color colorJugador1 = new Color32(25, 118, 210, 255); // blau
    public Color colorJugador2 = new Color32(211, 47, 47, 255);  // vermell

    [Header("Referencies (les omple el generador)")]
    public Transform pantalla;
    public TMP_Text txtTornPartida;
    public TMP_Text txtJugadorTorn;
    public TMP_Text txtTemps;
    public TMP_Text txtPuntsTorn;
    public TMP_Text txtNom1;
    public TMP_Text txtNom2;
    public TMP_Text txtPunts1;
    public TMP_Text txtPunts2;
    public TMP_Text txtGuanya;

    // ---------- API per a la logica de joc ----------
    public void ActualitzarHUD(int tornPartida, string jugadorTorn, float tempsRestant, int puntsTorn, int punts1, int punts2)
    {
        if (txtTornPartida != null) txtTornPartida.text = "Torn_Partida: " + tornPartida;
        if (txtJugadorTorn != null)
        {
            txtJugadorTorn.text = jugadorTorn;
            if (txtNom1 != null && jugadorTorn == txtNom1.text) txtJugadorTorn.color = colorJugador1;
            else if (txtNom2 != null && jugadorTorn == txtNom2.text) txtJugadorTorn.color = colorJugador2;
            else txtJugadorTorn.color = colorText;
        }
        if (txtTemps != null) txtTemps.text = Mathf.CeilToInt(Mathf.Max(0f, tempsRestant)).ToString();
        if (txtPuntsTorn != null) txtPuntsTorn.text = (puntsTorn >= 0 ? "+" : "") + puntsTorn + " PTS";
        if (txtPunts1 != null) txtPunts1.text = punts1 + " PTS";
        if (txtPunts2 != null) txtPunts2.text = punts2 + " PTS";
        if (txtGuanya != null)
            txtGuanya.text = punts1 > punts2 ? "<" : (punts2 > punts1 ? ">" : "\u2605");
    }

    public void PosarNoms(string nom1, string nom2)
    {
        if (txtNom1 != null) txtNom1.text = nom1;
        if (txtNom2 != null) txtNom2.text = nom2;
    }

    void LateUpdate()
    {
        if (!mirarCamera || pantalla == null || Camera.main == null) return;
        Vector3 d = pantalla.position - Camera.main.transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.001f) pantalla.rotation = Quaternion.LookRotation(d);
    }

#if UNITY_EDITOR
    [ContextMenu("Generar HUD")]
    void Generar()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);

        Vector3 centre = transform.position;
        if (taulell != null)
        {
            Renderer[] rends = taulell.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                centre = b.center;
                centre.y = b.max.y;
            }
        }
        else Debug.LogWarning("No has arrossegat el Taulell: la pantalla es posa on hi ha aquest objecte.");

        Sprite panell = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite cercle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject cg = new GameObject("HUD_Pantalla", typeof(RectTransform));
        cg.transform.SetParent(transform, false);
        Canvas cv = cg.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        RectTransform crt = cg.GetComponent<RectTransform>();
        crt.sizeDelta = midaPanell;
        cg.transform.localScale = Vector3.one * escalaPantalla;
        cg.transform.position = centre + new Vector3(desplacament.x, alturaPantalla, desplacament.y);
        cg.transform.rotation = Quaternion.Euler(0f, girY, 0f);
        pantalla = cg.transform;

        // Marc (verd fosc) i fons (verd semitransparent) a dins
        Image marc = NovaImatge("Marc", cg.transform, Vector2.zero, midaPanell, colorMarc, panell);
        marc.type = Image.Type.Sliced;
        marc.pixelsPerUnitMultiplier = 2.5f;
        Image fons = NovaImatge("Fons", cg.transform, Vector2.zero, midaPanell - new Vector2(gruixMarc * 2f, gruixMarc * 2f), colorPanell, panell);
        fons.type = Image.Type.Sliced;
        fons.pixelsPerUnitMultiplier = 2.5f;

        float w = midaPanell.x * 0.5f;
        float h = midaPanell.y * 0.5f;
        float xj = w * 0.62f;

        txtTornPartida = NouText("Torn_Partida", cg.transform, new Vector2(-w * 0.62f, h * 0.78f), new Vector2(520f, 70f),
            "Torn_Partida: 0", 46f, FontStyles.Bold, TextAlignmentOptions.Left, colorText);

        NouText("Etiqueta_Torn", cg.transform, new Vector2(0f, h * 0.78f), new Vector2(500f, 60f),
            "TORN ACTUAL", 40f, FontStyles.Bold, TextAlignmentOptions.Center, colorText);
        txtJugadorTorn = NouText("Jugador_Torn", cg.transform, new Vector2(0f, h * 0.50f), new Vector2(500f, 80f),
            "PLAYER_1", 58f, FontStyles.Bold, TextAlignmentOptions.Center, colorJugador1);
        txtTemps = NouText("Temps", cg.transform, new Vector2(0f, h * 0.08f), new Vector2(300f, 160f),
            "12", 130f, FontStyles.Bold, TextAlignmentOptions.Center, colorText);
        txtPuntsTorn = NouText("Punts_Torn", cg.transform, new Vector2(0f, -h * 0.42f), new Vector2(500f, 100f),
            "+6 PTS", 72f, FontStyles.Bold, TextAlignmentOptions.Center, colorText);

        txtGuanya = NouText("Indicador_Guanya", cg.transform, new Vector2(0f, -h * 0.80f), new Vector2(200f, 90f),
            "<", 80f, FontStyles.Bold, TextAlignmentOptions.Center, colorMarc);

        txtNom1 = NouText("Nom_Jugador1", cg.transform, new Vector2(-xj, h * 0.50f), new Vector2(420f, 70f),
            "PLAYER_1", 50f, FontStyles.Bold, TextAlignmentOptions.Center, colorJugador1);
        NovaImatge("Icona_Jugador1", cg.transform, new Vector2(-xj, h * 0.02f), new Vector2(200f, 200f), new Color(1f, 1f, 1f, 0.9f), cercle);
        txtPunts1 = NouText("Punts_Jugador1", cg.transform, new Vector2(-xj, -h * 0.55f), new Vector2(420f, 80f),
            "70 PTS", 54f, FontStyles.Bold, TextAlignmentOptions.Center, colorText);

        txtNom2 = NouText("Nom_Jugador2", cg.transform, new Vector2(xj, h * 0.50f), new Vector2(420f, 70f),
            "PLAYER_2", 50f, FontStyles.Bold, TextAlignmentOptions.Center, colorJugador2);
        NovaImatge("Icona_Jugador2", cg.transform, new Vector2(xj, h * 0.02f), new Vector2(200f, 200f), new Color(1f, 1f, 1f, 0.9f), cercle);
        txtPunts2 = NouText("Punts_Jugador2", cg.transform, new Vector2(xj, -h * 0.55f), new Vector2(420f, 80f),
            "67 PTS", 54f, FontStyles.Bold, TextAlignmentOptions.Center, colorText);

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("HUD generat a " + cg.transform.position);
    }

    Image NovaImatge(string nom, Transform pare, Vector2 pos, Vector2 mida, Color c, Sprite s)
    {
        GameObject g = new GameObject(nom, typeof(RectTransform));
        g.transform.SetParent(pare, false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = mida;
        Image im = g.AddComponent<Image>();
        im.sprite = s;
        im.color = c;
        im.raycastTarget = false;
        return im;
    }

    TMP_Text NouText(string nom, Transform pare, Vector2 pos, Vector2 mida, string txt, float fs, FontStyles st, TextAlignmentOptions al, Color c)
    {
        GameObject g = new GameObject(nom, typeof(RectTransform));
        g.transform.SetParent(pare, false);
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = mida;
        TextMeshProUGUI t = g.AddComponent<TextMeshProUGUI>();
        t.text = txt;
        t.fontSize = fs;
        t.fontStyle = st;
        t.alignment = al;
        t.color = c;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }
#endif
}