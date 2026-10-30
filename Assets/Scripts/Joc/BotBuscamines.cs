using System.Collections.Generic;
using UnityEngine;

// Rival controlat per l'ordinador (mode "Contra el bot"). Juga els torns del jugador 2.
// Nomes fa servir el que veu un jugador (numeros destapats i banderes):
//  1. Si un numero ja te totes les seves mines marcades, les altres caselles del voltant son segures -> les destapa.
//  2. Si un numero te tantes caselles tapades com mines li falten, totes son mines -> hi posa bandera.
//  3. Si no pot deduir res, s'arrisca amb una casella a l'atzar.
// Camina fins a prop de la casella abans de picar-la, perque es vegi que juga.
public class BotBuscamines : MonoBehaviour
{
    public JocBuscamines joc;
    public PlayerController avatar;
    public int jugadorBot = 1;
    public float velocitat = 4.5f;
    [Tooltip("Segons que pensa entre accio i accio (minim, maxim)")]
    public Vector2 tempsPensar = new Vector2(0.7f, 1.4f);
    [Tooltip("Distancia a la que s'atura per picar una casella")]
    public float distanciaPicar = 3f;

    Vector2Int objectiu;
    bool teObjectiu, objectiuDestapar;
    float espera;

    void Update()
    {
        if (joc == null || avatar == null) return;
        bool elMeuTorn = joc.EstatActual == JocBuscamines.Estat.Jugant && joc.JugadorActual == jugadorBot && !PauseMenuController.EstaPausat;
        if (!elMeuTorn)
        {
            teObjectiu = false;
            espera = Random.Range(tempsPensar.x, tempsPensar.y) + 0.6f;   // una mica mes al principi del torn
            return;
        }

        if (!teObjectiu)
        {
            avatar.PosarCaminant(false);
            espera -= Time.deltaTime;
            if (espera > 0f) return;
            teObjectiu = Triar(out objectiu, out objectiuDestapar);
            if (!teObjectiu) return;
        }

        // Si mentrestant la casella ja s'ha destapat (cascada), en busca una altra
        if (joc.EsOberta(objectiu) || (objectiuDestapar && joc.TeBandera(objectiu))) { teObjectiu = false; return; }

        Bounds b;
        if (!joc.LimitsCasella(objectiu, out b)) { teObjectiu = false; return; }
        Vector3 dest = b.center;
        Vector3 pos = avatar.transform.position;
        Vector3 d = dest - pos;
        d.y = 0f;
        if (d.magnitude > distanciaPicar)
        {
            Caminar(d.normalized);
            return;
        }

        // Ha arribat: pica i pensa la seguent
        avatar.PosarCaminant(false);
        avatar.MirarCapAra(dest);
        joc.ExecutarAccio(objectiuDestapar, objectiu.y, objectiu.x);
        teObjectiu = false;
        espera = Random.Range(tempsPensar.x, tempsPensar.y);
    }

    void Caminar(Vector3 dir)
    {
        Transform t = avatar.transform;
        Vector3 p = t.position + dir * velocitat * Time.deltaTime;
        p.y = Terra(p, joc.LimitsTaulell.max.y);
        t.position = p;
        t.rotation = Quaternion.RotateTowards(t.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
        avatar.PosarCaminant(true);
    }

    // Alcada de la casella sota el bot (ignora els jugadors)
    float Terra(Vector3 p, float dalt)
    {
        RaycastHit[] cops = Physics.RaycastAll(new Vector3(p.x, dalt + 5f, p.z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        float millor = float.NegativeInfinity;
        foreach (RaycastHit c in cops)
        {
            if (c.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (c.point.y > millor) millor = c.point.y;
        }
        return float.IsNegativeInfinity(millor) ? avatar.transform.position.y : millor;
    }

    // ---------- Decidir que fer ----------
    bool Triar(out Vector2Int p, out bool destapar)
    {
        int F = joc.FilesActuals, C = joc.ColumnesActuals;
        List<Vector2Int> segures = new List<Vector2Int>(), mines = new List<Vector2Int>(), tapades = new List<Vector2Int>();

        for (int f = 0; f < F; f++)
            for (int c = 0; c < C; c++)
            {
                int n = joc.NumeroVisible(f, c);
                if (n == -1)
                {
                    if (!joc.TeBandera(new Vector2Int(c, f))) tapades.Add(new Vector2Int(c, f));
                    continue;
                }
                if (n <= 0) continue;

                int banderes = 0;
                List<Vector2Int> veinesTapades = new List<Vector2Int>();
                for (int df = -1; df <= 1; df++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int ff = f + df, cc = c + dc;
                        if ((df == 0 && dc == 0) || ff < 0 || ff >= F || cc < 0 || cc >= C) continue;
                        if (joc.NumeroVisible(ff, cc) != -1) continue;
                        Vector2Int v = new Vector2Int(cc, ff);
                        if (joc.TeBandera(v)) banderes++;
                        else veinesTapades.Add(v);
                    }
                if (veinesTapades.Count == 0) continue;
                if (banderes == n) segures.AddRange(veinesTapades);
                else if (n - banderes == veinesTapades.Count) mines.AddRange(veinesTapades);
            }

        destapar = true;
        if (segures.Count > 0) { p = MesProper(segures); return true; }
        if (mines.Count > 0) { p = MesProper(mines); destapar = false; return true; }
        if (tapades.Count > 0) { p = tapades[Random.Range(0, tapades.Count)]; return true; }
        p = Vector2Int.zero;
        return false;
    }

    Vector2Int MesProper(List<Vector2Int> llista)
    {
        Vector2Int millor = llista[0];
        float dMillor = float.MaxValue;
        foreach (Vector2Int v in llista)
        {
            Bounds b;
            if (!joc.LimitsCasella(v, out b)) continue;
            float d = (b.center - avatar.transform.position).sqrMagnitude;
            if (d < dMillor) { dMillor = d; millor = v; }
        }
        return millor;
    }
}
