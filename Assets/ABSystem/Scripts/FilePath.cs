using System.IO;
using UnityEngine;

public class FilePath
{
    private static string persistentDataPath;

    private static void Init()
    {
        if (persistentDataPath == null)
        {
            /*
#if !UNITY_EDITOR && UNITY_ANDROID
            AndroidJavaClass aj = new AndroidJavaClass("com.powergame.BLCX.Tools");
            persistentDataPath = aj.CallStatic<string>("getWritablePath");
            if (string.IsNullOrEmpty(persistentDataPath))
                persistentDataPath = "/mnt/sdcard";
#else
            persistentDataPath = Application.persistentDataPath;
#endif
            */
            persistentDataPath = Application.persistentDataPath;
            Debug.Log("persistentDataPath >> " + persistentDataPath);
        }
    }

    public static string GetStreamAssetsFile(string path, bool forWWW = true)
    {
        string filePath = null;
#if UNITY_EDITOR
        if (forWWW)
            filePath = string.Format("file://{0}/StreamingAssets/{1}", Application.dataPath, path);
        else
            filePath = string.Format("{0}/StreamingAssets/{1}", Application.dataPath, path);
#elif UNITY_ANDROID
        filePath = string.Format("jar:file://{0}!/assets/{1}", Application.dataPath, path);
#else
        filePath = string.Format("file://{0}/Raw/{1}", Application.dataPath, path);
#endif
        return filePath;
    }

    /// <summary>
    /// 可写路径
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="createDir">是否创建目录</param>
    /// <returns></returns>
    public static string GetWritablePath(string path, bool createDir = false, bool isFile = true)
    {
        if (persistentDataPath == null) Init();
        
        string filePath = persistentDataPath + "/" + path;

        if (createDir)
        {
            DirectoryInfo di = null;
            if (isFile)
            {
                FileInfo f = new FileInfo(filePath);
                di = f.Directory;
            }
            else
            {
                di = new DirectoryInfo(filePath);
            }

            if (di.Exists == false)
            {
                di.Create();
            }
        }
        return filePath;
    }

    public static string GetSysWritablePath(string path, bool createDir = true, bool isFile = true)
    {
        return GetWritablePath("__blcx__/" + path, createDir, isFile);
    }
}