using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace YFan.Utils
{
    /// <summary>
    /// 资产加载工具接口
    /// + 负责管理资产的加载、缓存和引用计数
    /// </summary>
    public interface IAssetUtil : IUtility
    {
        /// <summary>
        /// 初始化资产加载工具
        /// + 必须在使用资产加载功能前调用
        /// </summary>
        UniTask InitializeAsync();

        /// <summary>
        /// 异步加载资产
        /// + 加载完成后，资产引用计数加一
        /// + 使用方法：
        ///   - 调用 LoadAsync 方法加载资产
        ///   - 加载完成后，通过返回的 UniTask 获取资产实例
        /// </summary>
        UniTask<T> LoadAsync<T>(string key) where T : Object;

        /// <summary>
        /// 异步实例化资产
        /// + 实例化后，资产引用计数加一
        /// + 实例化完成后，资产引用计数减一
        /// + 使用方法：
        ///   - 调用 InstantiateAsync 方法实例化资产
        ///   - 实例化完成后，通过返回的 UniTask 获取实例化后的 GameObject
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, bool stayWorldPosition = false);

        /// <summary>
        /// 释放资产引用
        /// + 调用后引用计数减一
        /// + 引用计数为零时，资产将被释放
        /// </summary>
        void Release(string key);

        /// <summary>
        /// 获取资产引用计数
        /// </summary>
        int GetRefCount(string key);

        /// <summary>
        /// 转储资产缓存信息
        /// </summary>
        void DumpCacheInfo();
    }

    public class AssetUtil : IAssetUtil, IDisposable
    {
        #region 内部数据结构
        private class AssetCacheData
        {
            public string Key;
            public AsyncOperationHandle Handle;
            public int RefCount;
            public object Asset;
            public Type AssetType;
        }
        #endregion

        #region 字段

        // 资产缓存
        private readonly Dictionary<string, AssetCacheData> _assetCache = new Dictionary<string, AssetCacheData>();
        // 加载任务缓存
        private readonly Dictionary<string, UniTask<object>> _loadingTasks = new Dictionary<string, UniTask<object>>();
        private const string LogModule = "AssetUtil";

        #endregion

        #region 初始化与销毁

        public async UniTask InitializeAsync()
        {
            try
            {
                await Addressables.InitializeAsync();
                YLog.Info("Addressables Initialized", LogModule);
            }
            catch (Exception e)
            {
                YLog.Exception(e, LogModule);
            }
        }

        public void Dispose()
        {
            foreach (var kvp in _assetCache)
            {
                if (kvp.Value.Handle.IsValid())
                    Addressables.Release(kvp.Value.Handle);
            }
            _assetCache.Clear();
            _loadingTasks.Clear();
            YLog.Info("AssetUtil Disposed & Cleared", LogModule);
        }

        #endregion

        #region 核心加载逻辑

        public async UniTask<T> LoadAsync<T>(string key) where T : Object
        {
            if (string.IsNullOrEmpty(key))
            {
                YLog.Error("LoadAsync 失败: Key 为空", LogModule);
                return null;
            }

            // 检查缓存中是否已存在该资产，如果存在则直接返回
            if (_assetCache.TryGetValue(key, out var cacheData))
            {
                if (cacheData.Asset is T resultAsset)
                {
                    cacheData.RefCount++;
                    return resultAsset;
                }
                else
                {
                    YLog.Error($"Key '{key}' 类型不匹配。缓存:{cacheData.AssetType}, 请求:{typeof(T)}", LogModule);
                    return null;
                }
            }

            // 并发保护：检查是否正在加载中
            // 如果正在加载中，等待加载完成后递归调用自己去命中缓存，确保引用计数逻辑统一
            if (_loadingTasks.TryGetValue(key, out var loadingTask))
            {
                var result = await loadingTask;
                return await LoadAsync<T>(key);
            }

            // 开始新任务
            // 注意不直接 await taskSource，因为它会被转换消耗掉
            var taskSource = LoadInternalAsync<T>(key);

            // 将 Task 转为 object 并“保鲜” (Preserve)
            // 这样这个 objectTask 就可以被多次 await (被这里的逻辑 await，也被并发的其他逻辑 await)
            var objectTask = taskSource.ContinueWith(t => (object)t).Preserve();

            _loadingTasks.Add(key, objectTask);

            try
            {
                object resultObj = await objectTask;
                return resultObj as T;
            }
            catch (Exception e)
            {
                YLog.Exception(e, LogModule);
                // 异常时移除，防止死锁
                if (_loadingTasks.ContainsKey(key)) _loadingTasks.Remove(key);
                return null;
            }
        }

        /// <summary>
        /// 内部加载方法，负责实际的加载操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private async UniTask<T> LoadInternalAsync<T>(string key) where T : Object
        {
            T result = null;
            AsyncOperationHandle<T> handle = default;

            try
            {
                handle = Addressables.LoadAssetAsync<T>(key);
                result = await handle.ToUniTask();

                if (handle.Status == AsyncOperationStatus.Succeeded && result != null)
                {
                    var data = new AssetCacheData()
                    {
                        Key = key,
                        Handle = handle,
                        RefCount = 1,
                        Asset = result,
                        AssetType = typeof(T)
                    };

                    // 二次检查缓存（防止极其边缘的并发情况）
                    if (_assetCache.ContainsKey(key))
                    {
                        Addressables.Release(handle);
                        _assetCache[key].RefCount++;
                        result = _assetCache[key].Asset as T;
                    }
                    else
                    {
                        _assetCache.Add(key, data);
                    }
                }
                else
                {
                    YLog.Error($"Addressables 加载失败: {key}", LogModule);
                    if (handle.IsValid()) Addressables.Release(handle);
                }
            }
            catch (Exception e)
            {
                YLog.Exception(e, LogModule);
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
            finally
            {
                // 无论成功失败，移除 loading 标记
                if (_loadingTasks.ContainsKey(key))
                {
                    _loadingTasks.Remove(key);
                }
            }

            return result;
        }

        #endregion

        #region 实例化与辅助功能
        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, bool stayWorldPosition = false)
        {
            var prefab = await LoadAsync<GameObject>(key);
            if (prefab == null) return null;
            // 实例化不增加 Asset 的 RefCount，因为 Instantiate 是克隆
            // 但 LoadAsync 已经 +1 了，这是正确的，代表只要场景里有这个物体，AssetBundle 就不能卸载
            // 用户 Destroy 物体时，需要手动调用 Release(key)
            return Object.Instantiate(prefab, parent, stayWorldPosition);
        }

        public void Release(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (_assetCache.TryGetValue(key, out var data))
            {
                data.RefCount--;
                if (data.RefCount <= 0)
                {
                    if (data.Handle.IsValid()) Addressables.Release(data.Handle);
                    _assetCache.Remove(key);
                    YLog.Info($"内存移除: {key}", LogModule);
                }
            }
            else
            {
                YLog.Warn($"尝试释放未缓存的资源: {key}", LogModule);
            }
        }

        public int GetRefCount(string key)
        {
            if (_assetCache.TryGetValue(key, out var data)) return data.RefCount;
            return 0;
        }

        public void DumpCacheInfo()
        {
            YLog.Info($"Cache ({_assetCache.Count}) | Loading ({_loadingTasks.Count})", LogModule);
            foreach (var kvp in _assetCache)
            {
                YLog.Info($"Key: {kvp.Key} | Ref: {kvp.Value.RefCount}", LogModule);
            }
        }

        #endregion
    }
}
