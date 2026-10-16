using System;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;

namespace BetterGrappleWorms
{
    static class TubeWormHooks
    {

        public static void OnEnable()
        {
            On.TubeWorm.ctor += TubeWorm_ctor;
            On.TubeWorm.GrabbedByPlayer += TubeWorm_GrabbedByPlayer;
            IL.TubeWorm.GrabbedByPlayer += TubeWorm_GrabbedByPlayerIL;
            IL.TubeWorm.JumpButton += TubeWorm_JumpButtonIL;
        }

        //dictionary setup
        public static void TubeWorm_ctor(On.TubeWorm.orig_ctor orig, TubeWorm self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!BetterGrappleWorms.lastPlayerZeroGPoleGrab.ContainsKey(self))
            {
                BetterGrappleWorms.lastPlayerZeroGPoleGrab.Add(self, new bool[2]);
            }
        }


        //use cancel handling
        public static void TubeWorm_GrabbedByPlayer(On.TubeWorm.orig_GrabbedByPlayer orig, TubeWorm self)
        {
            Player player = null;
            for (int i = 0; i < self.grabbedBy.Count; i++)
            {
                if (self.grabbedBy[i].grabber is Player)
                {
                    player = (self.grabbedBy[i].grabber as Player);
                    break;
                }
            }
            //if we're using the jump button tongue controls and the player is climbing a vine or pole in zero gravity, cancel use
            if (self.useBool && (!ModManager.MMF || !MoreSlugcats.MMF.cfgOldTongue.Value))
            {
                if (player != null && player.room != null && player.EffectiveRoomGravity == 0f &&
                    (player.animation == Player.AnimationIndex.VineGrab || player.vineGrabDelay >= 9 ||
                    player.animation == Player.AnimationIndex.ZeroGPoleGrab || BetterGrappleWorms.lastPlayerZeroGPoleGrab[self][0] || BetterGrappleWorms.lastPlayerZeroGPoleGrab[self][1]))
                {
                    self.useBool = false;
                }
            }
            orig(self);
            //track pole grabbing status because zero g jump button check needs it
            BetterGrappleWorms.lastPlayerZeroGPoleGrab[self][1] = BetterGrappleWorms.lastPlayerZeroGPoleGrab[self][0];
            BetterGrappleWorms.lastPlayerZeroGPoleGrab[self][0] = player != null && player.animation == Player.AnimationIndex.ZeroGPoleGrab;
        }

        //IL hooks (FUCK THESE THINGS)

        public static void TubeWorm_GrabbedByPlayerIL(ILContext il)
        {
            try
            {
                //match tube worm assignment
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                MoveType.After,
                   x => x.MatchLdloc(0),
                      x => x.MatchLdarg(0),
                x => x.MatchStfld<Player>(nameof(Player.tubeWorm))
                );

                c.MoveAfterLabels();


                c.Emit(OpCodes.Ldloc, 0); //push player
                c.Emit(OpCodes.Ldloc, 1); //push the vector of where the worm is aiming
                //replace aim dir if we're in zero g
                c.EmitDelegate<Func<Player, Vector2, Vector2>>((Player plr, Vector2 shootDir) =>
                {
                    if (plr.EffectiveRoomGravity == 0)
                    {
                        shootDir = new Vector2(plr.input[0].x, plr.input[0].y).normalized;
                        //if player isn't pressing any movement keys, aim wherever their body is aimed at
                        shootDir = shootDir == new Vector2(0, 0) ? (plr.bodyChunks[0].pos - plr.bodyChunks[1].pos).normalized : shootDir;
                    }
                    return shootDir;
                });

                c.Emit(OpCodes.Stloc, 1);
            }
            catch (Exception e)
            {
                BetterGrappleWorms.MyLogger.LogError(e);
            }
        }

        //Adding extra conditions to tube worm jump controls so it can be used in zero gravity
        public static void TubeWorm_JumpButtonIL(ILContext il)
        {
            try
            {
                //match the part where it checks the player's body mode
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                MoveType.After,
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<Player>(nameof(Player.bodyMode)),
                x => x.MatchLdsfld<Player.BodyModeIndex>(nameof(Player.BodyModeIndex.Default)),
                x => x.MatchCall(out _)
                );

                c.MoveAfterLabels();

                //(the body mode check result is already on the stack)
                c.Emit(OpCodes.Ldarg, 0); //push self onto stack 
                c.Emit(OpCodes.Ldarg, 1); //push player onto stack
                //change the "player body mode is default" so it's also true in zero g
                c.EmitDelegate<Func<bool, TubeWorm, Player, bool>>((bool originalResult, TubeWorm self, Player plr) =>
                {
                    if (!BGWConfig.zeroGTongue.Value)
                    {
                        return originalResult;
                    }
                    return (originalResult || (plr.bodyMode == Player.BodyModeIndex.ZeroG && plr.animation == Player.AnimationIndex.ZeroGSwim))
                    && (!BGWConfig.corridorDropTongueDisable.Value || !plr.corridorDrop);

                });
            }
            catch (Exception e)
            {
                BetterGrappleWorms.MyLogger.LogError(e);
            }
        }


    }
}
