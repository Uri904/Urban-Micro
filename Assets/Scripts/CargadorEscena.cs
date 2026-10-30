using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class CargadorEscena : MonoBehaviour
{
    public GameObject panelCargando;
    public TMP_Text textoProgreso;
    public float tiempoMinimo = 1.5f;

    public void Cargar(string nombreEscena)
    {
        StartCoroutine(CargarAsync(nombreEscena));
    }

IEnumerator CargarAsync(string nombreEscena)
{
    if (panelCargando != null) panelCargando.SetActive(true);

    Application.backgroundLoadingPriority = ThreadPriority.Low;
    yield return null;

    AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscena);
    op.allowSceneActivation = false;

    float tiempo = 0f;
    while (op.progress < 0.9f || tiempo < tiempoMinimo)
    {
        tiempo += Time.deltaTime;

        if (textoProgreso != null)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            textoProgreso.text = "Cargando... " + Mathf.RoundToInt(p * 100) + "%";
        }
        yield return null;
    }

    if (textoProgreso != null) textoProgreso.text = "Casi listo...";
    yield return new WaitForSecondsRealtime(0.5f);

    op.allowSceneActivation = true;
}

    public void Salir()
    {
        Application.Quit();
    }
}