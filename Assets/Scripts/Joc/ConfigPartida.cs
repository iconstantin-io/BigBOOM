using UnityEngine;

// Opcions que el menu principal passa a l'escena de joc (nivell) i preferencies guardades (volum, sensibilitat).
public static class ConfigPartida
{
    public struct Nivell
    {
        public string nom;
        public int columnes, files, mines;
        public Nivell(string nom, int columnes, int files, int mines)
        {
            this.nom = nom; this.columnes = columnes; this.files = files; this.mines = mines;
        }
    }

    // Mateixes mides que els menus del GeneradorTaulell (FACIL, NORMAL, DIFICIL).
    // Tots es centren sobre la mateixa zona de l'illa; els petits deixen espai buit al voltant.
    public static readonly Nivell[] Nivells =
    {
        new Nivell("Principiant", 9, 9, 10),
        new Nivell("Intermedi", 16, 16, 40),
        new Nivell("Expert", 30, 16, 99),
    };

    // Fals si s'obre EscenaMapa directament des de l'editor: llavors el JocBuscamines fa servir els valors de l'inspector
    public static bool Definida { get; private set; }
    public static Nivell NivellActual { get; private set; } = Nivells[1];

    public static int IndexNivell { get; private set; } = 1;

    public static void TriarNivell(int i)
    {
        IndexNivell = Mathf.Clamp(i, 0, Nivells.Length - 1);
        NivellActual = Nivells[IndexNivell];
        Definida = true;
    }

    // ---------- Mode de joc ----------
    public enum ModeJoc { Local, Bot, OnlineHost, OnlineClient }
    public static ModeJoc Mode { get; set; }
    public static bool EsOnline { get { return Mode == ModeJoc.OnlineHost || Mode == ModeJoc.OnlineClient; } }

    // Noms dels dos jugadors d'aquesta partida (null = els de l'inspector del Joc)
    public static string[] NomsPartida { get; set; }

    // Si el jugador j el controla algu des d'aquest ordinador (amb teclat i ratoli)
    public static bool JugadorLocal(int j)
    {
        switch (Mode)
        {
            case ModeJoc.Bot: return j == 0;
            case ModeJoc.OnlineHost: return j == 0;
            case ModeJoc.OnlineClient: return j == 1;
            default: return true;
        }
    }

    // Nom del jugador 1 (es demana en obrir el joc i es guarda)
    const string ClauNom = "BigBOOM_NomJugador1";
    public static bool NomDemanat { get; set; }
    public static string NomJugador1
    {
        get { return PlayerPrefs.GetString(ClauNom, ""); }
        set { PlayerPrefs.SetString(ClauNom, value); PlayerPrefs.Save(); }
    }

    const string ClauVolum = "BigBOOM_Volum";
    const string ClauSensibilitat = "BigBOOM_Sensibilitat";
    static float sensibilitat = 1f;

    public static float Volum
    {
        get { return AudioListener.volume; }
        set { AudioListener.volume = value; PlayerPrefs.SetFloat(ClauVolum, value); }
    }

    public static float Sensibilitat
    {
        get { return sensibilitat; }
        set { sensibilitat = value; PlayerPrefs.SetFloat(ClauSensibilitat, value); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Carregar()
    {
        Definida = false;
        NomDemanat = false;
        Mode = ModeJoc.Local;
        NomsPartida = null;
        NivellActual = Nivells[1];
        AudioListener.volume = PlayerPrefs.GetFloat(ClauVolum, 1f);
        sensibilitat = PlayerPrefs.GetFloat(ClauSensibilitat, 1f);
    }
}
