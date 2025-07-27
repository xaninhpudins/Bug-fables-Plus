using HarmonyLib;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using BFPlus.Patches;
using static BFPlus.Extensions.MainManager_Ext;

namespace BFPlus.Extensions
{
    class PauseMenu_Ext : MonoBehaviour
    {
        public AreaItem[] areaItems;
        public MedalCategory[] medalCategories;
        public Sprite[] categoryIcons;
        public GameObject medalCategoryIcon = null;
        public int chooseMedalCategory = -1;
        public int presetId = -1;
        public SpriteRenderer[] presetIcons = new SpriteRenderer[3];
        public static PauseMenu_Ext Instance
        {
            get
            {
                if (MainManager.pausemenu.GetComponent<PauseMenu_Ext>() == null)
                {
                    return MainManager.pausemenu.gameObject.AddComponent<PauseMenu_Ext>();
                }
                return MainManager.pausemenu.GetComponent<PauseMenu_Ext>();
            }
        }

        static void CheckFastTextState()
        {
            int settingID = MainManager.settingsindex[MainManager.listvar[MainManager.instance.option]];
            if (settingID == (int)NewMenuText.TextSkip)
            {
                MainManager.pausemenu.SettingsToggleSound();
                MainManager_Ext.fastText = !MainManager_Ext.fastText;
                MainManager.pausemenu.UpdateText();
            }

            if (settingID == (int)NewMenuText.ShowResistance)
            {
                MainManager.pausemenu.SettingsToggleSound();
                MainManager_Ext.showResistance = !MainManager_Ext.showResistance;
                MainManager.pausemenu.UpdateText();
            }

            if (settingID == (int)NewMenuText.NewBattleThemes)
            {
                MainManager.pausemenu.SettingsToggleSound();
                MainManager_Ext.newBattleThemes = !MainManager_Ext.newBattleThemes;
                MainManager.pausemenu.UpdateText();
            }
        }

        public static void SetEnemyData()
        {
            MainManager.pausemenu.enemydata = MainManager_Ext.enemyData;
        }

        static void SetBigFableIcon(List<Transform> icons)
        {
            if (MainManager.instance.flags[(int)NewCode.BIGFABLE])
            {
                icons.Add(MainManager.NewUIObject("bigFable", MainManager.pausemenu.boxes[1].transform, new Vector3(2f, 4.5f, -0.1f), Vector3.one * 0.8f, MainManager.itemsprites[0,142]).transform);
            }

            if (MainManager.instance.flags[(int)NewCode.EVEN])
            {
                icons.Add(MainManager.NewUIObject("even", MainManager.pausemenu.boxes[1].transform, new Vector3(2f, 4.5f, -0.1f), Vector3.one * 0.8f, MainManager.itemsprites[0, 102]).transform);
            }

            if (MainManager.instance.flags[(int)NewCode.COMMAND])
            {
                icons.Add(MainManager.NewUIObject("command", MainManager.pausemenu.boxes[1].transform, new Vector3(2f, 4.5f, -0.1f), Vector3.one * 0.6f, MainManager.guisprites[13]).transform);
            }

            if (MainManager.instance.flags[(int)NewCode.SCAVENGE])
            {
                icons.Add(MainManager.NewUIObject("scavenge",  MainManager.pausemenu.boxes[1].transform, new Vector3(2f, 4.5f, -0.1f), Vector3.one * 0.5f, MainManager.guisprites[22]).transform);
            }
        }


        static bool CanDequip(int[] medal)
        {
            return !(medal[0] == (int)Medal.MPPlus && MainManager.instance.bp < 3);
        }

        static bool CanEquip(int[] medal)
        {
            return (medal[0] == (int)Medal.MPPlus && MainManager.instance.maxtp > 3) || medal[0] != (int)Medal.MPPlus;
        }

        public void SetupAreaItemData()
        {
            string[] allAreasDatas = MainManager_Ext.assetBundle.LoadAsset<TextAsset>("AreaItemsData").ToString().Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            areaItems = new AreaItem[allAreasDatas.Length];
            for(int i=0;i <allAreasDatas.Length; i++)
            {
                areaItems[i] = new AreaItem();
                string[] areaData = allAreasDatas[i].Split(new char[] { '{' });
                if (areaData[0].Length > 0 && areaData[0] != "")
                {
                    areaItems[i].crystalBerries = areaData[0].Split(',').Select(int.Parse).ToArray();
                }

                if (areaData[1].Length > 0 && areaData[1] != "")
                {
                    areaItems[i].discoveries = areaData[1].Split(',').Select(int.Parse).ToArray();
                }

                if (areaData[2].Length > 0 && areaData[2] != "")
                {
                    areaItems[i].loreBooks = areaData[2].Split(',').Select(int.Parse).ToArray();
                }
            }
        }

        public void SetupMedalCategoriesData()
        {
            string[] allMedalCategories = MainManager_Ext.assetBundle.LoadAsset<TextAsset>("MedalCategories").ToString().Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            medalCategories = new MedalCategory[allMedalCategories.Length];
            for (int i = 0; i < allMedalCategories.Length; i++)
            {
                medalCategories[i] = new MedalCategory();
                string[] categoryData = allMedalCategories[i].Split(new char[] { ';' });
                medalCategories[i].name = categoryData[0];
                medalCategories[i].iconId = int.Parse(categoryData[1]);

                var sizes = categoryData[2].Split(',').Select(float.Parse).ToArray();
                medalCategories[i].iconSize = new Vector2(sizes[0], sizes[1]);

                if (categoryData[3].Length > 0 && categoryData[3] != "")
                {
                    medalCategories[i].medals = categoryData[3].Split(',').Select(int.Parse).ToArray();
                }
            }

            if(categoryIcons == null)
            {
                categoryIcons = MainManager_Ext.assetBundle.LoadAssetWithSubAssets<Sprite>("medalIcons").OrderBy(s => int.Parse(s.name.Split('_')[1])).ToArray();
            }
        }

        public int[] GetObtainedCategories()
        {
            if (medalCategories == null)
                SetupMedalCategoriesData();

            List<int> obtainedCategories = new List<int>() { 0 };

            for (int i = 0; i< medalCategories.Length; i++)
            {
                for (int j = 0; j < MainManager.instance.badges.Count; j++)
                {
                    if (medalCategories[i].medals != null && medalCategories[i].medals.Contains(MainManager.instance.badges[j][0]))
                    {
                        obtainedCategories.Add(i);
                        break;
                    }
                }
            }
            return obtainedCategories.ToArray();
        }

        public int[] GetCategoryMedals()
        {
            if (MainManager.instance.badges.Count != 0)
            {
                MedalCategory category = medalCategories[MainManager.listvar[MainManager.instance.option]];
                List<int> list = new List<int>();
                int[][] array = MainManager.instance.badges.ToArray();

                for (int i = 0; i < array.Length; i++)
                {
                    if ((category.medals != null && category.medals.Contains(array[i][0])) || MainManager.listvar[MainManager.instance.option] == 0)
                    {
                        list.Add(i);
                    }
                }
                return list.ToArray();
            }
            return new int[0];
        }

        public void DestroyMedalCategoryIcon()
        {
            if (Instance.medalCategoryIcon != null)
            {
                Destroy(Instance.medalCategoryIcon);
            }
        }

        public IEnumerator SaveMedalPreset()
        {
            if (AnyMedalEquipped())
            {
                var preset = new MainManager_Ext.MedalPreset();

                for (int i = 0; i < MainManager.instance.badges.Count; i++)
                {
                    if (MainManager.instance.badges[i][1] != -2)
                    {
                        preset.medals.Add(new int[] { MainManager.instance.badges[i][0], MainManager.instance.badges[i][1] });
                    }
                }

                MainManager.instance.DestroyList();
                MainManager.pausemenu.gameObject.SetActive(false);

                MainManager.instance.StartCoroutine(MainManager.SetText("|letterprompt,5,-11,-212,8|", null, null));
                yield return new WaitUntil(() => !MainManager.instance.message);
                preset.name = MainManager.instance.flagstring[5];
                preset.mpNeeded = MainManager.instance.maxbp - MainManager.instance.bp;

                MainManager_Ext.Instance.medalPresets[Instance.presetId] = preset;
                MainManager.pausemenu.canpick = true;
                MainManager.PlaySound("ATKSuccess");
                MainManager.pausemenu.gameObject.SetActive(true);
                ResetToPage();
                yield break;
            }

            MainManager.pausemenu.canpick = true;
            MainManager.PlayBuzzer();
        }

        public void DeEquipAllBadges()
        {
            for (int i = 0; i < MainManager.instance.badges.Count; i++)
            {
                MainManager.instance.badges[i][1] = -2;
            }
            MainManager.instance.bp = MainManager.instance.maxbp;
            MainManager.ApplyBadges();
        }

        public static bool HasAllBadges(List<int[]> required)
        {
            var originalCounts = MainManager.instance.badges
                .GroupBy(b => b[0])
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var group in required.GroupBy(b => b[0]))
            {
                int badgeId = group.Key;
                int neededCount = group.Count();

                if (!originalCounts.TryGetValue(badgeId, out int availableCount) || availableCount < neededCount)
                {
                    PauseMenu_Ext.Instance.UpdateDesc(MainManager.menutext[306]);
                    return false;
                }
            }

            return true;
        }

        public void LoadMedalPreset(MedalPreset preset)
        {
            DeEquipAllBadges();

            if (HasAllBadges(preset.medals) && HasEnoughMp(preset) && MainManager.instance.playerdata.Length == 3)
            {
                for(int i=0;i < preset.medals.Count; i++)
                {
                    int index = MainManager.instance.badges.FindIndex(b => b[0] == preset.medals[i][0] && b[1] == -2);
                    if(index != -1)
                    {
                        MainManager.instance.badges[index][1] = preset.medals[i][1];
                    }
                    MainManager.instance.bp -= Mathf.Clamp(Convert.ToInt32(MainManager.badgedata[preset.medals[i][0], 2]), 0, MainManager.instance.flags[613] ? 1 : 999);
                }
                MainManager.ApplyBadges();
                MainManager.PlaySound("ATKSuccess");
                ResetToPage();
                return;
            }

            MainManager.pausemenu.canpick = true;
            MainManager.PlayBuzzer();
        }

        public void ResetToPage()
        {
            MainManager.listY = -1;
            MainManager.pausemenu.page = 3;
            Instance.presetId = -1;
            MainManager.ResetList();
            MainManager.pausemenu.UpdateText();
        }

        public void DeletePreset()
        {
            MainManager_Ext.Instance.medalPresets[Instance.presetId] = null;
            MainManager.PlaySound("ATKSuccess");
            ResetToPage();
        }

        public void GetPresetCode(MedalPreset preset)
        {
            GUIUtility.systemCopyBuffer = Compressor.CompressAndEncode(preset.ToString());
            MainManager.PlaySound("ATKSuccess");
            UpdateDesc(MainManager.menutext[288]);
        }

        public IEnumerator LoadFromCodePreset()
        {
            string lastClipboard = "";
            UpdateDesc(MainManager.menutext[289]);

            while (!MainManager.GetKey(5, false))
            {
                if(lastClipboard != GUIUtility.systemCopyBuffer)
                {
                    lastClipboard = GUIUtility.systemCopyBuffer;
                    string presetString = Compressor.DecodeAndDecompress(lastClipboard);

                    var preset = MedalPreset.GetPresetFromString(presetString);
                    if(preset != null)
                    {
                        UpdateDesc(MainManager.menutext[290]);
                        MainManager.PlaySound("ATKSuccess");
                        CalculatePresetMpNeeded(preset);

                        MainManager_Ext.Instance.medalPresets[Instance.presetId] = preset;
                        yield return EventControl.halfsec;
                        MainManager.pausemenu.canpick = true;
                        ResetToPage();
                        yield break;
                    }
                    else
                    {
                        UpdateDesc(MainManager.menutext[289]);
                    }
                }
                yield return null;
            }
            UpdateDesc(MainManager.menutext[303]);
            MainManager.pausemenu.canpick = true;
            MainManager.PlaySound("BadgeDequip");
        }

        void CalculatePresetMpNeeded(MedalPreset preset)
        {
            preset.mpNeeded = 0;
            for(int i = 0; i < preset.medals.Count; i++)
            {
                preset.mpNeeded += Mathf.Clamp(Convert.ToInt32(MainManager.badgedata[preset.medals[i][0], 2]), 0, MainManager.instance.flags[613] ? 1 : 999);
            }
        }

        public void UpdateDesc(string text)
        {
            MainManager.DestroyText(MainManager.pausemenu.boxes[1].transform);
            MainManager.pausemenu.StartCoroutine(MainManager.SetText(text, 0, null, false, false, new Vector3(-5.65f, 0.75f), Vector3.zero, Vector2.one, MainManager.pausemenu.boxes[1].transform, null));
        }

        public bool HasEnoughMp(MedalPreset preset)
        {
            int tpNeeded = preset.medals.Count(m => m[0] == (int)Medal.MPPlus) * 3;        
            if(!(MainManager.instance.maxbp >= preset.mpNeeded - tpNeeded && MainManager.instance.maxtp > tpNeeded))
            {
                UpdateDesc(MainManager.menutext[307]);
                return false;
            }
            return true;
        }

        public bool AnyMedalEquipped()
        {
            for (int i = 0; i < MainManager.instance.badges.Count; i++)
            {
                if (MainManager.instance.badges[i][1] != -2)
                {
                    return true;
                }
            }
            return false;
        }

        public class MedalCategory
        {
            public int[] medals;
            public string name;
            public int iconId;
            public Vector2 iconSize;
        }

        public class AreaItem
        {
            public int[] crystalBerries;
            public int[] loreBooks;
            public int[] discoveries;

            public int GetCBPercent()
            {
                if (crystalBerries != null)
                {
                    int count = 0;
                    for (int i = 0; i < crystalBerries.Length; i++)
                    {
                        if (MainManager.instance.crystalbflags[crystalBerries[i]])
                        {
                            count++;
                        }
                    }
                    return (int)((float)count / crystalBerries.Length * 100);
                }
                return 100;
            }

            public int GetDiscoveriesPercent()
            {
                if (discoveries != null)
                {
                    int count = 0;
                    for (int i = 0; i < discoveries.Length; i++)
                    {
                        if (MainManager.instance.librarystuff[0, discoveries[i]])
                        {
                            count++;
                        }
                    }
                    return (int)((float)count / discoveries.Length * 100);
                }
                return 100;
            }
            //had the data wrong, i need to do flag based
            public int GetLoreBookPercent()
            {
                if (loreBooks != null)
                {
                    int count = 0;
                    for (int i = 0; i < loreBooks.Length; i++)
                    {
                        if (MainManager.instance.flags[loreBooks[i]])
                        {
                            count++;
                        }
                    }
                    return (int)((float)count / loreBooks.Length * 100);
                }
                return 100;
            }
        }
    }
}
