using UnityEngine;

public class InGameUIGlobal : MonoBehaviour
{
    private static InGameUIGlobal instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}