using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Runtime key-rebinding screen. Assign the list of permitted KeyCodes and
    /// wire each control row's Change button to BeginCapture(index).
    /// </summary>
    public class NovaFNFControlsController : UdonSharpBehaviour
    {
        public NovaFNFInputBindings inputBindings;
        public NovaFNFSaveManager saveManager;
        public KeyCode[] assignableKeys;
        public Text capturePrompt;
        public Text[] bindingLabels;

        private int capturingIndex = -1;

        private void Start()
        {
            if (assignableKeys == null || assignableKeys.Length == 0)
            {
                assignableKeys = new KeyCode[]
                {
                    KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E,
                    KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J,
                    KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O,
                    KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T,
                    KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
                    KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
                    KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7,
                    KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.LeftArrow, KeyCode.DownArrow,
                    KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.Return, KeyCode.KeypadEnter,
                    KeyCode.Space, KeyCode.Tab, KeyCode.Backspace, KeyCode.LeftShift,
                    KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
                    KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.Comma, KeyCode.Period,
                    KeyCode.Slash, KeyCode.Semicolon, KeyCode.Quote, KeyCode.LeftBracket,
                    KeyCode.RightBracket, KeyCode.Minus, KeyCode.Equals
                };
            }

            RefreshLabels();
        }

        private void Update()
        {
            if (capturingIndex < 0 || assignableKeys == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelCapture();
                return;
            }

            for (int i = 0; i < assignableKeys.Length; i++)
            {
                KeyCode candidate = assignableKeys[i];
                if (candidate == KeyCode.None || !Input.GetKeyDown(candidate)) continue;

                inputBindings.SetBinding(capturingIndex, candidate);
                capturingIndex = -1;
                UpdateCapturePrompt();
                RefreshLabels();

                if (saveManager != null)
                    saveManager.SaveKeyBindings(inputBindings);
                return;
            }
        }

        public void BeginCapture(int bindingIndex)
        {
            if (inputBindings == null || bindingIndex < 0 || bindingIndex > 16) return;
            capturingIndex = bindingIndex;
            UpdateCapturePrompt();
        }

        public void CancelCapture()
        {
            capturingIndex = -1;
            UpdateCapturePrompt();
        }

        public void ResetAllBindings()
        {
            if (inputBindings == null) return;
            inputBindings.ResetDefaults();
            capturingIndex = -1;
            RefreshLabels();
            UpdateCapturePrompt();
            if (saveManager != null)
                saveManager.SaveKeyBindings(inputBindings);
        }

        public void RefreshLabels()
        {
            if (inputBindings == null || bindingLabels == null) return;
            int count = Mathf.Min(bindingLabels.Length, 17);
            for (int i = 0; i < count; i++)
            {
                if (bindingLabels[i] != null)
                    bindingLabels[i].text = inputBindings.GetBinding(i).ToString();
            }
        }

        private void UpdateCapturePrompt()
        {
            if (capturePrompt == null) return;
            capturePrompt.text = capturingIndex >= 0
                ? "PRESS A KEY... (ESC TO CANCEL)"
                : "";
        }
    }
}
