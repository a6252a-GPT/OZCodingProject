using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] PlayerAnmation animationController;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        moveInput = InputManager.Movement;
        UpdateAnimation();
    }
    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
    private void UpdateAnimation()
    {
        int moveState = 0;
        if(moveInput.x > 0.1f)
        {
            moveState = 1;
        }
        else if(moveInput.x < -0.1f)
        {
            moveState = -1;
        }
        animationController.SetMoveState(moveState);

    }
}
