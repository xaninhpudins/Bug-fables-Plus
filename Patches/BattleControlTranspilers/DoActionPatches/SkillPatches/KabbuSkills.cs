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

namespace BFPlus.Patches.BattleControlTranspilers.DoActionPatches.SkillPatches
{
    /// <summary>
    /// Replace the GetPlayerData Call for heavy strike with the current turn field
    /// </summary>
    public class PatchHeavyStrikeHoloSkill : PatchBaseDoAction
    {
        public PatchHeavyStrikeHoloSkill()
        {
            priority = 45134;
        }

        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdcI4(1), i=>i.MatchLdcI4(1), i=>i.MatchCall(AccessTools.Method(typeof(MainManager), "GetPlayerData", new Type[] { typeof(int), typeof(bool)})));

            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PatchHeavyStrikeHoloSkill), "GetCurrentTurnPlayer"));
            Utils.RemoveUntilInst(cursor, i => i.MatchNewobj(out _));

            cursor.GotoNext(i => i.MatchLdcI4(1), i => i.MatchLdcI4(1), i => i.MatchCall(AccessTools.Method(typeof(MainManager), "GetPlayerData", new Type[] { typeof(int), typeof(bool) })));

            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PatchHeavyStrikeHoloSkill), "GetCurrentTurnPlayer"));
            Utils.RemoveUntilInst(cursor, i => i.MatchLdfld(out _));
        }

        static MainManager.BattleData GetCurrentTurnPlayer()
        {
            return MainManager.instance.playerdata[MainManager.battle.currentturn];
        }
    }
}
