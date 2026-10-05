using UnityEngine;

public static class JsonLoader
{
    public static T LoadJson<T>(string path)
    {
        TextAsset json = Resources.Load<TextAsset>(path);
        return JsonUtility.FromJson<T>(json.text);
    }
}