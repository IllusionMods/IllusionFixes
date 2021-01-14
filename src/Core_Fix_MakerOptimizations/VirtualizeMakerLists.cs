using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
using Illusion.Extensions;
using IllusionUtility.SetUtility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IllusionFixes
{
    public partial class MakerOptimizations
    {
        internal static class VirtualizeMakerLists
        {
            internal static void InstallHooks()
            {
                var h = Harmony.CreateAndPatchAll(typeof(VirtualizeMakerLists));

                // Moreaccs overrides one of the methods that we patch with its own patch, so the patch has to be patched as well
                var moraccs = Type.GetType("MoreAccessoriesKOI.CustomAcsSelectKind_OnSelect_Patches, MoreAccessories");
                if (moraccs != null)
                {
                    var target = moraccs.GetMethod("Prefix", AccessTools.all);
                    Logger.LogDebug("Found MoreAccessories, patching: " + target?.FullDescription());
                    var fix = new HarmonyMethod(typeof(VirtualizeMakerLists), nameof(OnSelectFix));
                    h.Patch(target, transpiler: fix);
                }
            }

            private sealed class VirtualListData
            {
                public readonly int ItemsInRow;
                public readonly int ItemHeight;

                public List<CustomSelectInfoComponent> ItemCache { get; }
                public List<CustomSelectInfo> ItemList { get; }
                public RectTransform Content { get; }
                public GridLayoutGroup LayoutGroup { get; }
                private readonly CustomSelectListCtrl _listCtrl;

                private bool IsVisible
                {
                    get
                    {
                        if (_listCtrl == null || _listCtrl.canvasGrp == null || _listCtrl.canvasGrp.Length == 0)
                            return false;

                        var canvasGroups = _listCtrl.canvasGrp;
                        for (var i = 0; i < canvasGroups.Length; i++)
                            if (!canvasGroups[i].interactable)
                                return false;

                        return true;
                    }
                }

                public float ScrollPositionY
                {
                    get => Content.localPosition.y;
                    set => Content.SetLocalPositionY(value);
                }

                private CustomSelectInfo _selectedItem;
                private bool _selectionChanged;
                public CustomSelectInfo SelectedItem
                {
                    get => _selectedItem;
                    set
                    {
                        if (_selectedItem != value)
                        {
                            _selectedItem = value;
                            _selectionChanged = true;
                        }
                    }
                }

                public Dictionary<CustomSelectInfo, Sprite> ThumbCache { get; } = new Dictionary<CustomSelectInfo, Sprite>();

                public int LastItemsAbove;
                public bool IsDirty;

                public VirtualListData(int itemsInRow, List<CustomSelectInfoComponent> listEntryCache,
                    RectTransform content, List<CustomSelectInfo> itemList, CustomSelectListCtrl listCtrl)
                {
                    ItemsInRow = itemsInRow;
                    ItemCache = listEntryCache ?? throw new ArgumentNullException(nameof(listEntryCache));
                    ItemList = itemList ?? throw new ArgumentNullException(nameof(itemList));
                    _listCtrl = listCtrl;

                    if (!content) throw new ArgumentNullException(nameof(content));
                    Content = content;
                    LayoutGroup = content.GetComponent<GridLayoutGroup>();

                    // Alternatively can be calculated by doing Mathf.RoundToInt(LayoutGroup.cellSize.x + LayoutGroup.spacing.x);
                    ItemHeight = 120;

                    InitialTopPadding = LayoutGroup.padding.top;
                    InitialBotPadding = LayoutGroup.padding.bottom;

                    IsDirty = true;
                }

                public readonly int InitialBotPadding;
                public readonly int InitialTopPadding;

                public void UpdateSelection()
                {
                    ToggleAllOff();
                    if (SelectedItem != null)
                    {
                        if (SelectedItem.sic != null)
                            SelectedItem.sic.tgl.isOn = true;

                        if (_selectionChanged && IsVisible) // Only update the scroll after the list is fully loaded and shown, or it will get reset to 0
                        {
                            if (ScrollListsToSelection.Value)
                                ScrollToSelection();
                            _selectionChanged = false;
                        }
                    }
                }

                /// <summary>
                /// Scroll the list to show currently selected item (if necessary)
                /// </summary>
                public void ScrollToSelection()
                {
                    var itemRow = GetItemVisibleRow(SelectedItem);

                    if (itemRow >= 0)
                    {
                        var minScroll = (itemRow - 4f) * ItemHeight;
                        var maxScroll = (itemRow + 0.5f) * ItemHeight;
                        var targetScroll = itemRow * ItemHeight;
                        //Console.WriteLine($"pos={ScrollPositionY} row={itemRow} min={minScroll} max={maxScroll} target={targetScroll}");
                        if (ScrollPositionY < minScroll || ScrollPositionY > maxScroll)
                            ScrollPositionY = Mathf.Max(0, targetScroll - ItemHeight * 1.7f);
                    }
                }

                /// <summary>
                /// Get row of the item as it is seen in the list
                /// (ignores hidden items)
                /// </summary>
                private int GetItemVisibleRow(CustomSelectInfo item)
                {
                    var i = GetItemVisibleIndex(item);
                    return i < 0 ? -1 : i / ItemsInRow;
                }

                /// <summary>
                /// Get item index in the list as it is seen
                /// (ignores hidden items so it's not an index in the list)
                /// </summary>
                private int GetItemVisibleIndex(CustomSelectInfo item)
                {
                    var count = 0;
                    for (var index = 0; index < ItemList.Count; index++)
                    {
                        var x = ItemList[index];
                        if (!x.disvisible)
                        {
                            if (x == item) return count;
                            count++;
                        }
                    }
                    return -1;
                }

                public Sprite GetThumbSprite(CustomSelectInfo item)
                {
                    if (!ThumbCache.TryGetValue(item, out var thumb) || thumb == null)
                    {
                        var thumbTex = CommonLib.LoadAsset<Texture2D>(item.assetBundle, item.assetName, false, string.Empty);
                        if (thumbTex)
                        {
                            thumb = Sprite.Create(thumbTex, new Rect(0f, 0f, thumbTex.width, thumbTex.height), new Vector2(0.5f, 0.5f));
                            ThumbCache[item] = thumb;
                        }
                    }

                    return thumb;
                }

                public void ToggleAllOff()
                {
                    ItemCache.ForEach(x => x.tgl.isOn = false);
                }

                public static bool IsItemNew(CustomSelectInfo item)
                {
                    return !DisableNewIndicator.Value && Singleton<Manager.Character>.Instance.chaListCtrl.CheckItemID(item.category, item.index) == 1;
                }

                public static void MarkItemAsNotNew(CustomSelectInfo customSelectInfo)
                {
                    Singleton<Manager.Character>.Instance.chaListCtrl.AddItemID(customSelectInfo.category, customSelectInfo.index, 2);
                }
            }

            private static readonly Dictionary<CustomSelectListCtrl, VirtualListData> _listCache = new Dictionary<CustomSelectListCtrl, VirtualListData>();

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), "Update")]
            private static void ListUpdate(CustomSelectListCtrl __instance)
            {
                if (!_listCache.TryGetValue(__instance, out var listData)) return;

                var scrollPosition = listData.ScrollPositionY;
                // How many items are not visible in current view
                var visibleItemCount = listData.ItemList.Count(x => !x.disvisible);
                var offscreenItemCount = Mathf.Max(0, visibleItemCount - listData.ItemCache.Count);
                // How many items are above current view rect and not visible
                var rowsAboveViewRect = Mathf.FloorToInt(Mathf.Clamp(scrollPosition / listData.ItemHeight, 0, offscreenItemCount));
                var itemsAboveViewRect = rowsAboveViewRect * listData.ItemsInRow;

                if (listData.LastItemsAbove == itemsAboveViewRect && !listData.IsDirty) return;

                listData.LastItemsAbove = itemsAboveViewRect;
                listData.IsDirty = false;

                // Store selected item to preserve selection when moving the list with mouse
                var selectedItem = listData.ItemList.Find(x => x.sic != null && x.sic.gameObject == EventSystem.current.currentSelectedGameObject);

                listData.ItemList.ForEach(x => x.sic = null);
                // Apply visible list items to actual cache items
                var count = 0;
                foreach (var item in listData.ItemList.Where(x => !x.disvisible).Skip(itemsAboveViewRect))
                {
                    if (count >= listData.ItemCache.Count) break;

                    var cachedEntry = listData.ItemCache[count];

                    count++;

                    cachedEntry.info = item;
                    item.sic = cachedEntry;

                    cachedEntry.Disable(item.disable);

                    cachedEntry.objNew.SetActiveIfDifferent(VirtualListData.IsItemNew(item));

                    var thumb = listData.GetThumbSprite(item);
                    cachedEntry.img.sprite = thumb;

                    if (ReferenceEquals(selectedItem, item))
                        EventSystem.current.SetSelectedGameObject(cachedEntry.gameObject);

                    cachedEntry.gameObject.SetActiveIfDifferent(true);
                }

                // Disable unused cache items
                if (count < listData.ItemCache.Count)
                {
                    foreach (var cacheEntry in listData.ItemCache.Skip(count))
                        cacheEntry.gameObject.SetActiveIfDifferent(false);
                }

                listData.UpdateSelection();

                // Apply top and bottom offsets to create the illusion of having all of the list items
                var topOffset = Mathf.RoundToInt(rowsAboveViewRect * listData.ItemHeight);
                listData.LayoutGroup.padding.top = listData.InitialTopPadding + topOffset;

                var totalHeight = Mathf.CeilToInt((float)visibleItemCount / listData.ItemsInRow) * listData.ItemHeight;
                var cacheEntriesHeight = Mathf.CeilToInt((float)listData.ItemCache.Count / listData.ItemsInRow) * listData.ItemHeight;
                var trailingHeight = totalHeight - cacheEntriesHeight - topOffset;
                listData.LayoutGroup.padding.bottom = Mathf.FloorToInt(Mathf.Max(0, trailingHeight) + listData.InitialBotPadding);

                // Needed after changing padding since it doesn't make the object dirty
                LayoutRebuilder.MarkLayoutForRebuild(listData.Content);
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.Create))]
            private static bool ListCreate(CustomSelectListCtrl __instance, CustomSelectListCtrl.OnChangeItemFunc _onChangeItemFunc,
                List<CustomSelectInfo> ___lstSelectInfo, GameObject ___objContent, GameObject ___objTemp)
            {
                __instance.onChangeItemFunc = _onChangeItemFunc;

                // Remove old list items if recreating the list
                foreach (Transform c in ___objContent.transform) Destroy(c.gameObject);

                var toggleGroup = ___objContent.GetComponent<ToggleGroup>();
                toggleGroup.allowSwitchOff = true;

                // Figure out how many items fit in the window at most
                // Need to use this specific RT because it's the most reliable
                var windowRt = __instance.GetComponent<RectTransform>();

                if (ListWidth.Value != 3) windowRt.sizeDelta += new Vector2((ListWidth.Value - 3) * 120, 0);

                var itemsInRow = ((int)windowRt.rect.width - 33) / 120;
                var itemsInColumn = ((int)windowRt.rect.height - 105) / 120 + 2;
                var totalVisibleItems = itemsInRow * itemsInColumn;

                // Create a cache of list items for the virtual list
                var spawnedItems = new List<CustomSelectInfoComponent>();
                var setHandlerMethod = Traverse.Create(__instance).Method("SetToggleHandler", new[] { typeof(GameObject) });
                for (int i = 0; i < totalVisibleItems; i++)
                {
                    var copy = Instantiate(___objTemp, ___objContent.transform, false);
                    var copyInfoComp = copy.GetComponent<CustomSelectInfoComponent>();

                    copyInfoComp.tgl.group = toggleGroup;
                    copyInfoComp.tgl.isOn = false;

                    setHandlerMethod.GetValue(copy);

                    copyInfoComp.img = copy.GetComponent<Image>();

                    var newTr = copy.transform.Find("New");
                    if (newTr) copyInfoComp.objNew = newTr.gameObject;

                    spawnedItems.Add(copyInfoComp);
                    copy.SetActive(false);
                }

                __instance.imgRaycast = spawnedItems.Select(x => x.img).ToArray();

                _listCache[__instance] = new VirtualListData(itemsInRow, spawnedItems, ___objContent.GetComponent<RectTransform>(), ___lstSelectInfo, __instance);

                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.SelectItem), typeof(int))]
            private static bool ListSelectItem(CustomSelectListCtrl __instance, int index)
            {
                if (_listCache.TryGetValue(__instance, out var listData))
                {
                    var itemInfo = listData.ItemList.Find(item => item.index == index);
                    if (itemInfo != null)
                    {
                        ChangeItem(__instance, itemInfo, listData);
                        listData.UpdateSelection();
                    }
                }

                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.SelectItem), typeof(string))]
            private static bool ListSelectItem(CustomSelectListCtrl __instance, string name)
            {
                if (_listCache.TryGetValue(__instance, out var listData))
                {
                    var itemInfo = listData.ItemList.Find(item => item.name == name);
                    if (itemInfo != null)
                    {
                        ChangeItem(__instance, itemInfo, listData);
                        listData.UpdateSelection();
                    }
                }

                return false;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.ChangeItem), typeof(GameObject))]
            private static void ListChangeItem(CustomSelectListCtrl __instance, GameObject obj)
            {
                // Capture which item is selected
                if (_listCache.TryGetValue(__instance, out var listData))
                {
                    var itemInfo = listData.ItemList.Find(item => item.sic != null && item.sic.gameObject == obj);
                    listData.SelectedItem = itemInfo;
                }
            }

            private static void ChangeItem(CustomSelectListCtrl __instance, CustomSelectInfo itemInfo, VirtualListData listData)
            {
                // Calling original whenever possible is probably better for interop since any hooks will run
                if (itemInfo.sic != null)
                {
                    __instance.ChangeItem(itemInfo.sic.gameObject);
                    return;
                }

                __instance.onChangeItemFunc?.Invoke(itemInfo.index);

                var tv = new Traverse(__instance);
                tv.Field<string>("selectDrawName").Value = itemInfo.name;
#if KK
                var tmp = tv.Field<TMPro.TextMeshProUGUI>("textDrawName").Value;
                if (tmp) tmp.text = itemInfo.name;
#endif

                if (VirtualListData.IsItemNew(itemInfo))
                {
                    VirtualListData.MarkItemAsNotNew(itemInfo);
                    MarkListDirty(__instance);
                }

                listData.SelectedItem = itemInfo;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.UpdateStateNew))]
            private static bool ListUpdateStateNew(CustomSelectListCtrl __instance)
            {
                // Handled automatically when updating the list
                MarkListDirty(__instance);
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.ToggleAllOff))]
            private static bool ListToggleAllOff(CustomSelectListCtrl __instance)
            {
                if (_listCache.TryGetValue(__instance, out var listData)) listData.ToggleAllOff();
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.SelectNextItem))]
            private static bool ListSelectNextItem(CustomSelectListCtrl __instance)
            {
                if (_listCache.TryGetValue(__instance, out var listData))
                {
                    var i = listData.ItemList.IndexOf(listData.SelectedItem);
                    var nextItem = listData.ItemList.Skip(i + 1).FirstOrDefault(x => !x.disable && !x.disvisible);
                    if (nextItem != null) __instance.SelectItem(nextItem.index);
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.SelectPrevItem))]
            private static bool ListSelectPrevItem(CustomSelectListCtrl __instance)
            {
                if (_listCache.TryGetValue(__instance, out var listData))
                {
                    var i = listData.ItemList.IndexOf(listData.SelectedItem);
                    if (i < 0) i = 0;
                    var nextItem = listData.ItemList.Take(i).Reverse().FirstOrDefault(x => !x.disable && !x.disvisible);
                    if (nextItem != null) __instance.SelectItem(nextItem.index);
                }
                return false;
            }

            // Instead of trying to get the thumb sprite from the list item that might not exist now, get the sprite from our cache instead
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CustomSelectKind), nameof(CustomSelectKind.OnSelect))]
            [HarmonyPatch(typeof(CustomAcsSelectKind), nameof(CustomAcsSelectKind.OnSelect))]
            private static IEnumerable<CodeInstruction> OnSelectFix(IEnumerable<CodeInstruction> instructions)
            {
                var getsprm = AccessTools.Property(typeof(Image), nameof(Image.sprite)).GetGetMethod() ?? throw new MemberNotFoundException("Image.sprite");
                var replacem = AccessTools.Method(typeof(VirtualizeMakerLists), nameof(GetThumbSpriteHook)) ?? throw new MemberNotFoundException("GetThumbSpriteHook");

                return new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(OpCodes.Callvirt, getsprm))
                    .Repeat(cm => cm
                        .SetAndAdvance(OpCodes.Ldarg_0, null)
                        .SetAndAdvance(OpCodes.Call, replacem)
                        .SetAndAdvance(OpCodes.Nop, null))
                    .Instructions();
            }

            // Instead of calling the disable methods on the sic which can be null now, mark the list as dirty so it gets updated as normal
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.DisableItem), typeof(string), typeof(bool))]
            [HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.DisableItem), typeof(int), typeof(bool))]
            [HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.DisvisibleItem), typeof(string), typeof(bool))]
            [HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.DisvisibleItem), typeof(int), typeof(bool))]
            private static IEnumerable<CodeInstruction> OnDisableFix(IEnumerable<CodeInstruction> instructions)
            {
                var sicFld = AccessTools.Field(typeof(CustomSelectInfo), "sic") ?? throw new MemberNotFoundException("CustomSelectInfo.sic");
                var replacem = AccessTools.Method(typeof(VirtualizeMakerLists), nameof(MarkListDirty)) ?? throw new MemberNotFoundException("TriggerDirtyHook");

                return new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldloc_1),
                        new CodeMatch(OpCodes.Ldfld, sicFld),
                        new CodeMatch(OpCodes.Ldarg_2),
                        new CodeMatch(OpCodes.Callvirt))
                    .SetAndAdvance(OpCodes.Ldarg_0, null)
                    .SetAndAdvance(OpCodes.Call, replacem)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Nop, null)
                    .Instructions();
            }

            private static void MarkListDirty(CustomSelectListCtrl list)
            {
                if (_listCache.TryGetValue(list, out var listData)) listData.IsDirty = true;
            }

            private static Sprite GetThumbSpriteHook(CustomSelectInfo item, MonoBehaviour csk)
            {
                var list = Traverse.Create(csk).Field<CustomSelectListCtrl>("listCtrl").Value;
                _listCache.TryGetValue(list, out var listData);
                return listData?.GetThumbSprite(item);
            }
        }
    }
}