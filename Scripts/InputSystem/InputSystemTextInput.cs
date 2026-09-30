using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace EightAID.EIGHTAIDLib.Input
{
    /// <summary>Supplies uGUI and TMP text fields with Input System IME state.</summary>
    [DisallowMultipleComponent]
    public sealed class InputSystemTextInput : BaseInput
    {
        private Keyboard _keyboard;
        private string _composition = string.Empty;
        private IMECompositionMode _compositionMode = IMECompositionMode.Auto;
        private Vector2 _cursorPosition;

        public override string compositionString
        {
            get { BindKeyboard(); return _composition; }
        }

        public override IMECompositionMode imeCompositionMode
        {
            get => _compositionMode;
            set
            {
                _compositionMode = value;
                BindKeyboard();
                _keyboard?.SetIMEEnabled(value != IMECompositionMode.Off);
            }
        }

        public override Vector2 compositionCursorPos
        {
            get => _cursorPosition;
            set { _cursorPosition = value; BindKeyboard(); _keyboard?.SetIMECursorPosition(value); }
        }

        public override bool touchSupported => Touchscreen.current != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            AttachToModules();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachToModules();

        private static void AttachToModules()
        {
            foreach (var module in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Attach(module);
        }

        public static void Attach(InputSystemUIInputModule module)
        {
            if (module.inputOverride != null) return;
            var adapter = module.GetComponent<InputSystemTextInput>();
            module.inputOverride = adapter != null ? adapter : module.gameObject.AddComponent<InputSystemTextInput>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BindKeyboard();
        }

        protected override void OnDisable()
        {
            if (_keyboard != null) _keyboard.onIMECompositionChange -= OnCompositionChanged;
            _keyboard = null;
            _composition = string.Empty;
            base.OnDisable();
        }

        private void BindKeyboard()
        {
            if (_keyboard == Keyboard.current) return;
            if (_keyboard != null) _keyboard.onIMECompositionChange -= OnCompositionChanged;
            _keyboard = Keyboard.current;
            _composition = string.Empty;
            if (_keyboard == null) return;
            _keyboard.onIMECompositionChange += OnCompositionChanged;
            _keyboard.SetIMEEnabled(_compositionMode != IMECompositionMode.Off);
            _keyboard.SetIMECursorPosition(_cursorPosition);
        }

        private void OnCompositionChanged(IMECompositionString composition) => _composition = composition.ToString();
    }
}
