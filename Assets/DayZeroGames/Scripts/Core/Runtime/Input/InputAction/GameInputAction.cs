










using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;


























































public partial class @GameInputAction: IInputActionCollection2, IDisposable
{
    
    
    
    public InputActionAsset asset { get; }

    
    
    
    public @GameInputAction()
    {
        asset = InputActionAsset.FromJson(@"{
    ""version"": 1,
    ""name"": ""GameInputAction"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""35d669ff-224f-41bf-af70-f04213ccac09"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""f74cd87d-12f9-46bd-aab2-d4a806245bb5"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""dbcae0a7-a8bc-42af-8707-d1d9dae71416"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""38fba1d7-2bbe-4816-8509-c49cf78ba83c"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""1D Axis"",
                    ""id"": ""a24e4380-8592-49cb-adcc-db454702ea72"",
                    ""path"": ""1DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""ea672032-5ce7-4074-b570-36335d4b66ce"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""b6deb960-a781-4b43-bbeb-07b4b6076fef"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        
        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_Move = m_Player.FindAction("Move", throwIfNotFound: true);
        m_Player_Jump = m_Player.FindAction("Jump", throwIfNotFound: true);
    }

    ~@GameInputAction()
    {
        UnityEngine.Debug.Assert(!m_Player.enabled, "This will cause a leak and performance issues, GameInputAction.Player.Disable() has not been called.");
    }

    
    
    
    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    
    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    
    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    
    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    
    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    
    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    
    public void Enable()
    {
        asset.Enable();
    }

    
    public void Disable()
    {
        asset.Disable();
    }

    
    public IEnumerable<InputBinding> bindings => asset.bindings;

    
    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    
    private readonly InputActionMap m_Player;
    private List<IPlayerActions> m_PlayerActionsCallbackInterfaces = new List<IPlayerActions>();
    private readonly InputAction m_Player_Move;
    private readonly InputAction m_Player_Jump;
    
    
    
    public struct PlayerActions
    {
        private @GameInputAction m_Wrapper;

        
        
        
        public PlayerActions(@GameInputAction wrapper) { m_Wrapper = wrapper; }
        
        
        
        public InputAction @Move => m_Wrapper.m_Player_Move;
        
        
        
        public InputAction @Jump => m_Wrapper.m_Player_Jump;
        
        
        
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        
        public void Enable() { Get().Enable(); }
        
        public void Disable() { Get().Disable(); }
        
        public bool enabled => Get().enabled;
        
        
        
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        
        
        
        
        
        
        
        
        public void AddCallbacks(IPlayerActions instance)
        {
            if (instance == null || m_Wrapper.m_PlayerActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Add(instance);
            @Move.started += instance.OnMove;
            @Move.performed += instance.OnMove;
            @Move.canceled += instance.OnMove;
            @Jump.started += instance.OnJump;
            @Jump.performed += instance.OnJump;
            @Jump.canceled += instance.OnJump;
        }

        
        
        
        
        
        
        
        private void UnregisterCallbacks(IPlayerActions instance)
        {
            @Move.started -= instance.OnMove;
            @Move.performed -= instance.OnMove;
            @Move.canceled -= instance.OnMove;
            @Jump.started -= instance.OnJump;
            @Jump.performed -= instance.OnJump;
            @Jump.canceled -= instance.OnJump;
        }

        
        
        
        
        public void RemoveCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        
        
        
        
        
        
        
        
        
        public void SetCallbacks(IPlayerActions instance)
        {
            foreach (var item in m_Wrapper.m_PlayerActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    
    
    
    public PlayerActions @Player => new PlayerActions(this);
    
    
    
    
    
    public interface IPlayerActions
    {
        
        
        
        
        
        
        void OnMove(InputAction.CallbackContext context);
        
        
        
        
        
        
        void OnJump(InputAction.CallbackContext context);
    }
}
