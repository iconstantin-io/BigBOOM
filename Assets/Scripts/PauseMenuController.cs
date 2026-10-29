using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panell de Pausa")]
    public GameObject panelPausa;

    private bool estaPausat = false;

    void Update()
    {
        // Si premes la tecla ESC, s'obre o es tanca la pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausat)
            {
                Continuar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        if (panelPausa != null) panelPausa.SetActive(true);
        Time.timeScale = 0f; // Congela el joc
        estaPausat = true;
    }

    public void Continuar()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        Time.timeScale = 1f; // Reprèn el joc
        estaPausat = false;
    }

    public void AnarAlMenuPrincipal()
    {
        Time.timeScale = 1f; // Torna a activar el temps abans de canviar d'escena
        SceneManager.LoadScene("MainMenu");
    }
}