using UnityEngine;
using Tangzx.ABSystem;

public class Test : MonoBehaviour
{
    AssetBundleManager manager;

    void Start()
    {
        manager = gameObject.AddComponent<AssetBundleManager>();
        manager.Init(() =>
        {
            LoadObjects();
        });
    }

    void LoadObjects()
    {
        manager.Load("Assets.Prefabs.Sphere.prefab", (a) =>
        {
            GameObject go = Instantiate(a.mainObject) as GameObject;//a.Instantiate();
            go.transform.localPosition = new Vector3(3, 3, 3);
        });
        //manager.Load("Assets.Prefabs.Cube.prefab.ab", (a) =>
        //{
        //    GameObject go = a.Instantiate();
        //    go.transform.localPosition = new Vector3(6, 3, 3);
        //});
        //manager.Load("Assets.Prefabs.Plane.prefab.ab", (a) =>
        //{
        //    GameObject go = a.Instantiate();
        //    go.transform.localPosition = new Vector3(9, 3, 3);
        //});
        //manager.Load("Assets.Prefabs.Capsule.prefab.ab", (a) =>
        //{
        //    GameObject go = a.Instantiate();
        //    go.transform.localPosition = new Vector3(12, 3, 3);
        //});
    }
}