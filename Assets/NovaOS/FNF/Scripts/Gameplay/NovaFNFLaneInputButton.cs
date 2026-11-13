using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Hook PressLane to a UI/EventTrigger Pointer Down event and ReleaseLane
    /// to Pointer Up. This gives touch/VR pointer players a way to hit and hold
    /// lanes when they are playing on a world-space screen.
    /// </summary>
    public class NovaFNFLaneInputButton : UdonSharpBehaviour
    {
        public NovaFNFGameplayController gameplay;
        [Range(0, 3)] public int lane;

        public void PressLane()
        {
            if (gameplay == null) return;
            gameplay.SetExternalLaneHeld(lane, true);
            gameplay.HitLane(lane);
        }

        public void ReleaseLane()
        {
            if (gameplay == null) return;
            gameplay.SetExternalLaneHeld(lane, false);
        }
    }
}
