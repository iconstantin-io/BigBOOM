using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelPrincipal;
    public GameObject panelPersonajes;
    public GameObject panelConfiguracion;

    [Header("Audio Settings")]
    public Slider volumeSlider;

    [Header("Scene Config")]
    public string nombreEscenaJuego = "EscenaMapa";

    public static int personajeSeleccionado = 1;

    void Start()
    {
        MostrarPanelPrincipal();

        // Inicialitzar el Slider de volum amb el volum actual del joc
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    // --- NAVEGACIÓ DE PANELLS ---

    public void MostrarPanelPrincipal()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelPersonajes != null) panelPersonajes.SetActive(false);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
    }

    public void AbrirSeleccionPersonaje()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelPersonajes != null) panelPersonajes.SetActive(true);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
    }

    public void AbrirConfiguracion()
    {
        if (panelPrincipal != null) panelPrincipal.SetActive(false);
        if (panelPersonajes != null) panelPersonajes.SetActive(false);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(true);
    }

    // --- ACCIONS DE JOC I CONFIGURACIÓ ---

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void SeleccionarPersonajeYJugar(int idPersonaje)
    {
        personajeSeleccionado = idPersonaje;
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void Jugar()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}