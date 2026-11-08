using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CarSales
{
    /// <summary>
    /// Resolves input actions safely. If an InputActionReference is missing or broken (for example after the
    /// .inputactions file was replaced and got a new GUID), built-in default bindings are used instead of crashing.
    /// </summary>
    public static class CarSalesInput
    {
        static Dictionary<string, InputAction> fallback;
        static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>Returns the referenced action, or a built-in default with the same name. Always enabled.</summary>
        public static InputAction Resolve(InputActionReference reference, string actionName)
        {
            InputAction action = null;
            if (reference != null)
            {
                try { action = reference.action; } catch { action = null; }
            }
            if (action == null)
            {
                action = Fallback(actionName);
                if (action != null && warned.Add(actionName))
                    Debug.LogWarning($"[CarSales] Input action '{actionName}' is not linked - using built-in default keys. " +
                                     "Open this scene and run Tools > Car Sales > Re-link Input Actions (or 2. Build Everything).");
            }
            if (action != null && !action.enabled) action.Enable();
            return action;
        }

        static InputAction Fallback(string name)
        {
            if (fallback == null) BuildFallback();
            if (fallback.TryGetValue(name, out var a)) return a;
            Debug.LogError($"[CarSales] Unknown input action '{name}'.");
            return null;
        }

        static void BuildFallback()
        {
            fallback = new Dictionary<string, InputAction>();

            var move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            fallback["Move"] = move;

            var look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlType: "Vector2");
            look.AddBinding("<Gamepad>/rightStick", processors: "scaleVector2(x=12,y=8)");
            fallback["Look"] = look;

            fallback["Jump"] = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            fallback["Sprint"] = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            fallback["Interact"] = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            fallback["ToggleShop"] = Button("ToggleShop", "<Keyboard>/o", "<Gamepad>/select");
            fallback["CloseMenu"] = Button("CloseMenu", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            fallback["Inventory"] = Button("Inventory", "<Keyboard>/tab", "<Gamepad>/dpad/left");
            fallback["Vehicle"] = Button("Vehicle", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            fallback["Map"] = Button("Map", "<Keyboard>/m", "<Gamepad>/dpad/up");
            fallback["Phone"] = Button("Phone", "<Keyboard>/p", "<Gamepad>/dpad/down");
            fallback["CameraView"] = Button("CameraView", "<Keyboard>/c", "<Gamepad>/rightStickPress");
            fallback["GearUp"] = Button("GearUp", "<Keyboard>/leftShift", "<Gamepad>/rightShoulder");
            fallback["GearDown"] = Button("GearDown", "<Keyboard>/leftCtrl", "<Gamepad>/leftShoulder");
        }

        static InputAction Button(string name, string keyboard, string gamepad)
        {
            var a = new InputAction(name, InputActionType.Button, keyboard);
            a.AddBinding(gamepad);
            return a;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (fallback != null) foreach (var a in fallback.Values) a.Dispose();
            fallback = null;
            warned.Clear();
        }
    }
}
