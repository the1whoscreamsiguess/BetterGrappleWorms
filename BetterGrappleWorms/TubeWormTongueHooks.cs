using System;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;

namespace BetterGrappleWorms
{
    static class TubeWormTongueHooks
    {

        public static void OnEnable()
        {
            On.TubeWorm.Tongue.ctor += TubeWorm_Tongue_ctor;
            On.TubeWorm.Tongue.ProperAutoAim += TubeWorm_Tongue_ProperAutoAim;
            On.TubeWorm.Tongue.Update += TubeWorm_Tongue_Update;

            IL.TubeWorm.Tongue.Update += TubeWorm_Tongue_UpdateIL;
            IL.TubeWorm.Tongue.Elasticity += TubeWorm_Tongue_ElasticityIL;
        }

        //dictionary setup
        public static void TubeWorm_Tongue_ctor(On.TubeWorm.Tongue.orig_ctor orig, TubeWorm.Tongue self, TubeWorm worm, int tongueNum)
        {
            orig(self, worm, tongueNum);
            if (!BetterGrappleWorms.tongueIsAttachedToVine.ContainsKey(self))
            {
                BetterGrappleWorms.tongueIsAttachedToVine.Add(self, false);
                BetterGrappleWorms.tongueVinePos.Add(self, null);
            }
        }

        //Override tube worm tongue aiming with a custom one if being held by a player in zero gravity
        public static Vector2 TubeWorm_Tongue_ProperAutoAim(On.TubeWorm.Tongue.orig_ProperAutoAim orig, TubeWorm.Tongue self, Vector2 originalDir)
        {
            Player player = null;
            if (self.worm.grabbedBy.Count > 0 && self.worm.grabbedBy[0].grabber is Player)
            {
                player = self.worm.grabbedBy[0].grabber as Player;
            }
            //If we're in zero gravity, use different omnidirectional aiming
            if (player.EffectiveRoomGravity == 0)
            {
                float tongueDistance = self.idealRopeLength * 1.53f;
                float ropeLength = self.idealRopeLength;
                float dirDegrees = Custom.VecToDeg(originalDir);
                Vector2 input = new Vector2(player.input[0].x, player.input[0].y);

                Vector2 result = originalDir;
                float bestDistance = float.MaxValue;
                for (float checkingAngle = 0f; checkingAngle < 17.5f; checkingAngle += 2.5f)
                {
                    //positive and negative checking angles
                    for (float flip = -1f; flip <= 1f; flip += 2f)
                    {
                        Vector2? wallHitPos = SharedPhysics.ExactTerrainRayTracePos(self.room, self.baseChunk.pos, self.baseChunk.pos + Custom.DegToVec(dirDegrees + checkingAngle * flip) * tongueDistance);
                        if (wallHitPos != null)
                        {
                            //Try to center the distance on a point in front of the player to facilitate pulling yourself forward
                            float myDistance = Mathf.Abs((wallHitPos.Value - self.baseChunk.pos).magnitude - (tongueDistance * 0.85f));

                            if (myDistance < bestDistance)
                            {
                                bestDistance = myDistance;
                                result = Custom.DegToVec(dirDegrees + checkingAngle * flip);
                                self.worm.playerCheatAttachPos = new Vector2?(wallHitPos.Value + Custom.DirVec(wallHitPos.Value, self.baseChunk.pos) * 2f);
                            }
                        }
                    }
                }
                return result;
            }
            return orig(self, originalDir);
        }

        //Vine attachment updating and validation
        public static void TubeWorm_Tongue_Update(On.TubeWorm.Tongue.orig_Update orig, TubeWorm.Tongue self)
        {
            orig(self);

            Player player = null;
            if (self.worm.grabbedBy.Count > 0 && self.worm.grabbedBy[0].grabber is Player)
            {
                player = self.worm.grabbedBy[0].grabber as Player;
            }

            if (BetterGrappleWorms.tongueIsAttachedToVine[self])
            {
                ClimbableVinesSystem vinesSystem = self.room.climbableVines;
                //detach if the vine is invalid, we have become unattached, or the player is climbing the same vine
                if (player.animation == Player.AnimationIndex.VineGrab && BetterGrappleWorms.tongueVinePos[self].vine == player.vinePos.vine)
                {
                    self.Release();
                }
                if (!self.Attached || vinesSystem == null)
                {
                    TubeWormTongueCustom.TubeWormTongueUnstickFromVine(self);
                }
                else //Set terrain stuck pos to vine pos each frame to approximate being stuck to the vine
                {
                    self.terrainStuckPos = vinesSystem.OnVinePos(BetterGrappleWorms.tongueVinePos[self]);
                }
            }
        }

        //IL hooks

        //Pull on attached vine
        public static void TubeWorm_Tongue_ElasticityIL(ILContext il)
        {
            try
            {
                //Match the part right before applying elasticity to body chunks
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<TubeWorm.Tongue>(nameof(TubeWorm.Tongue.mode)),
                x => x.MatchLdsfld<TubeWorm.Tongue.Mode>(nameof(TubeWorm.Tongue.Mode.AttachedToObject)),
                x => x.MatchCall(out _),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<TubeWorm.Tongue>(nameof(TubeWorm.Tongue.attachedChunk)),
                x => x.MatchDup(),
                x => x.MatchLdfld<BodyChunk>(nameof(BodyChunk.vel))
                );

                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg, 0);
                c.Emit(OpCodes.Ldloc, 1);
                c.Emit(OpCodes.Ldloc, 2);
                c.Emit(OpCodes.Ldloc, 4);
                c.Emit(OpCodes.Ldloc, 5);
                c.Emit(OpCodes.Ldloc, 0);
                //do the body chunk stuff but for an attached vine
                c.EmitDelegate<Action<TubeWorm.Tongue, Vector2, float, float, float, float>>((TubeWorm.Tongue self, Vector2 a, float totalLength, float num2, float num3, float num) =>
                {
                    if (BetterGrappleWorms.tongueIsAttachedToVine[self] && BetterGrappleWorms.tongueVinePos[self] != null)
                    {
                        ClimbableVinesSystem vinesSystem = self.room.climbableVines;
                        if (vinesSystem != null)
                        {
                            float vineMass = BetterGrappleWorms.tongueVinePos[self].vine.Mass(vinesSystem.PrevSegAtFloat(BetterGrappleWorms.tongueVinePos[self].vine, BetterGrappleWorms.tongueVinePos[self].floatPos));
                            num = vineMass / (vineMass + self.baseChunk.mass);
                            vinesSystem.PushAtVine(BetterGrappleWorms.tongueVinePos[self], (a * ((totalLength - num2) * num3 * (1f - num))) * 2);
                        }
                    }
                });
            }
            catch (Exception e)
            {
               BetterGrappleWorms.MyLogger.LogError(e);
            }
        }

        //Insert TubeWormTongueStickToVines in the middle of the update code
       public static void TubeWorm_Tongue_UpdateIL(ILContext il)
        {
            try
            {
                //match the part during retraction right after checking for poles to stick to
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                MoveType.After,
                x => x.MatchStloc(7),
                x => x.MatchLdloc(7),
                x => x.MatchLdloc(6),
                x => x.MatchBlt(out _)
                );

                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg, 0);
                //extra code
                c.EmitDelegate<Action<TubeWorm.Tongue>>((Action<TubeWorm.Tongue>)TubeWormTongueCustom.TubeWormTongueLookForVineToStickTo);
            }
            catch (Exception e)
            {
                BetterGrappleWorms.MyLogger.LogError(e);
            }
        }

    }
}
