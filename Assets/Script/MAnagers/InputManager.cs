using UnityEngine;
using UnityEngine.InputSystem;

public enum AttackType
{
    Single,
    Spread
}

public class InputManager : MonoBehaviour
{
    public static Vector2 Movement { get; private set; } = Vector2.zero;
    public static bool IsFire { get; private set; } = false;
    public static AttackType CurrentAttackType { get; private set; } = AttackType.Single;

    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;
    private InputAction fireAction;
    private InputAction previousAction;
    private InputAction nextAction;
    
    private void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        fireAction = playerMap.FindAction("Attack");
        previousAction = playerMap.FindAction("Previous");
        nextAction = playerMap.FindAction("Next");

        playerMap.Enable();
    }

    private void Update()
    {
        Movement = moveAction.ReadValue<Vector2>();
        IsFire = fireAction.IsPressed();

        // 공격 패턴 변경 (GameManager에서 숫자키로 패턴 변경 사용 중 — 임시 비활성화)
        // if (previousAction.WasPressedThisFrame())
        //     CurrentAttackType = AttackType.Single;

        // if (nextAction.WasPressedThisFrame())
        //     CurrentAttackType = AttackType.Spread;
    }
}
