using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CargadorEscena : MonoBehaviour
{
    public void Cargar(string nombreEscena)
    {
        StartCoroutine(CargarAsync(nombreEscena));
    }

    IEnumerator CargarAsync(string nombreEscena)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscena);

        while (!op.isDone)
        {
            yield return null;
        }
    }
}