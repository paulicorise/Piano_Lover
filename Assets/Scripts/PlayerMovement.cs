using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float jumpSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveSpeed * moveInput.x, rb.linearVelocity.y);
    }

    public void Jump()
    {
        // One launch per correct chord, without stacking upward speed.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
