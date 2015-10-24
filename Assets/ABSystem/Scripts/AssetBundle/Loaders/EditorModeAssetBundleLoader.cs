#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using Uzen.AB;

/// <summary>
/// 编辑器模式下用的加载器
/// </summary>
public class EditorModeAssetBundleLoader : AssetBundleLoader
{
    class ABInfo : AssetBundleInfo
    {
        protected override Object mainObject
        {
            get
            {
                string newPath = AssetBundlePathResolver.instance.GetEditorModePath(path);
                Object mainObject = AssetDatabase.LoadMainAssetAtPath(newPath);
                return mainObject;
            }
        }
    }

    public override void Load()
    {
        if (bundleInfo == null)
        {
            this.state = LoadState.State_Complete;
            this.bundleInfo = bundleManager.CreateBundleInfo(this, new ABInfo());
            this.bundleInfo.isReady = true;
        }

        bundleManager.StartCoroutine(this.LoadResource());
    }

    IEnumerator LoadResource()
    {
        yield return new WaitForSeconds(0.1f);
        this.Complete();
    }
}
#endif