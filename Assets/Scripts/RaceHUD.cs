using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD básico. Arrastra dos Text (UI > Legacy > Text) de un Canvas.
/// debugText es opcional: sirve para comprobar que el progreso funciona.
/// </summary>
public class RaceHUD : MonoBehaviour
{
    public Text positionText;
    public Text debugText;

    void Update()
    {
        RaceTracker tracker = RaceTracker.Instance;
        if (tracker == null || tracker.Player == null) return;

        RacerProgress player = tracker.Player;

        if (positionText != null)
            positionText.text = $"Posición {player.Position} / {tracker.AliveCount}";

        if (debugText != null && player.route != null)
            debugText.text = $"Ruta: {player.Distance:0} / {player.route.TotalLength:0} m";
    }
}
