using System.Collections;
using UnityEngine;

public class CoroutineManager : MonoBehaviour
{
    public static bool Create(out CoroutineManager outMgr)
    {
        var go = new GameObject("CoroutineManager");
        outMgr = go.AddComponent<CoroutineManager>();

        DontDestroyOnLoad(go);

        return true;
    }

    public Coroutine Run(IEnumerator coroutine)
    {
        return StartCoroutine(coroutine);
    }
    public void Stop(IEnumerator coroutine)
    {
        StopCoroutine(coroutine);
    }
}
