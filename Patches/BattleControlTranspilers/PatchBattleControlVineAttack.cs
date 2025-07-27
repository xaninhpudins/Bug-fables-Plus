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

namespace BFPlus.Patches.BattleControlTranspilers
{
    /// <summary>
    /// Patch amount of hit the vine hits for mars, so that its only 1 even in hard mode
    /// </summary>
    public class PatchMarsVineHits : PatchBaseBattleControlVineAttack
    {
        public PatchMarsVineHits()
        {
            priority = 158572;
        }

        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdfld(out _), i=>i.MatchLdelema(out _));
            var callerIdRef = cursor.Next.Operand;

            cursor.GotoNext(i => i.MatchLdloc1());

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, callerIdRef);
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PatchMarsVineHits), "CheckVineHits"));
            cursor.RemoveRange(2);

            indexInserted = cursor.Index;
        }

        static bool CheckVineHits(int callerId)
        {
            return MainManager.battle.HardMode() && MainManager.battle.enemydata[callerId].animid != (int)NewEnemies.Mars;
        }
    }
}
