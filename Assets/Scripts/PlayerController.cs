using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private PlayerInput controls;
    private CharacterController controller;

    private Vector2 moveInput;

    void Awake()
    {
        controls = new PlayerInput();
        controller = GetComponent<CharacterController>();

        controls.PlayerMap.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.PlayerMap.Move.canceled += _ => moveInput = Vector2.zero;
    }

    void Update()
    {
        //if (MapManager.isMapOpen) return;
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        controller.Move(move * moveSpeed * Time.deltaTime);
    }
    private void OnEnable()
    {
        controls.PlayerMap.Enable();
    }

    private void OnDisable()
    {
        controls.PlayerMap.Disable();
    }
}
