using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    public static bool TryFindComponent<T>(out T component) where T : UnityEngine.Object
    {
        component = UnityEngine.Object.FindAnyObjectByType<T>();

        if (component == null)
        {
            Debug.LogError($"{typeof(T).Name} not found in scene!");
            return false;
        }

        return true;
    }

    public static bool TryLoadResource<T>(out T resource, string path) where T : UnityEngine.Object
    {
        resource = Resources.Load<T>(path);
        if (resource == null)
        {
            Debug.LogError($"[ResourceUtil] Failed to load resource at path: {path} (Type: {typeof(T).Name})");
            return false;
        }

        return true;
    }

    public static bool TryFindGameObject(out GameObject gameObject, string name)
    {
        gameObject = GameObject.Find(name);
        if (gameObject == null)
        {
            Debug.LogError($"[GameObjectUtil] GameObject not found: {name}");
            return false;
        }
        return true;
    }

    public static bool TryGetComponent<T>(out T component, GameObject gameObject) where T : Component
    {
        component = gameObject.GetComponent<T>();
        if (component == null)
        {
            Debug.LogError($"[GameObjectUtil] Missing component of type {typeof(T).Name} on {gameObject.name}");
            return false;
        }
        return true;
    }

    // 오버로드: 이름으로 바로 RectTransform 찾기
    public static bool TryFindComponent<T>(out T component, string gameObjectName) where T : Component
    {
        component = null;
        if (!TryFindGameObject(out GameObject go, gameObjectName))
            return false;

        return TryGetComponent(out component, go);
    }

    // List<T> 범위 지정 셔플
    public static void Shuffle<T>(this List<T> list, int startIndex, int endIndex, System.Random random)
    {
        for (int i = endIndex - 1; i > startIndex; i--)
        {
            int randIdx = random.Next(startIndex, i + 1);

            T value = list[randIdx];
            list[randIdx] = list[i];
            list[i] = value;
        }
    }

    // List<T> 전체 셔플
    public static void Shuffle<T>(this List<T> list, System.Random random = null)
    {
        if (random == null)
            random = new System.Random();

        list.Shuffle(0, list.Count, random);
    }

    // Array(T[]) 범위 지정 셔플
    public static void Shuffle<T>(this T[] array, int startIndex, int endIndex, System.Random random = null)
    {
        if (random == null)
            random = new System.Random();

        for (int i = endIndex - 1; i > startIndex; i--)
        {
            int randIdx = random.Next(startIndex, i + 1);

            T value = array[randIdx];
            array[randIdx] = array[i];
            array[i] = value;
        }
    }

    // Array(T[]) 전체 셔플
    public static void Shuffle<T>(this T[] array, System.Random random = null)
    {
        if (random == null)
            random = new System.Random();

        array.Shuffle(0, array.Length, random);
    }

    /// <summary>
    /// 특정 조건이 true가 될 때까지 코루틴으로 대기 후 onComplete 호출
    /// </summary>
    public static IEnumerator WaitUntilTrue(Func<bool> condition, Action onComplete)
    {
        while (!condition())
        {
            yield return null; // 매 프레임 대기
        }

        onComplete?.Invoke();
    }
}

