using BFPlus.Extensions;
using BFPlus.Patches.DoActionPatches;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BFPlus.Patches.PauseMenuTranspilers.UpdatePatches
{
    public class PatchTextSkipState : PatchBasePauseMenuUpdate
    {
        public PatchTextSkipState()
        {
            priority = 2095;
        }
        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchStloc1(), i => i.MatchLdloc1(), i => i.MatchLdcI4(183));
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PauseMenu_Ext), "CheckFastTextState"));
        }
    }

    public class PatchEnemyData : PatchBasePauseMenuUpdate
    {
        public PatchEnemyData()
        {
            priority = 3905;
        }
        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(MoveType.After,i => i.MatchStfld(AccessTools.Field(typeof(PauseMenu), "enemydata")));
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PauseMenu_Ext), "SetEnemyData"));
        }
    }

    public class PatchMpPlusEquip : PatchBasePauseMenuUpdate
    {
        public PatchMpPlusEquip()
        {
            priority = 1005;
        }
        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdstr("BadgeDequip"));
            int cursorIndex = cursor.Index;

            cursor.GotoPrev(i => i.MatchLdloc(out _));

            var arrayRef = cursor.Next.Operand;
            ILLabel label = null;        
            
            cursor.GotoNext(i => i.MatchBlt(out label));

            cursor.Goto(cursorIndex);

            cursor.Emit(OpCodes.Ldloc_S, arrayRef);
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PauseMenu_Ext), "CanDequip"));
            cursor.Emit(OpCodes.Brfalse, label);

            cursor.GotoNext(i=>i.MatchLdcI4(-1), i=>i.MatchStsfld(out _),i => i.MatchLdstr("BadgeEquip"));
            cursor.Emit(OpCodes.Ldloc_S, arrayRef);
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PauseMenu_Ext), "CanEquip"));
            cursor.Emit(OpCodes.Brfalse, label);
        }
    }

    public class PatchChooseMedalCategory : PatchBasePauseMenuUpdate
    {
        public PatchChooseMedalCategory()
        {
            priority = 988;
        }
        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdcR4(5), i=>i.MatchStfld(AccessTools.Field(typeof(MainManager), "inputcooldown")), i=>i.MatchLdsfld(out _));
            cursor.GotoNext(MoveType.After,i => i.MatchStfld(out _));
            int cursorIndex = cursor.Index;

            cursor.GotoNext(MoveType.After,i => i.MatchCall(AccessTools.Method(typeof(MainManager), "ApplyBadges")));
            var jumpLabel = cursor.Next.Operand;
            cursor.Goto(cursorIndex);

            ILLabel label = cursor.DefineLabel();
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PatchChooseMedalCategory), "CanChooseMedalCategory"));
            cursor.Emit(OpCodes.Brfalse, label);
            cursor.Emit(OpCodes.Br, jumpLabel);
            cursor.MarkLabel(label);

            ILLabel outLabel = null;
            cursor.GotoPrev(i => i.MatchLdcI4(2));
            cursor.GotoNext(i => i.MatchBge(out outLabel));
            cursor.Emit(OpCodes.Beq, outLabel);
            cursor.Remove();
        }

        static bool CanChooseMedalCategory()
        {
            if(MainManager.pausemenu.page == 0 && PauseMenu_Ext.Instance.chooseMedalCategory == -1 && MainManager.instance.option < MainManager.listvar.Length)
            {
                PauseMenu_Ext.Instance.chooseMedalCategory = MainManager.listvar[MainManager.instance.option];
                MainManager.listY = -1;

                MainManager.instance.multilist = PauseMenu_Ext.Instance.GetCategoryMedals();
                MainManager.pausemenu.lastmedal = MainManager.SaveList();
                MainManager.ResetList();
                MainManager.pausemenu.UpdateText();
                MainManager.PlaySound("PageFlip");

                PauseMenu_Ext.MedalCategory category = PauseMenu_Ext.Instance.medalCategories[PauseMenu_Ext.Instance.chooseMedalCategory];

                if(PauseMenu_Ext.Instance.medalCategoryIcon == null)
                {
                    var sprite = category.iconId >= 0 ? PauseMenu_Ext.Instance.categoryIcons[category.iconId] : MainManager.guisprites[Mathf.Abs(category.iconId)];
                    Vector3 position = category.iconId >= 0 ? new Vector2(-1.3f, 4.45f) : new Vector2(-1.35f, 4f);

                    PauseMenu_Ext.Instance.medalCategoryIcon = MainManager.NewUIObject("CategoryIcon", MainManager.pausemenu.boxes[0].transform, position, Vector3.one*0.45f, sprite);
                }
                return true;
            }

            if(MainManager.pausemenu.page == 3 && MainManager.instance.option < MainManager.listvar.Length)
            {
                if(PauseMenu_Ext.Instance.presetId == -1)
                {
                    //we are in the preset list
                    PauseMenu_Ext.Instance.presetId = MainManager.listvar[MainManager.instance.option];
                    MainManager.listY = -1;

                    MainManager.ResetList();
                    MainManager.pausemenu.UpdateText();
                    MainManager.PlaySound("PageFlip");
                }
                else
                {
                    var preset = MainManager_Ext.Instance.medalPresets[PauseMenu_Ext.Instance.presetId];

                    if(MainManager.instance.option != 1 && MainManager.instance.option != 4 && preset == null)
                    {
                        MainManager.PlayBuzzer();
                        return true;
                    }

                    switch (MainManager.instance.option)
                    {
                        case 0:
                            PauseMenu_Ext.Instance.LoadMedalPreset(preset);
                            break;

                        case 1:
                            MainManager.pausemenu.canpick = false;
                            MainManager.instance.StartCoroutine(PauseMenu_Ext.Instance.SaveMedalPreset());
                            break;

                        case 2:
                            PauseMenu_Ext.Instance.DeletePreset();
                            break;

                        case 3:
                            PauseMenu_Ext.Instance.GetPresetCode(preset);
                            break;

                        case 4:
                            MainManager.pausemenu.canpick = false;
                            MainManager.instance.StartCoroutine(PauseMenu_Ext.Instance.LoadFromCodePreset());
                            break;

                    }
                }

                return true;
            }

            return false;
        }
    }

    public class PatchExitMedalCategory : PatchBasePauseMenuUpdate
    {
        public PatchExitMedalCategory()
        {
            priority = 850;
        }
        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdcI4(3), i => i.MatchLdcI4(0), i => i.MatchCall(AccessTools.Method(typeof(MainManager), "GetKey", new Type[] {typeof(int), typeof(bool)})));
            cursor.GotoNext(i => i.MatchLdcI4(3), i => i.MatchLdcI4(0), i => i.MatchCall(AccessTools.Method(typeof(MainManager), "GetKey", new Type[] { typeof(int), typeof(bool) })));
            cursor.GotoNext(i => i.MatchLdcI4(3), i => i.MatchLdcI4(0), i => i.MatchCall(AccessTools.Method(typeof(MainManager), "GetKey", new Type[] { typeof(int), typeof(bool) })));

            ILLabel label = cursor.DefineLabel();
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PatchExitMedalCategory), "IsInMedalCategory"));
            cursor.Emit(OpCodes.Brfalse, label);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(label);
        }

        static bool IsInMedalCategory()
        {
            if((PauseMenu_Ext.Instance.chooseMedalCategory != -1 || MainManager.pausemenu.page > 0) && MainManager.GetKey(5, false))
            {
                PauseMenu_Ext.Instance.chooseMedalCategory = -1;
                PauseMenu_Ext.Instance.DestroyMedalCategoryIcon();
                MainManager.instance.inputcooldown = 5f;
                MainManager.listY = -1;
                MainManager.ResetList();
                if (MainManager.pausemenu.page == 0) 
                {
                    MainManager.LoadList(MainManager.pausemenu.lastmedal);
                }

                if(MainManager.pausemenu.page == 3 && PauseMenu_Ext.Instance.presetId != -1)
                {
                    PauseMenu_Ext.Instance.presetId = -1;
                }
                else
                {
                    MainManager.pausemenu.page = 0;
                }
                MainManager.pausemenu.UpdateText();
                MainManager.PlaySound("PageFlip");
                return true;
            }

            if(PauseMenu_Ext.Instance.presetId != -1 && MainManager.pausemenu.page == 3 && MainManager_Ext.Instance.medalPresets[PauseMenu_Ext.Instance.presetId] != null && MainManager.instance.option >= 5)
            {
                MainManager_Ext.MedalPreset preset = MainManager_Ext.Instance.medalPresets[PauseMenu_Ext.Instance.presetId];

                int index = MainManager.instance.option - 5;
                //right
                if (MainManager.GetKey(3, false))
                {
                    preset.icons[index]++;

                    if(preset.icons[index] >= PauseMenu_Ext.Instance.categoryIcons.Length)
                    {
                        preset.icons[index] = 0;
                    }
                    MainManager.PlayScrollSound();
                    PauseMenu_Ext.Instance.presetIcons[index].sprite = PauseMenu_Ext.Instance.categoryIcons[preset.icons[index]];
                    return true;
                }

                //left
                if (MainManager.GetKey(2, false))
                {
                    preset.icons[index]--;

                    if (preset.icons[index] < 0)
                    {
                        preset.icons[index] = PauseMenu_Ext.Instance.categoryIcons.Length-1;
                    }
                    MainManager.PlayScrollSound();
                    PauseMenu_Ext.Instance.presetIcons[index].sprite = PauseMenu_Ext.Instance.categoryIcons[preset.icons[index]];
                    return true;
                }
            }


            return false;
        }
    }


    public class PatchEquippedMedalListBug : PatchBasePauseMenuUpdate
    {
        public PatchEquippedMedalListBug()
        {
            priority = 1204;
        }
        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdarg0(), i => i.MatchLdfld(AccessTools.Field(typeof(PauseMenu), "lastmedal")));
            cursor.RemoveRange(3);
        }
    }

    public class PatchAddNewPage : PatchBasePauseMenuUpdate
    {
        public PatchAddNewPage()
        {
            priority = 1186;
        }
        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdfld(AccessTools.Field(typeof(PauseMenu), "page")), i=>i.MatchLdcI4(2), i=>i.MatchBle(out _));
            cursor.GotoNext(i => i.MatchLdcI4(2));
            cursor.Emit(OpCodes.Ldc_I4, 3);
            cursor.Remove();
        }
    }

}
