using UnityEngine;

public class Performance : MonoBehaviour
{
    void Awake()
    {
        Application.runInBackground = true;   // don’t pause when unfocused
        QualitySettings.vSyncCount = 0;       // no VSync cap
        Application.targetFrameRate = -1;     // unlimited
    }
}
