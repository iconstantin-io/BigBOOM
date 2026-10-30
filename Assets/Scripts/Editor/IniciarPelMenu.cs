using UnityEditor;
using UnityEditor.SceneManagement;

// Menu BigBOOM > Començar sempre pel menu
// Si esta marcat (per defecte), en donar a Play a l'editor el joc comenca per la primera escena del Build (MainMenu),
// encara que tinguis oberta EscenaMapa. En una build ja comenca sempre pel menu.
[InitializeOnLoad]
static class IniciarPelMenu
{
    const string Clau = "BigBOOM_IniciarPelMenu";
    const string RutaMenu = "BigBOOM/Començar sempre pel menú";

    static IniciarPelMenu()
    {
        EditorApplication.delayCall += Aplicar;
    }

    static bool Actiu { get { return EditorPrefs.GetBool(Clau, true); } }

    [MenuItem(RutaMenu, false, 0)]
    static void Canviar()
    {
        EditorPrefs.SetBool(Clau, !Actiu);
        Aplicar();
    }

    [MenuItem(RutaMenu, true)]
    static bool Validar()
    {
        Menu.SetChecked(RutaMenu, Actiu);
        return true;
    }

    static void Aplicar()
    {
        SceneAsset primera = null;
        if (Actiu)
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.enabled)
                {
                    primera = AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path);
                    break;
                }
        EditorSceneManager.playModeStartScene = primera;
    }
}
