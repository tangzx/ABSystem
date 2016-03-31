using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundleBuildPanel : EditorWindow
{
    [MenuItem("Tang/AssetBundleConfigPanel")]
    static void Open()
    {
        GetWindow<AssetBundleBuildPanel>();
    }

    [MenuItem("Tang/Build AssetBundles")]
    static void BuildAssetBundles()
    {
        AssetBundleBuildConfig config = AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(savePath);
        if (config == null)
            return;

#if UNITY_5
        ABBuilder builder = new AssetBundleBuilder5x(new AssetBundlePathResolver());
#else
        ABBuilder builder = new AssetBundleBuilder4x(new AssetBundlePathResolver());
#endif
        builder.SetDataWriter(config.depInfoFileFormat == AssetBundleBuildConfig.Format.Text ? new AssetBundleDataWriter() : new AssetBundleDataBinaryWriter());

        builder.Begin();

        for (int i = 0; i < config.filters.Count; i++)
        {
            AssetBundleFilter f = config.filters[i];
            if (f.valid)
                builder.AddRootTargets(new DirectoryInfo(f.path), new string[] { f.filter });
        }

        builder.Export();
        builder.End();
    }

    class Styles
    {
        public static GUIStyle box;
        public static GUIStyle toolbar;
        public static GUIStyle toolbarButton;
        public static GUIStyle tooltip;
    }

    const string savePath = "Assets/ABSystem/config.asset";

    AssetBundleBuildConfig config;

    AssetBundleBuildPanel()
    {

    }

    void UpdateStyles()
    {
        Styles.box = new GUIStyle(GUI.skin.box);
        Styles.box.margin = new RectOffset();
        Styles.box.padding = new RectOffset();
        Styles.toolbar = new GUIStyle(EditorStyles.toolbar);
        Styles.toolbar.margin = new RectOffset();
        Styles.toolbar.padding = new RectOffset();
        Styles.toolbarButton = EditorStyles.toolbarButton;
        Styles.tooltip = GUI.skin.GetStyle("AssetLabel");
    }

    void OnGUI()
    {
        if (config == null)
        {
            config = AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(savePath);
            if (config == null)
            {
                config = new AssetBundleBuildConfig();
            }
        }

        UpdateStyles();
        //tool bar
        GUILayout.BeginHorizontal(Styles.toolbar);
        {
            if (GUILayout.Button("Add", Styles.toolbarButton))
            {
                config.filters.Add(new AssetBundleFilter());
            }
            if (GUILayout.Button("Save", Styles.toolbarButton))
            {
                Save();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Build", Styles.toolbarButton))
            {
                Build();
            }
        }
        GUILayout.EndHorizontal();

        //context
        GUILayout.BeginVertical();

		//format
		GUILayout.BeginHorizontal();
		{
			EditorGUILayout.PrefixLabel("DepInfoFileFormat");
			config.depInfoFileFormat = (AssetBundleBuildConfig.Format)EditorGUILayout.EnumPopup(config.depInfoFileFormat);
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(10);

        for (int i = 0; i < config.filters.Count; i++)
        {
            AssetBundleFilter filter = config.filters[i];
            GUILayout.BeginHorizontal();
            {
                filter.valid = GUILayout.Toggle(filter.valid, "valid", GUILayout.ExpandWidth(false));
                filter.path = GUILayout.TextField(filter.path, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
                {
                    string dataPath = Application.dataPath;
                    string selectedPath = EditorUtility.OpenFolderPanel("Path", dataPath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        if (selectedPath.StartsWith(dataPath))
                        {
                            filter.path = "Assets/" + selectedPath.Substring(dataPath.Length + 1);
                        }
                        else
                        {
                            ShowNotification(new GUIContent("不能在Assets目录之外!"));
                        }
                    }
                }
                filter.filter = GUILayout.TextField(filter.filter, GUILayout.Width(200));
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                {
                    config.filters.RemoveAt(i);
                    i--;
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        //set dirty
        if (GUI.changed)
            EditorUtility.SetDirty(config);
    }

    private void Build()
    {
        Save();
        BuildAssetBundles();
    }

    void Save()
    {
        AssetBundlePathResolver pathResolver = new AssetBundlePathResolver();

        if (AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(savePath) == null)
        {
            AssetDatabase.CreateAsset(config, savePath);
        }
        else
        {
            EditorUtility.SetDirty(config);
        }
    }
}