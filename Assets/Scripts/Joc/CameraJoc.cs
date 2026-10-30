using UnityEngine;
using UnityEngine.InputSystem;

// Camera de la partida: vista aeria del tauler o seguiment en 3a persona del jugador que juga.
// Els canvis de mode son suaus i permet sacsejades (explosions). Va a la Main Camera.
public class CameraJoc : MonoBehaviour
{
    public enum Mode { Aeria, Seguir }

    [Header("Seguiment (3a persona)")]
    public float distancia = 5.5f;
    public float alcada = 1.8f;
    public float desplacamentLateral = 0.5f;
    public float inclinacioInicial = 24f;
    public Vector2 limitsInclinacio = new Vector2(-15f, 70f);
    [Tooltip("Graus per pixel de moviment del ratoli (es multiplica per la sensibilitat de les opcions)")]
    public float sensibilitat = 0.12f;

    [Header("Suavitat")]
    public float velocitatSeguir = 16f;
    public float velocitatAeria = 2.2f;
    public float duradaTransicio = 1.2f;

    public Mode ModeActual { get; private set; }

    Transform objectiu;
    float fovBase = 60f;
    Vector3 posAeria, posActual;
    Quaternion rotAeria, rotActual;
    float inclinacio, gir, transicio = 1f;
    float sacsejada, duradaSacsejada, tSacsejada;
    bool iniciat;

    // Camp de visio de la camera (el de l'escena)
    public float FovBase { get { return fovBase; } }

    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null) fovBase = cam.fieldOfView;
    }

    public void VistaAeria(Vector3 pos, Vector3 mirarA)
    {
        VistaAeria(pos, Quaternion.LookRotation(mirarA - pos));
    }

    public void VistaAeria(Vector3 pos, Quaternion rot)
    {
        posAeria = pos;
        rotAeria = rot;
        if (ModeActual != Mode.Aeria) transicio = 0f;
        ModeActual = Mode.Aeria;
    }

    public void Seguir(Transform t)
    {
        if (ModeActual != Mode.Seguir || objectiu != t)
        {
            transicio = 0f;
            inclinacio = inclinacioInicial;
            gir = t.eulerAngles.y;      // comenca darrere del personatge
        }
        objectiu = t;
        ModeActual = Mode.Seguir;
    }

    // Col·loca la camera al seu lloc sense animacio
    public void SaltarTransicio()
    {
        Vector3 p; Quaternion r;
        CalcularObjectiu(out p, out r);
        posActual = p;
        rotActual = r;
        transicio = 1f;
        iniciat = true;
        transform.SetPositionAndRotation(p, r);
    }

    public void Sacsejar(float intensitat, float durada)
    {
        float restant = duradaSacsejada > 0f ? sacsejada * Mathf.Clamp01(1f - tSacsejada / duradaSacsejada) : 0f;
        sacsejada = Mathf.Max(restant, intensitat);
        duradaSacsejada = Mathf.Max(0.05f, durada);
        tSacsejada = 0f;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (!iniciat)
        {
            posActual = transform.position;
            rotActual = transform.rotation;
            iniciat = true;
        }

        if (ModeActual == Mode.Seguir && objectiu != null && Cursor.lockState == CursorLockMode.Locked && dt > 0f)
        {
            Mouse m = Mouse.current;
            if (m != null)
            {
                // El ratoli fa orbitar la camera; el personatge es gira cap on camina (PlayerController)
                Vector2 d = m.delta.ReadValue() * sensibilitat * ConfigPartida.Sensibilitat;
                gir += d.x;
                inclinacio = Mathf.Clamp(inclinacio - d.y, limitsInclinacio.x, limitsInclinacio.y);
            }
        }

        Vector3 posObj; Quaternion rotObj;
        CalcularObjectiu(out posObj, out rotObj);

        transicio = Mathf.MoveTowards(transicio, 1f, dt / duradaTransicio);
        float vel = ModeActual == Mode.Seguir ? Mathf.Lerp(velocitatAeria, velocitatSeguir, transicio * transicio) : velocitatAeria;        float k = 1f - Mathf.Exp(-vel * dt);
        posActual = Vector3.Lerp(posActual, posObj, k);
        rotActual = Quaternion.Slerp(rotActual, rotObj, ModeActual == Mode.Seguir && transicio >= 1f ? 1f : k);

        Vector3 desp = Vector3.zero;
        if (tSacsejada < duradaSacsejada)
        {
            tSacsejada += dt;
            float f = 1f - Mathf.Clamp01(tSacsejada / duradaSacsejada);
            desp = Random.insideUnitSphere * sacsejada * f * f;
        }
        transform.SetPositionAndRotation(posActual + desp, rotActual);
    }

    void CalcularObjectiu(out Vector3 pos, out Quaternion rot)
    {
        if (ModeActual != Mode.Seguir || objectiu == null)
        {
            pos = posAeria;
            rot = rotAeria;
            return;
        }

        rot = Quaternion.Euler(inclinacio, gir, 0f);
        Vector3 pivot = objectiu.position + Vector3.up * alcada + Quaternion.Euler(0f, gir, 0f) * Vector3.right * desplacamentLateral;
        Vector3 dir = rot * Vector3.back;

        // Si hi ha alguna cosa entre el jugador i la camera, s'apropa
        float dist = distancia;
        RaycastHit[] cops = Physics.SphereCastAll(pivot, 0.25f, dir, distancia, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit c in cops)
        {
            if (c.collider.transform.IsChildOf(objectiu) || c.distance <= 0f) continue;
            if (c.distance < dist) dist = c.distance;
        }
        pos = pivot + dir * Mathf.Max(0.8f, dist - 0.1f);
    }
}
