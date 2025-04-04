using UnityEngine;

public class FlyCam : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public float boostMultiplier = 2f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    public bool ShowFPS;
    void Start()
    {
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Mouse Look
        yaw += lookSpeed * Input.GetAxis("Mouse X");
        pitch -= lookSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -90f, 90f); // Prevent flipping
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

        // Movement Input
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f);
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // Up and Down movement with Space and Ctrl
        if (Input.GetKey(KeyCode.Space))
            move.y += 1;
        if (Input.GetKey(KeyCode.LeftControl))
            move.y -= 1;

        // Move the object
        transform.Translate(move * speed * Time.deltaTime, Space.Self);

        // Unlock cursor with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnGUI()
    {
        if(ShowFPS) GUI.Label(new Rect(0, 0, 200, 32), "FPS: " + Mathf.RoundToInt(1 / Time.smoothDeltaTime));
    }
}
