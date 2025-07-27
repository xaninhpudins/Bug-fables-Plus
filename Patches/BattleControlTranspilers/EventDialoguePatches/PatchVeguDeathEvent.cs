using BFPlus.Extensions;
using BFPlus.Patches.DoActionPatches;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFPlus.Patches.BattleControlTranspilers.EventDialoguePatches
{
    /// <summary>
    /// Fixed a softlock on VeGu with killing him with lifelust while numbed while having no extra turns.
    /// </summary>
    public class PatchVeguDeathEvent : PatchBaseEventDialogue
    {
        public PatchVeguDeathEvent()
        {
            priority = 6911;
        }

        protected override void ApplyPatch(ILCursor cursor)
        {
            cursor.GotoNext(i => i.MatchLdloc1(), i => i.MatchLdcI4(24));
            cursor.GotoNext(
                i => i.MatchLdloc1(),
                i => i.MatchLdloc1(),
                i => i.MatchLdloc1(),
                i => i.MatchCall(AccessTools.Method(typeof(BattleControl), "CheckDead")));

            ILLabel label = cursor.DefineLabel();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PatchVeguDeathEvent), "DoVeguDeath"));
            Utils.InsertYieldReturn(cursor);
            cursor.Emit(OpCodes.Br, label);

            cursor.GotoNext(i => i.MatchBrtrue(out _));
            cursor.GotoNext(i => i.MatchLdarg0());
            cursor.MarkLabel(label);
        }

        static IEnumerator DoVeguDeath()
        {
            int veguId = MainManager.battle.EnemyInField((int)MainManager.Enemies.VenusBoss);
            for (int i = 0; i < MainManager.battle.enemydata.Length; i++) 
            {
                if (veguId != i && MainManager.battle.enemydata[i].battleentity.deathcoroutine == null)
                {
                    MainManager.battle.enemydata[i].battleentity.StartDeath();
                }
            }
            yield return MainManager.battle.enemydata[veguId].battleentity.Death(true);
            yield return EventControl.sec;
            MainManager.battle.EndBattleWon(true, null);
        }

    }
}
