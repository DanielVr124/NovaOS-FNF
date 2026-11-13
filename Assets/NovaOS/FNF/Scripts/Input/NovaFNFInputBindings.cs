using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Central keyboard binding table shared by menus and gameplay.
    /// Keep this component on the same persistent controller object and assign
    /// it to both NovaFNFMenuController and NovaFNFGameplayController.
    /// </summary>
    public class NovaFNFInputBindings : UdonSharpBehaviour
    {
        [Header("Lane bindings (primary + secondary)")]
        public KeyCode lane0Primary = KeyCode.LeftArrow;
        public KeyCode lane0Secondary = KeyCode.A;
        public KeyCode lane1Primary = KeyCode.DownArrow;
        public KeyCode lane1Secondary = KeyCode.S;
        public KeyCode lane2Primary = KeyCode.UpArrow;
        public KeyCode lane2Secondary = KeyCode.W;
        public KeyCode lane3Primary = KeyCode.RightArrow;
        public KeyCode lane3Secondary = KeyCode.D;

        [Header("Menu and gameplay actions")]
        public KeyCode menuUpPrimary = KeyCode.UpArrow;
        public KeyCode menuUpSecondary = KeyCode.W;
        public KeyCode menuDownPrimary = KeyCode.DownArrow;
        public KeyCode menuDownSecondary = KeyCode.S;
        public KeyCode acceptPrimary = KeyCode.Return;
        public KeyCode acceptSecondary = KeyCode.KeypadEnter;
        public KeyCode acceptTertiary = KeyCode.Space;
        public KeyCode back = KeyCode.Escape;
        public KeyCode pause = KeyCode.P;

        public bool LanePressed(int lane)
        {
            KeyCode primary = GetLanePrimary(lane);
            KeyCode secondary = GetLaneSecondary(lane);
            return (primary != KeyCode.None && Input.GetKeyDown(primary)) ||
                (secondary != KeyCode.None && Input.GetKeyDown(secondary));
        }

        public bool LaneHeld(int lane)
        {
            KeyCode primary = GetLanePrimary(lane);
            KeyCode secondary = GetLaneSecondary(lane);
            return (primary != KeyCode.None && Input.GetKey(primary)) ||
                (secondary != KeyCode.None && Input.GetKey(secondary));
        }

        public bool MenuUpPressed()
        {
            return (menuUpPrimary != KeyCode.None && Input.GetKeyDown(menuUpPrimary)) ||
                (menuUpSecondary != KeyCode.None && Input.GetKeyDown(menuUpSecondary));
        }

        public bool MenuDownPressed()
        {
            return (menuDownPrimary != KeyCode.None && Input.GetKeyDown(menuDownPrimary)) ||
                (menuDownSecondary != KeyCode.None && Input.GetKeyDown(menuDownSecondary));
        }

        public bool AcceptPressed()
        {
            return (acceptPrimary != KeyCode.None && Input.GetKeyDown(acceptPrimary)) ||
                (acceptSecondary != KeyCode.None && Input.GetKeyDown(acceptSecondary)) ||
                (acceptTertiary != KeyCode.None && Input.GetKeyDown(acceptTertiary));
        }

        public bool BackPressed()
        {
            return back != KeyCode.None && Input.GetKeyDown(back);
        }

        public bool PausePressed()
        {
            return pause != KeyCode.None && Input.GetKeyDown(pause);
        }

        public KeyCode GetBinding(int index)
        {
            if (index == 0) return lane0Primary;
            if (index == 1) return lane0Secondary;
            if (index == 2) return lane1Primary;
            if (index == 3) return lane1Secondary;
            if (index == 4) return lane2Primary;
            if (index == 5) return lane2Secondary;
            if (index == 6) return lane3Primary;
            if (index == 7) return lane3Secondary;
            if (index == 8) return menuUpPrimary;
            if (index == 9) return menuUpSecondary;
            if (index == 10) return menuDownPrimary;
            if (index == 11) return menuDownSecondary;
            if (index == 12) return acceptPrimary;
            if (index == 13) return acceptSecondary;
            if (index == 14) return acceptTertiary;
            if (index == 15) return back;
            if (index == 16) return pause;
            return KeyCode.None;
        }

        public void SetBinding(int index, KeyCode key)
        {
            if (index == 0) lane0Primary = key;
            else if (index == 1) lane0Secondary = key;
            else if (index == 2) lane1Primary = key;
            else if (index == 3) lane1Secondary = key;
            else if (index == 4) lane2Primary = key;
            else if (index == 5) lane2Secondary = key;
            else if (index == 6) lane3Primary = key;
            else if (index == 7) lane3Secondary = key;
            else if (index == 8) menuUpPrimary = key;
            else if (index == 9) menuUpSecondary = key;
            else if (index == 10) menuDownPrimary = key;
            else if (index == 11) menuDownSecondary = key;
            else if (index == 12) acceptPrimary = key;
            else if (index == 13) acceptSecondary = key;
            else if (index == 14) acceptTertiary = key;
            else if (index == 15) back = key;
            else if (index == 16) pause = key;
        }

        public void ResetDefaults()
        {
            lane0Primary = KeyCode.LeftArrow;
            lane0Secondary = KeyCode.A;
            lane1Primary = KeyCode.DownArrow;
            lane1Secondary = KeyCode.S;
            lane2Primary = KeyCode.UpArrow;
            lane2Secondary = KeyCode.W;
            lane3Primary = KeyCode.RightArrow;
            lane3Secondary = KeyCode.D;
            menuUpPrimary = KeyCode.UpArrow;
            menuUpSecondary = KeyCode.W;
            menuDownPrimary = KeyCode.DownArrow;
            menuDownSecondary = KeyCode.S;
            acceptPrimary = KeyCode.Return;
            acceptSecondary = KeyCode.KeypadEnter;
            acceptTertiary = KeyCode.Space;
            back = KeyCode.Escape;
            pause = KeyCode.P;
        }

        private KeyCode GetLanePrimary(int lane)
        {
            if (lane == 0) return lane0Primary;
            if (lane == 1) return lane1Primary;
            if (lane == 2) return lane2Primary;
            return lane3Primary;
        }

        private KeyCode GetLaneSecondary(int lane)
        {
            if (lane == 0) return lane0Secondary;
            if (lane == 1) return lane1Secondary;
            if (lane == 2) return lane2Secondary;
            return lane3Secondary;
        }
    }
}
