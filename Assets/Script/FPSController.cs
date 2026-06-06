using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6.0f;
    public float runSpeed = 10.0f;
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    [Tooltip("�Ч� Player ���U�� CameraHolder �Ū����i��")]
    public Transform cameraHolder;
    public float mouseSensitivity = 2.0f;
    public float lookXLimit = 85.0f;

    [Header("Interaction")]
    public PhysicsGrabber grabber;

    // ==========================================
    // �i�ק�j�G�w��u�����ġv���}�B�n�t�γ]�w
    // ==========================================
    [Header("Audio Settings (�}�B�n - �����Ī�)")]
    [Tooltip("�Щ즲���a���W�� AudioSource �i��")]
    public AudioSource footstepAudioSource;

    [Tooltip("������������ (�i�H�� 1~2 �������H���D�］��)")]
    public AudioClip[] walkSounds;

    [Tooltip("�]�B�������� (�i�H�� 1~2 �������H���D�］��)")]
    public AudioClip[] runSounds;

    // �ΨӰO���e�@�V�O���O�b�]�B�A�p�G���A�����N�n������
    private bool wasRunning = false;
    // ==========================================

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;
    private bool isPaused = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraHolder == null)
        {
            Transform foundHolder = transform.Find("CameraHolder");
            if (foundHolder != null)
            {
                cameraHolder = foundHolder;
            }
            else
            {
                Debug.LogError("<color=red>[FPSController]</color> �䤣�� CameraHolder�I");
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isPaused) return;

        // --- �B�z�ƹ����� (Look Logic) ---
        if (canMove && cameraHolder != null)
        {
            if (grabber == null || !grabber.isInspecting)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                rotationX -= mouseY;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                cameraHolder.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, mouseX, 0);
            }
        }

        // --- �B�z���� (Movement Logic) ---
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        float movementDirectionY = moveDirection.y;
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // --- �B�z���D�P���O (Jump & Gravity) ---
        if (characterController.isGrounded)
        {
            moveDirection.y = -0.5f;
            if (canMove && Input.GetButtonDown("Jump"))
            {
                moveDirection.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }
        }
        else
        {
            moveDirection.y = movementDirectionY + (gravity * Time.deltaTime);
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // �C�V�I�s�B�z�}�B�n����k
        HandleFootsteps(isRunning);
    }

    // ==========================================
    // �i�ק�j�G�s������ļ����޿�֤�
    // ==========================================
    private void HandleFootsteps(bool isRunning)
    {
        // �p�G���a���b�a�W�B����ʡB�ιC���Ȱ��A�N�j����n���d��
        if (!characterController.isGrounded || !canMove || isPaused)
        {
            if (footstepAudioSource.isPlaying) footstepAudioSource.Stop();
            return;
        }

        // ���o���a��ڪ��u�������ʳt�סv
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // �p�G�u�����b����
        if (currentSpeed > 0.1f)
        {
            // �p�G�n���S�b���A�Ϊ̪��a�q�u�����v�������u�]�B�v(�ΤϹL��)
            if (!footstepAudioSource.isPlaying || wasRunning != isRunning)
            {
                PlayContinuousSound(isRunning);
                wasRunning = isRunning;
            }
        }
        else
        {
            // �p�G���a���U�ӤF�A�ӥB�n���٦b���A�ߨ�j��I�_����I
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }
    }

    private void PlayContinuousSound(bool isRunning)
    {
        if (footstepAudioSource == null) return;

        // �M�w�n�Ψ����}�C�٬O�]�B�}�C
        AudioClip[] clips = isRunning ? runSounds : walkSounds;
        if (clips.Length == 0) return;

        // �H����@�ӭ���
        int randomIndex = Random.Range(0, clips.Length);

        // �]�w�����ݩʨö}�l����
        footstepAudioSource.clip = clips[randomIndex];
        footstepAudioSource.loop = true; // �i����j�G�������Ħ۰ʵL���`��
        footstepAudioSource.pitch = Random.Range(0.95f, 1.05f); // �L�L���ܭ��աA�קKťı�h��
        footstepAudioSource.Play();
    }
    // ==========================================

    void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canMove = false;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            canMove = true;
        }
    }
}