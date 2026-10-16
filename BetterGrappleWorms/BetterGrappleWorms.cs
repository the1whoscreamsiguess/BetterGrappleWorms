using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using System.Security.Permissions;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using IL.MoreSlugcats;
using System.Net.NetworkInformation;
using RWCustom;
using System.Diagnostics.Eventing.Reader;
using MoreSlugcats;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace BetterGrappleWorms
{
    [BepInPlugin("t1ws.bettergrappleworms", "BetterGrappleWorms", "1.0")]
    public class BetterGrappleWorms : BaseUnityPlugin
    {
        static bool initiated;

        //have to do this bs so that grapple worm use cancel works because it triggers the frame after the player lets go of the pole
        public static Dictionary<TubeWorm, bool[]> lastPlayerZeroGPoleGrab;

        public static Dictionary<TubeWorm.Tongue, bool> tongueIsAttachedToVine;
        public static Dictionary<TubeWorm.Tongue, ClimbableVinesSystem.VinePosition> tongueVinePos;

        public static BepInEx.Logging.ManualLogSource MyLogger; //so that the il hooks in the static classes can do error logging if they fail

        public void Awake()
        {
            On.RainWorld.OnModsInit += new On.RainWorld.hook_OnModsInit(Init);
        }

        static void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (!initiated)
            {
                initiated = true;
                MachineConnector.SetRegisteredOI("t1ws.bettergrappleworms", new BGWConfig());
            }
        }

        public void OnEnable()
        {
            MyLogger = Logger;
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            TubeWormHooks.OnEnable();
            TubeWormTongueHooks.OnEnable();
        }

        void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            lastPlayerZeroGPoleGrab = new Dictionary<TubeWorm, bool[]>();
            tongueIsAttachedToVine = new Dictionary<TubeWorm.Tongue, bool>();
            tongueVinePos = new Dictionary<TubeWorm.Tongue, ClimbableVinesSystem.VinePosition>();
        }

    }
}
