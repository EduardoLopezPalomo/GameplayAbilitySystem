using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using supercachola.AbilitySystem.Tags;

namespace supercachola.AbilitySystem.Input
{
    /// <summary>Links an input action to an ability.</summary>
    [Serializable]
    public struct AbilityInputBinding
    {
        [SerializeField] private InputActionReference action;
        [SerializeField] private GameplayTag ability;

        public InputActionReference Action => action;
        public GameplayTag Ability => ability;
    }

    /// <summary>
    /// Activates abilities when input actions are performed. No code needed.
    /// </summary>
    [AddComponentMenu("Ability System/Ability Input Binder")]
    public class AbilityInputBinder : MonoBehaviour
    {
        [Tooltip("If empty, it is searched in this GameObject and its parents.")]
        [SerializeField] private AbilitySystemComponent target;

        [SerializeField] private List<AbilityInputBinding> bindings = new List<AbilityInputBinding>();

        // Actions this component had to enable, so it only disables those.
        private readonly List<InputAction> enabledByUs = new List<InputAction>();

        private void Awake()
        {
            if (target == null)
                target = GetComponentInParent<AbilitySystemComponent>();
        }

        private void OnEnable()
        {
            if (target == null)
            {
                Debug.LogWarning($"AbilityInputBinder '{name}': no AbilitySystemComponent found.", this);
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                InputAction action = GetAction(bindings[i]);
                if (action == null)
                {
                    Debug.LogWarning($"AbilityInputBinder '{name}': binding {i} has no input action.", this);
                    continue;
                }

                action.performed += OnPerformed;

                if (!action.enabled)
                {
                    action.Enable();
                    enabledByUs.Add(action);
                }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                InputAction action = GetAction(bindings[i]);
                if (action != null)
                    action.performed -= OnPerformed;
            }

            for (int i = 0; i < enabledByUs.Count; i++)
                enabledByUs[i].Disable();

            enabledByUs.Clear();
        }

        // One handler for every binding: it checks which action fired.
        private void OnPerformed(InputAction.CallbackContext context)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                InputAction action = GetAction(bindings[i]);
                if (action == context.action)
                    target.TryActivateAbility(bindings[i].Ability);
            }
        }

        private static InputAction GetAction(AbilityInputBinding binding)
        {
            return binding.Action != null ? binding.Action.action : null;
        }
    }
}
