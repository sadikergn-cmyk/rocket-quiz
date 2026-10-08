using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveForce = 14f;
    [SerializeField] private float jumpImpulse = 7.5f;
    [SerializeField] private float fallLimitY = -10f;

    private Rigidbody body;
    private Vector3 startPosition;
    private bool grounded;
    private bool jumpQueued;

    public int RespawnCount { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && grounded)
        {
            jumpQueued = true;
        }

        if (transform.position.y < fallLimitY)
        {
            Respawn();
        }
    }

    private void FixedUpdate()
    {
        Vector2 input = ReadMoveInput();
        if (input.sqrMagnitude > 0f)
        {
            body.AddForce(new Vector3(input.x, 0f, input.y) * moveForce);
        }

        if (jumpQueued)
        {
            jumpQueued = false;
            grounded = false;
            body.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        }
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    public void Respawn()
    {
        RespawnCount++;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = startPosition;
        transform.position = startPosition;
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                grounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        grounded = false;
    }
}
