using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tangzx.ABSystem
{
    public enum BuildPhase
    {
        Analyze1,
        Analyze2,
        Merge1,
        Merge2,
        BeforeExport1,
        BeforeExport2
    }

    public class ABBuilder : IAssetBundleBuilder
    {
        protected AssetBundleDataWriter dataWriter = new AssetBundleDataBinaryWriter();
        protected AssetBundlePathResolver pathResolver;

        private List<AssetBundleEntry> createdEntries = new List<AssetBundleEntry>();

        public ABBuilder() : this(new AssetBundlePathResolver())
        {

        }

        public ABBuilder(AssetBundlePathResolver resolver)
        {
            this.pathResolver = resolver;
            this.InitDirs();
            AssetBundleUtils.pathResolver = pathResolver;
        }

        void InitDirs()
        {
            new DirectoryInfo(pathResolver.BundleSavePath).Create();
            new FileInfo(pathResolver.HashCacheSaveFile).Directory.Create();
        }

        public void Begin()
        {
            EditorUtility.DisplayProgressBar("Loading", "Loading...", 0.1f);
            AssetBundleUtils.Init();
        }

        public void End()
        {
            AssetBundleUtils.SaveCache();
            AssetBundleUtils.ClearCache();
            EditorUtility.ClearProgressBar();
        }

        public List<AssetBundleEntry> GetAll()
        {
            var list = new List<AssetBundleEntry>();
            var targets = AssetBundleUtils.GetAll();
            for (int i = 0; i < targets.Count; i++)
            {
                list.Add(targets[i]);
            }
            list.AddRange(createdEntries);
            return list;
        }

        public virtual void Analyze()
        {
            var all = GetAll();
            processModifiers(BuildPhase.Analyze1);
            foreach (AssetTarget target in all)
            {
                target.Analyze();
            }
            processModifiers(BuildPhase.Analyze2);

            all = GetAll();
            processModifiers(BuildPhase.Merge1);
            foreach (AssetBundleEntry target in all)
            {
                target.Merge();
            }
            processModifiers(BuildPhase.Merge2);

            all = GetAll();
            processModifiers(BuildPhase.BeforeExport1);
            foreach (AssetBundleEntry target in all)
            {
                target.BeforeExport();
            }
            processModifiers(BuildPhase.BeforeExport2);
        }

        public virtual void Export()
        {
            this.Analyze();
        }

        public void AddRootTargets(DirectoryInfo bundleDir, string[] partterns = null, SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (partterns == null)
                partterns = new string[] { "*.*" };
            for (int i = 0; i < partterns.Length; i++)
            {
                FileInfo[] prefabs = bundleDir.GetFiles(partterns[i], searchOption);
                foreach (FileInfo file in prefabs)
                {
                    AssetTarget target = AssetBundleUtils.Load(file);
                    target.exportType = AssetBundleExportType.Root;
                }
            }
        }

        protected void SaveDepAll(List<AssetBundleEntry> all)
        {
            string path = Path.Combine(pathResolver.BundleSavePath, pathResolver.DependFileName);

            if (File.Exists(path))
                File.Delete(path);

            List<AssetBundleEntry> exportList = new List<AssetBundleEntry>();
            for (int i = 0; i < all.Count; i++)
            {
                AssetBundleEntry target = all[i];
                if (target.needSelfExport)
                    exportList.Add(target);
            }
            AssetBundleDataWriter writer = dataWriter;
            writer.Save(path, exportList.ToArray());
        }

        public void SetDataWriter(AssetBundleDataWriter w)
        {
            this.dataWriter = w;
        }

        /// <summary>
        /// 删除未使用的AB，可能是上次打包出来的，而这一次没生成的
        /// </summary>
        /// <param name="all"></param>
        protected void RemoveUnused(List<AssetBundleEntry> all)
        {
            HashSet<string> usedSet = new HashSet<string>();
            for (int i = 0; i < all.Count; i++)
            {
                AssetBundleEntry target = all[i];
                if (target.needSelfExport)
                    usedSet.Add(target.bundleName);
            }

            DirectoryInfo di = new DirectoryInfo(pathResolver.BundleSavePath);
            FileInfo[] abFiles = di.GetFiles("*.ab");
            for (int i = 0; i < abFiles.Length; i++)
            {
                FileInfo fi = abFiles[i];
                if (usedSet.Add(fi.Name))
                {
                    Debug.Log("Remove unused AB : " + fi.Name);

                    fi.Delete();
                    //for U5X
                    File.Delete(fi.FullName + ".manifest");
                }
            }
        }

        void processModifiers(BuildPhase phase)
        {
            Type et = typeof(ABBuilder);
            Type[] list = et.Assembly.GetTypes();
            for (int i = 0; i < list.Length; i++)
            {
                Type t = list[i];
                if (!t.IsAbstract && typeof(IAssetBundleEntryModifier).IsAssignableFrom(t))
                {
                    IAssetBundleEntryModifier n = (IAssetBundleEntryModifier)Activator.CreateInstance(t);
                    n.process(this, phase);
                }
            }
        }

        AssetBundlePack IAssetBundleBuilder.createFakeEntry()
        {
            AssetBundlePack abe = new AssetBundlePack();
            createdEntries.Add(abe);
            return abe;
        }
    }
}