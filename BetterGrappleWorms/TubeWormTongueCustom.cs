using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterGrappleWorms
{
    public static class TubeWormTongueCustom
    {

        //custom tube worm tongue vine stick code
        //vine sticking works by putting the worm in attach to terrain mode and then setting its tongue stuck pos to the vine position every frame

        public static void TubeWormTongueStickToVine(TubeWorm.Tongue self, ClimbableVinesSystem vinesSystem, ClimbableVinesSystem.VinePosition vinePosition)
        {
            self.AttachToTerrain(vinesSystem.OnVinePos(vinePosition));
            BetterGrappleWorms.tongueIsAttachedToVine[self] = true;
            BetterGrappleWorms.tongueVinePos[self] = vinePosition;
        }

        public static void TubeWormTongueUnstickFromVine(TubeWorm.Tongue self)
        {
            BetterGrappleWorms.tongueIsAttachedToVine[self] = false;
        }

        public static void TubeWormTongueLookForVineToStickTo(TubeWorm.Tongue self)
        {

            Player player = null;
            if (self.worm.grabbedBy.Count > 0 && self.worm.grabbedBy[0] != null && self.worm.grabbedBy[0].grabber != null && self.worm.grabbedBy[0].grabber is Player)
            {
                player = self.worm.grabbedBy[0].grabber as Player;
            }
            //Respect settings
            if (!BGWConfig.vineTongue.Value || (BGWConfig.vineTongueZeroGOnly.Value && player != null && player.EffectiveRoomGravity > 0f))
            {
                return;
            }
            if (self.room != null)
            {
                ClimbableVinesSystem vinesSystem = self.room.climbableVines;
                if (vinesSystem != null)
                {
                    float rad = 35f; //radius needs to be a little bit stupid or else it'll slip through gaps between the vine segments
                    ClimbableVinesSystem.VinePosition vinePosition = vinesSystem.VineOverlap(self.pos, rad);
                    //If at first you don't succeed, do some intermediate steps
                    if (vinePosition == null)
                    {
                        for (float i = 0.125f; i < 1; i += 0.125f)
                        {
                            vinePosition = vinesSystem.VineOverlap(Vector2.Lerp(self.pos, self.lastPos, i), rad);
                            if (vinePosition != null)
                            {
                                break;
                            }
                        }
                    }

                    if (vinePosition != null && vinePosition.vine != null)
                    {
                        //Make sure we're not trying to stick to a vine already being climbed by the player
                        if (player.animation == Player.AnimationIndex.VineGrab && vinePosition.vine == player.vinePos.vine)
                        {
                            return;
                        }

                        TubeWormTongueStickToVine(self, vinesSystem, vinePosition);
                    }
                }
            }
        }

    }
}
