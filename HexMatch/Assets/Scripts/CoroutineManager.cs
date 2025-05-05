using System.Collections;
using UnityEngine;

public class CoroutineManager : MonoBehaviour
{
    public static void Destroy(ref CoroutineManager coroutineMgr)
    {
        coroutineMgr?.Destroy();
        coroutineMgr = null;
    }

    private void Destroy()
    {
        if (this != null)
            Destroy(this.gameObject);
    }

    public static bool Create(out CoroutineManager outMgr)
    {
        GameObject go = new();
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
