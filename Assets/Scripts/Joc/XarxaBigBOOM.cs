using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Connexio entre dos ordinadors per jugar online (TCP, port 7777).
// Un crea la partida (host, jugador 1) i l'altre s'hi uneix amb la IP (client, jugador 2).
// Els missatges son linies de text "TIPUS|dada|dada...". Es llegeixen en un fil apart i es processen al fil principal.
// Sobreviu als canvis d'escena (DontDestroyOnLoad): es crea al menu i es fa servir a la partida.
public class XarxaBigBOOM : MonoBehaviour
{
    public const int Port = 7777;
    public const string Versio = "1";

    public static XarxaBigBOOM Instancia { get; private set; }
    public static bool Connectat { get { return Instancia != null && Instancia.connectat; } }

    public bool EsHost { get; private set; }
    public bool RivalLlest { get; set; }          // el client ja te l'escena de joc carregada

    public event Action OnConnectat;
    public event Action<string[]> OnMissatge;
    public event Action<string> OnDesconnectat;

    TcpListener escolta;
    TcpClient client;
    StreamWriter escriptor;
    readonly object bloqueig = new object();
    readonly ConcurrentQueue<string> cua = new ConcurrentQueue<string>();
    volatile bool connectat, tancant;

    public static XarxaBigBOOM Crear()
    {
        Destruir();
        GameObject g = new GameObject("Xarxa_BigBOOM");
        DontDestroyOnLoad(g);
        Instancia = g.AddComponent<XarxaBigBOOM>();
        Application.runInBackground = true;        // que el joc segueixi quan la finestra no te el focus
        return Instancia;
    }

    public static void Destruir()
    {
        if (Instancia == null) return;
        Instancia.Tancar();
        Destroy(Instancia.gameObject);
        Instancia = null;
    }

    // ---------- Host ----------
    public void Allotjar()
    {
        EsHost = true;
        escolta = new TcpListener(IPAddress.Any, Port);
        escolta.Start();      // pot llancar excepcio si el port ja s'esta fent servir
        Thread fil = new Thread(() =>
        {
            try
            {
                TcpClient c = escolta.AcceptTcpClient();
                escolta.Stop();
                Preparar(c);
                cua.Enqueue("_CONNECTAT");
                Llegir();
            }
            catch (Exception e) { if (!tancant) cua.Enqueue("_ERROR|" + e.Message); }
        });
        fil.IsBackground = true;
        fil.Start();
    }

    // ---------- Client ----------
    public void Connectar(string ip)
    {
        EsHost = false;
        Thread fil = new Thread(() =>
        {
            try
            {
                TcpClient c = new TcpClient();
                IAsyncResult r = c.BeginConnect(ip, Port, null, null);
                if (!r.AsyncWaitHandle.WaitOne(6000)) { c.Close(); throw new Exception("No hi ha resposta de " + ip); }
                c.EndConnect(r);
                Preparar(c);
                cua.Enqueue("_CONNECTAT");
                Llegir();
            }
            catch (Exception e) { if (!tancant) cua.Enqueue("_ERROR|" + e.Message); }
        });
        fil.IsBackground = true;
        fil.Start();
    }

    void Preparar(TcpClient c)
    {
        client = c;
        c.NoDelay = true;
        escriptor = new StreamWriter(c.GetStream(), new UTF8Encoding(false));
        escriptor.AutoFlush = true;
    }

    void Llegir()
    {
        StreamReader lector = new StreamReader(client.GetStream(), new UTF8Encoding(false));
        string linia;
        try
        {
            while (!tancant && (linia = lector.ReadLine()) != null) cua.Enqueue(linia);
        }
        catch (Exception) { }
        if (!tancant) cua.Enqueue("_DESCONNECTAT");
    }

    // Envia un missatge. Els numeros decimals sempre amb punt (independent de l'idioma de l'ordinador)
    public void Enviar(params object[] parts)
    {
        if (escriptor == null || !connectat) return;
        string[] s = new string[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            object p = parts[i];
            if (p is float) s[i] = ((float)p).ToString("0.###", CultureInfo.InvariantCulture);
            else s[i] = Convert.ToString(p, CultureInfo.InvariantCulture).Replace("|", "/").Replace("\n", " ");
        }
        try { lock (bloqueig) escriptor.WriteLine(string.Join("|", s)); }
        catch (Exception) { cua.Enqueue("_DESCONNECTAT"); }
    }

    void Update()
    {
        string linia;
        while (cua.TryDequeue(out linia))
        {
            if (linia == "_CONNECTAT")
            {
                connectat = true;
                if (OnConnectat != null) OnConnectat();
            }
            else if (linia.StartsWith("_ERROR") || linia == "_DESCONNECTAT")
            {
                bool estava = connectat;
                Tancar();
                string motiu = linia == "_DESCONNECTAT" ? "El rival s'ha desconnectat" : (estava ? "Connexió perduda" : "No s'ha pogut connectar: " + linia.Substring(Mathf.Min(7, linia.Length)));
                if (OnDesconnectat != null) OnDesconnectat(motiu);
                return;
            }
            else
            {
                string[] p = linia.Split('|');
                if (p[0] == "LLEST") RivalLlest = true;
                if (OnMissatge != null) OnMissatge(p);
            }
        }
    }

    public void Tancar()
    {
        tancant = true;
        connectat = false;
        try { if (escriptor != null) escriptor.Dispose(); } catch (Exception) { }
        try { if (client != null) client.Close(); } catch (Exception) { }
        try { if (escolta != null) escolta.Stop(); } catch (Exception) { }
        escriptor = null;
    }

    void OnDestroy()
    {
        Tancar();
        if (Instancia == this) Instancia = null;
    }

    // ---------- Utilitats ----------
    public static float F(string s) { return float.Parse(s, CultureInfo.InvariantCulture); }
    public static int I(string s) { return int.Parse(s, CultureInfo.InvariantCulture); }

    // IPs d'aquest ordinador (per dir-les a l'altre jugador)
    public static List<string> IPsLocals()
    {
        List<string> ips = new List<string>();
        try
        {
            foreach (IPAddress a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)) ips.Add(a.ToString());
        }
        catch (Exception) { }
        if (ips.Count == 0) ips.Add("127.0.0.1");
        return ips;
    }
}
