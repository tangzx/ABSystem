# 特点
* 指定要打包的文件，程序会自动分析依赖、拆分打包粒度到最优方式打包
* 缓存上次的打包信息，下次打包会增量打包
* 自动管理卸载不用的AB
* 可扩展，自定义打包和加载路径

# 如何运行
1. 先执行菜单 `Tang / Build AssetBundles` 打包
2. 增加宏 `AB_MODE`
3. 运行测试

# 如何使用
```c#

void Start()
{
	AssetBundleManager manager = AssetBundleManager.Instance;
	manager.Init(() =>
    {
        LoadObjects();
    });
}

void LoadObjects()
{
    manager.Load("Assets.Prefabs.Sphere.prefab.ab", (a) =>
    {
        GameObject go = Instantiate(a.mainObject) as GameObject;
    });
}

```