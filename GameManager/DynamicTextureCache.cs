using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using GameManager;

namespace Zhsan.GameManager;

/// <summary>
/// 實時動態材質緩存，向舊版 Live/Scene/Page/Temp 分級機制靠齊。
/// 繪製區間內只登記請求（下一楨生效），實際加載發生在 Update 階段的 Drain 或非繪製期的同步調用。
/// </summary>
internal static class DynamicTextureCache
{
    private static readonly object SyncRoot = new();
    // 主組：按 Session.TextureRecs 的 CacheType 標籤分級清理（Live/Scene/Page/Temp）。
    private static readonly Dictionary<ImageRequest, Texture2D> Textures = new();
    // Temp 組：任何級別的清理都無條件釋放（對齊舊版 TextureTempDics）。
    private static readonly Dictionary<ImageRequest, Texture2D> TempTextures = new();
    // 繪製期登記、Update 期排空的待加載請求。
    private static readonly Dictionary<ImageRequest, bool> Pending = new();
    // 加載失敗的鍵，避免每楨重試；清理時允許再次嘗試。
    private static readonly HashSet<ImageRequest> Failed = new();

    internal static int Count
    {
        get { lock (SyncRoot) return Textures.Count + TempTextures.Count; }
    }

    /// <summary>查詢已加載的材質；繪製期與 Update 期都可安全調用。</summary>
    internal static bool TryGet(ImageRequest request,
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out Texture2D texture)
    {
        lock (SyncRoot)
        {
            if (TempTextures.TryGetValue(request, out texture) && !texture.IsDisposed) return true;
            if (Textures.TryGetValue(request, out texture) && !texture.IsDisposed) return true;
            texture = null;
            return false;
        }
    }

    /// <summary>繪製期登記請求；實際加載推遲到 Update 期的 Drain。</summary>
    internal static void Request(ImageRequest request, bool isTemp)
    {
        lock (SyncRoot)
        {
            if (Failed.Contains(request)) return;
            Pending.TryAdd(request, isTemp);
        }
    }

    /// <summary>繪製區間外同步加載（舊版實時語義），供 Update/LoadContent 使用。</summary>
    internal static Texture2D? LoadNow(ImageRequest request, bool isTemp)
    {
        lock (SyncRoot)
        {
            if (TempTextures.TryGetValue(request, out var existing) && !existing.IsDisposed) return existing;
            if (Textures.TryGetValue(request, out existing) && !existing.IsDisposed) return existing;
            return Load(request, isTemp);
        }
    }

    /// <summary>排空繪製期登記的請求；必須在繪製區間外（Update 階段）調用。</summary>
    internal static void Drain()
    {
        KeyValuePair<ImageRequest, bool>[] batch;
        lock (SyncRoot)
        {
            if (Pending.Count == 0) return;
            batch = Pending.ToArray();
            Pending.Clear();
        }
        foreach (var pair in batch)
        {
            lock (SyncRoot) Load(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// 按 Live/Scene/Page/Temp 分級釋放；任何級別的清理都先清空 Temp 組與待加載隊列。
    /// 空標籤視為用戶材質，不主動清理（對齊舊版行為）。
    /// </summary>
    internal static void Clear(CacheType type)
    {
        lock (SyncRoot)
        {
            foreach (var texture in TempTextures.Values)
            {
                if (texture != null && !texture.IsDisposed) texture.Dispose();
            }
            TempTextures.Clear();

            Pending.Clear();
            Failed.Clear();

            foreach (var key in Textures.Keys.ToArray())
            {
                var texture = Textures[key];
                if (ShouldClear(type, CacheTypeOf(key)))
                {
                    if (texture != null && !texture.IsDisposed) texture.Dispose();
                    Textures.Remove(key);
                }
                else if (texture == null || texture.IsDisposed)
                {
                    // 去除已失效的材質
                    Textures.Remove(key);
                }
            }
        }
    }

    /// <summary>
    /// 按名稱釋放材質（主組與 Temp 組都匹配），並清除對應的失敗與待加載記錄。
    /// 用於舊版 Remove/RemoveTempDics 語義：文件被下載或更新後，丟棄舊緩存以便重新加載。
    /// </summary>
    internal static void RemoveByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (SyncRoot)
        {
            RemoveMatching(Textures, name);
            RemoveMatching(TempTextures, name);
            // 允許重新加載（例如頭像下載完成後重試）。
            Failed.RemoveWhere(request => request.Name == name);
            foreach (var key in Pending.Keys.ToArray())
            {
                if (key.Name == name) Pending.Remove(key);
            }
        }
    }

    private static void RemoveMatching(Dictionary<ImageRequest, Texture2D> group, string name)
    {
        foreach (var key in group.Keys.ToArray())
        {
            if (key.Name != name) continue;
            var texture = group[key];
            if (texture != null && !texture.IsDisposed) texture.Dispose();
            group.Remove(key);
        }
    }

    /// <summary>全部釋放；僅在應用退出時調用。</summary>
    internal static void Dispose()
    {
        lock (SyncRoot)
        {
            foreach (var texture in Textures.Values)
            {
                if (texture != null && !texture.IsDisposed) texture.Dispose();
            }
            Textures.Clear();
            foreach (var texture in TempTextures.Values)
            {
                if (texture != null && !texture.IsDisposed) texture.Dispose();
            }
            TempTextures.Clear();
            Pending.Clear();
            Failed.Clear();
        }
    }

    private static Texture2D? Load(ImageRequest request, bool isTemp)
    {
        if (Failed.Contains(request)) return null;
        try
        {
            var texture = PageResourceStore.LoadImage(request);
            if (isTemp) TempTextures[request] = texture;
            else Textures[request] = texture;
            return texture;
        }
        catch (Exception error)
        {
            Failed.Add(request);
            MigrationCheck.Log("dynamic-texture", $"Failed to load {request}: {error.Message}");
            return null;
        }
    }

    private static string CacheTypeOf(ImageRequest request)
    {
        // 用戶材質沒有元數據標籤，視為不可分級清理。
        if (request.User) return "";
        foreach (var pair in Session.TextureRecs)
        {
            if (pair.Key.Split('#')[0] == request.Name)
                return pair.Value.CacheType ?? "";
        }
        return "";
    }

    private static bool ShouldClear(CacheType type, string cacheType)
    {
        return type switch
        {
            CacheType.Live => cacheType is "Live" or "Scene" or "Page" or "Temp",
            CacheType.Scene => cacheType is "Scene" or "Page" or "Temp",
            CacheType.Page => cacheType is "Page" or "Temp",
            CacheType.Temp => cacheType == "Temp",
            _ => false
        };
    }
}
