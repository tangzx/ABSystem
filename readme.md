# 特点
* 指定要打包的文件，程序会自动分析依赖、拆分打包粒度到最优方式打包
* 缓存上次的打包信息，下次打包会增量打包
* 自动管理卸载不用的AB
* 可扩展，自定义打包和加载路径

# 如何运行
1. 通过`ABSystem/Builder Panel`打开配置界面
2. 在配置界面中更改打包配置
3. 点击`Build`执行打包
4. 增加宏 `AB_MODE`
5. 运行测试

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
        GameObject go = a.Instantiate(); //自动管理：当go被Destroy时，AB会被释放回收
    });

    manager.Load("Assets.my_txture.png.ab", (a) =>
    {
    	// a.Retain();	//强制引用计数加一
    	// a.Release();	//引用计数减一
    	Texture tex = a.Require(this); //自动管理：当this被Destroy时，AB会被释放回收
    });
}

```