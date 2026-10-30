using UnityEngine;
using TMPro;

// Rombe de color i nom flotant sobre el cap d'un jugador (per distingir J1 i J2 des de la vista aeria).
public class MarcadorJugador : MonoBehaviour
{
    public Color color = Color.white;
    public string nom = "";
    public float alcada = 2.35f;

    Transform rombe;
    TextMeshPro text;

    void Start()
    {
        rombe = EfectesJoc.Primitiva(PrimitiveType.Cube, transform, color, Vector3.up * alcada, Vector3.one * 0.24f).transform;
        rombe.localRotation = Quaternion.Euler(45f, 0f, 45f);

        GameObject g = new GameObject("Nom");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = Vector3.up * (alcada + 0.5f);
        text = g.AddComponent<TextMeshPro>();
        text.text = nom;
        text.fontSize = 4f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.Lerp(color, Color.white, 0.3f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color32(0, 0, 0, 200);
        text.rectTransform.sizeDelta = new Vector2(4f, 1f);
    }

    public void PosarNom(string n)
    {
        nom = n;
        if (text != null) text.text = n;
    }

    public void Mostrar(bool visible)
    {
        if (rombe != null) rombe.gameObject.SetActive(visible);
        if (text != null) text.gameObject.SetActive(visible);
    }

    void LateUpdate()
    {
        if (rombe == null) return;
        rombe.localPosition = Vector3.up * (alcada + Mathf.Sin(Time.time * 3f) * 0.08f);
        rombe.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
        Camera cam = Camera.main;
        if (cam != null && text != null)
            text.transform.rotation = Quaternion.LookRotation(text.transform.position - cam.transform.position);
    }
}
