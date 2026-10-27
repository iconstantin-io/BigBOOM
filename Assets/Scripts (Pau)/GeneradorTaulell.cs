using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[System.Serializable]
public class JocCaselles
{
    [Tooltip("Element 0 = cap mina al voltant, 1 = una mina, ... fins a 8")]
    public GameObject[] numeros = new GameObject[9];
    public GameObject mina;
}

public class GeneradorTaulell : MonoBehaviour
{
    [Header("Caselles TAPADES")]
    public JocCaselles tapadesModel1 = new JocCaselles();
    public JocCaselles tapadesModel2 = new JocCaselles();

    [Header("Caselles DESCOBERTES")]
    public JocCaselles descobertesModel1 = new JocCaselles();
    public JocCaselles descobertesModel2 = new JocCaselles();

    [Header("Ajustos")]
    public float mida = 2f;
    public bool obrirZonaInicial = true;
    [Tooltip("Gira el taulell 180 graus sobre l'eix Y")]
    public bool girar180 = true;
    [Tooltip("0 = un mapa diferent cada cop. Un altre número = sempre el mateix mapa.")]
    public int llavor = 0;

#if UNITY_EDITOR
    bool avisFalten;

    [ContextMenu("Generar FACIL (9x9, 10 mines)")]
    void GenerarFacil() { Generar(9, 9, 10); }

    [ContextMenu("Generar NORMAL (16x16, 40 mines)")]
    void GenerarNormal() { Generar(16, 16, 40); }

    [ContextMenu("Generar DIFICIL (30x16, 99 mines)")]
    void GenerarDificil() { Generar(30, 16, 99); }

    void Generar(int columnes, int files, int mines)
    {
        avisFalten = false;
        var rnd = llavor == 0 ? new System.Random() : new System.Random(llavor);

        // 1. Casella de partida (a l'interior, perquè l'obertura sigui gran)
        int sf = rnd.Next(1, files - 1);
        int sc = rnd.Next(1, columnes - 1);

        // 2. Posicions possibles per a les mines (mai a la zona de partida)
        var candidats = new List<Vector2Int>();
        for (int f = 0; f < files; f++)
            for (int c = 0; c < columnes; c++)
            {
                bool prop = Mathf.Abs(f - sf) <= 1 && Mathf.Abs(c - sc) <= 1;
                if (obrirZonaInicial && prop) continue;
                candidats.Add(new Vector2Int(c, f));
            }

        for (int i = candidats.Count - 1; i > 0; i--)   // barrejar
        {
            int j = rnd.Next(i + 1);
            (candidats[i], candidats[j]) = (candidats[j], candidats[i]);
        }

        if (mines > candidats.Count) mines = candidats.Count;
        bool[,] esMina = new bool[files, columnes];
        for (int i = 0; i < mines; i++) esMina[candidats[i].y, candidats[i].x] = true;

        // 3. Calcular els números
        int[,] num = new int[files, columnes];
        for (int f = 0; f < files; f++)
            for (int c = 0; c < columnes; c++)
            {
                int n = 0;
                for (int df = -1; df <= 1; df++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int ff = f + df, cc = c + dc;
                        if ((df != 0 || dc != 0) && ff >= 0 && ff < files && cc >= 0 && cc < columnes && esMina[ff, cc]) n++;
                    }
                num[f, c] = n;
            }

        // 4. Obrir la zona inicial en cascada
        bool[,] oberta = new bool[files, columnes];
        if (obrirZonaInicial)
        {
            var cua = new Queue<Vector2Int>();
            oberta[sf, sc] = true;
            cua.Enqueue(new Vector2Int(sc, sf));
            while (cua.Count > 0)
            {
                var p = cua.Dequeue();
                if (num[p.y, p.x] != 0) continue;
                for (int df = -1; df <= 1; df++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int ff = p.y + df, cc = p.x + dc;
                        if (ff < 0 || ff >= files || cc < 0 || cc >= columnes) continue;
                        if (oberta[ff, cc] || esMina[ff, cc]) continue;
                        oberta[ff, cc] = true;
                        cua.Enqueue(new Vector2Int(cc, ff));
                    }
            }
        }

        // 5. Esborrar el taulell anterior
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // 6. Crear files i caselles
        for (int f = 0; f < files; f++)
        {
            var fila = new GameObject($"Fila_{f + 1:00}");
            fila.transform.SetParent(transform, false);

            for (int c = 0; c < columnes; c++)
            {
                GameObject prefab = Triar(oberta[f, c], f, c, esMina[f, c], num[f, c]);
                if (prefab == null) continue;

                GameObject go = PrefabUtility.IsPartOfPrefabAsset(prefab)
                    ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, fila.transform)
                    : Instantiate(prefab, fila.transform);

                float px = c * mida;
                float pz = (files - 1 - f) * mida;
                if (girar180)
                {
                    px = (columnes - 1) * mida - px;
                    pz = (files - 1) * mida - pz;
                }
                go.transform.localPosition = new Vector3(px, 0, pz);
                if (girar180) go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                go.name = $"Casella_{f + 1:00}_{c + 1:00}";
            }
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);

        // 7. Mapa al Console
        var sb = new StringBuilder();
        sb.AppendLine($"Taulell {columnes}x{files}, {mines} mines. Partida a fila {sf + 1}, columna {sc + 1}");
        sb.AppendLine("(* = mina, . = 0, O = oberta)");
        for (int f = 0; f < files; f++)
        {
            for (int c = 0; c < columnes; c++)
                sb.Append(esMina[f, c] ? '*' : (num[f, c] == 0 ? '.' : num[f, c].ToString()[0]));
            sb.Append("    ");
            for (int c = 0; c < columnes; c++)
                sb.Append(oberta[f, c] ? 'O' : '#');
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }

    GameObject Triar(bool oberta, int f, int c, bool esMina, int n)
    {
        bool model1 = (f + c) % 2 == 0;   // alterna com un tauler d'escacs
        JocCaselles joc = oberta
            ? (model1 ? descobertesModel1 : descobertesModel2)
            : (model1 ? tapadesModel1 : tapadesModel2);

        if (esMina) return joc.mina;

        for (int i = n; i >= 0; i--)
        {
            if (i < joc.numeros.Length && joc.numeros[i] != null)
            {
                if (i != n && !avisFalten)
                {
                    avisFalten = true;
                    Debug.LogWarning("Falten prefabs per a alguns números (4-8): s'ha fet servir el més alt que hi ha.");
                }
                return joc.numeros[i];
            }
        }
        return null;
    }
#endif
}