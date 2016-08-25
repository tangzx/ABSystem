using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Tangzx.ABSystem
{
    public class AssetBundleBuildPanel : EditorWindow
    {
        [MenuItem("ABSystem/Builder Panel")]
        static void Open()
        {
            GetWindow<AssetBundleBuildPanel>("ABSystem", true);
        }

        [MenuItem("ABSystem/Builde AssetBundles")]
        static void BuildAssetBundles()
        {
            AssetBundleBuildConfig config = LoadAssetAtPath<AssetBundleBuildConfig>(savePath);

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

		static T LoadAssetAtPath<T>(string path) where T:Object
		{
#if UNITY_5
			return AssetDatabase.LoadAssetAtPath<T>(savePath);
#else
			return (T)AssetDatabase.LoadAssetAtPath(savePath, typeof(T));
#endif
		}

        const string savePath = "Assets/ABSystem/config.asset";

        private AssetBundleBuildConfig _config;
        private ReorderableList _list;

        AssetBundleBuildPanel()
        {

        }

        void OnListElementGUI(Rect rect, int index, bool isactive, bool isfocused)
        {
            const float GAP = 5;

            AssetBundleFilter filter = _config.filters[index];
            rect.y++;

            Rect r = rect;
            r.width = 16;
            r.height = 18;
            filter.valid = GUI.Toggle(r, filter.valid, GUIContent.none);

            r.xMin = r.xMax + GAP;
            r.xMax = rect.xMax - 300;
            GUI.enabled = false;
            filter.path = GUI.TextField(r, filter.path);
            GUI.enabled = true;

            r.xMin = r.xMax + GAP;
            r.width = 50;
            if (GUI.Button(r, "Select"))
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

            r.xMin = r.xMax + GAP;
            r.xMax = rect.xMax;
            filter.filter = GUI.TextField(r, filter.filter);
        }

        void OnListHeaderGUI(Rect rect)
        {
            EditorGUI.LabelField(rect, "Asset Filter");
        }

        void OnGUI()
        {
            bool execBuild = false;
            if (_config == null)
            {
                _config = LoadAssetAtPath<AssetBundleBuildConfig>(savePath);
                if (_config == null)
                {
                    _config = new AssetBundleBuildConfig();
                }
            }

            if (_list == null)
            {
                _list = new ReorderableList(_config.filters, typeof(AssetBundleFilter));
                _list.drawElementCallback = OnListElementGUI;
                _list.drawHeaderCallback = OnListHeaderGUI;
                _list.draggable = true;
                _list.elementHeight = 22;
            }

            //tool bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Add", EditorStyles.toolbarButton))
                {
                    _config.filters.Add(new AssetBundleFilter());
                }
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    Save();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Build", EditorStyles.toolbarButton))
                {
                    execBuild = true;
                }
            }
            GUILayout.EndHorizontal();

            //context
            GUILayout.BeginVertical();

            //format
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("DepInfoFileFormat");
                _config.depInfoFileFormat = (AssetBundleBuildConfig.Format)EditorGUILayout.EnumPopup(_config.depInfoFileFormat);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            
            _list.DoLayoutList();
            GUILayout.EndVertical();

            //set dirty
            if (GUI.changed)
                EditorUtility.SetDirty(_config);

            if (execBuild)
                Build();
        }

        private void Build()
        {
            Save();
            BuildAssetBundles();
        }

        void Save()
        {
            AssetBundlePathResolver.instance = new AssetBundlePathResolver();

            if (LoadAssetAtPath<AssetBundleBuildConfig>(savePath) == null)
            {
                AssetDatabase.CreateAsset(_config, savePath);
            }
            else
            {
                EditorUtility.SetDirty(_config);
            }
        }
    }
}