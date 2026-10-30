using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu BigBOOM > Preparar escena del mapa
// Deixa EscenaMapa igual que quan es juga: genera el tauler a la mida de la zona (Columnes x Files del Joc, 16x16)
// i treu tota la decoracio (arbres, roques, gespa...) que toca la zona del tauler, mirant la mida real de cada model.
public static class PrepararEscenaMapa
{
    const float Marge = 3f;          // metres lliures al voltant del tauler
    const float MargeSpawn = 8f;     // costat on apareix el jugador i hi ha la camera

    static readonly string[] noTocar = { "illa", "escenari", "taulell", "casella", "fila_", "castle", "wall", "tower", "gate", "hud", "player", "joc" };

    [MenuItem("BigBOOM/Preparar escena del mapa")]
    static void Preparar()
    {
        GeneradorTaulell gt = Object.FindFirstObjectByType<GeneradorTaulell>(FindObjectsInactive.Include);
        JocBuscamines joc = Object.FindFirstObjectByType<JocBuscamines>(FindObjectsInactive.Include);
        if (gt == null || joc == null)
        {
            EditorUtility.DisplayDialog("BigBOOM", "Obre EscenaMapa: no trobo el Taulell o el Joc.", "D'acord");
            return;
        }

        // 1. Tauler a la mida de la zona (el mateix que es veu en jugar el nivell mes gran)
        int c = joc.columnes, f = joc.files;
        MethodInfo generar = typeof(GeneradorTaulell).GetMethod("Generar", BindingFlags.NonPublic | BindingFlags.Instance);
        if (generar != null) generar.Invoke(gt, new object[] { c, f, Mathf.Min(joc.mines, c * f - 9) });

        // 2. Zona del tauler + marge (mes gran pel costat de la camera aeria, on apareix el jugador)
        Transform t = gt.transform;
        float m = gt.mida;
        Bounds zona = new Bounds(t.TransformPoint(new Vector3(-m * 0.5f, 0f, -m * 0.5f)), Vector3.zero);
        zona.Encapsulate(t.TransformPoint(new Vector3((c - 1) * m + m * 0.5f, 0f, (f - 1) * m + m * 0.5f)));
        Vector3 min = zona.min - new Vector3(Marge, 0f, Marge), max = zona.max + new Vector3(Marge, 0f, Marge);
        Vector3 d = joc.posicioCamera;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.z)) { if (d.x < 0f) min.x -= MargeSpawn; else max.x += MargeSpawn; }
        else { if (d.z < 0f) min.z -= MargeSpawn; else max.z += MargeSpawn; }

        float yTauler = t.position.y;
        Renderer[] rt = t.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rt) yTauler = Mathf.Max(yTauler, r.bounds.max.y);

        // 3. Decoracio que toca la zona
        List<GameObject> treure = new List<GameObject>();
        foreach (Transform tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            GameObject go = tr.gameObject;
            if (go.scene != gt.gameObject.scene) continue;
            if (PrefabUtility.GetNearestPrefabInstanceRoot(go) != go) continue;      // nomes models sencers
            if (tr.IsChildOf(t) || tr.IsChildOf(joc.transform) || go.GetComponentInChildren<PlayerController>(true) != null) continue;
            if (NomProtegit(go)) continue;

            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.x > 25f || b.size.z > 25f) continue;                          // res massa gran (illes, castell...)
            if (b.min.y > yTauler + 20f || b.max.y < yTauler - 3f) continue;           // ni les illes flotants ni les roques de sota
            if (b.max.x < min.x || b.min.x > max.x || b.max.z < min.z || b.min.z > max.z) continue;
            treure.Add(go);
        }

        int fets = 0, amagats = 0;
        foreach (GameObject go in treure)
        {
            if (go == null) continue;
            try { Undo.DestroyObjectImmediate(go); fets++; }
            catch { Undo.RecordObject(go, "Amagar decoracio"); go.SetActive(false); amagats++; }
        }
        EditorSceneManager.MarkSceneDirty(gt.gameObject.scene);

        string msg = "Tauler generat a " + c + "x" + f + ".\nDecoracio sobre la zona: " + fets + " esborrats"
            + (amagats > 0 ? ", " + amagats + " desactivats" : "") + ".\n\nDesa l'escena (Ctrl+S) per guardar-ho.";
        Debug.Log("BigBOOM > Preparar escena del mapa: " + msg.Replace("\n", " "));
        EditorUtility.DisplayDialog("BigBOOM", msg, "D'acord");
    }

    // No es toca res que formi part de l'illa, el tauler, el castell, el HUD o els jugadors
    static bool NomProtegit(GameObject go)
    {
        for (Transform t = go.transform; t != null; t = t.parent)
        {
            string n = t.name.ToLower();
            if (n == "illa_espera" || n.StartsWith("illes_fons")) return true;
            if (t == go.transform)
                foreach (string p in noTocar)
                    if (n.Contains(p)) return true;
            if (n.Contains("castle")) return true;
        }
        return false;
    }
}
