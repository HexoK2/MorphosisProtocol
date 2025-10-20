using UnityEngine;

public class AnimatedFloorButton : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private GameObject door;
    [SerializeField] private Transform buttonTop; // La partie supérieure du bouton qui s'enfonce

    [Header("Animation du Bouton")]
    [SerializeField] private float buttonPressDepth = 0.1f;
    [SerializeField] private float buttonAnimSpeed = 5f;

    [Header("Animation de la Porte")]
    [SerializeField] private bool doorSlideUp = true; // true = monte, false = s'ouvre sur le côté
    [SerializeField] private float doorMoveDistance = 4f;
    [SerializeField] private float doorAnimSpeed = 2f;
    [SerializeField] private AnimationCurve doorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Effets Visuels")]
    [SerializeField] private Material activatedMaterial;
    [SerializeField] private GameObject activationEffect; // Particules optionnelles
    [SerializeField] private AudioClip buttonSound;
    [SerializeField] private AudioClip doorSound;

    [Header("Comportement")]
    [SerializeField] private bool stayActivated = true; // Le bouton reste enfoncé
    [SerializeField] private float resetDelay = 3f; // Si stayActivated = false

    private Vector3 buttonInitialPos;
    private Vector3 buttonPressedPos;
    private Vector3 doorInitialPos;
    private Vector3 doorTargetPos;
    private bool isPressed = false;
    private bool isDoorMoving = false;
    private float doorAnimProgress = 0f;
    private Renderer buttonRenderer;
    private Material originalMaterial;
    private AudioSource audioSource;

    void Start()
    {
        // Initialiser les positions
        if (buttonTop != null)
        {
            buttonInitialPos = buttonTop.localPosition;
            buttonPressedPos = buttonInitialPos - Vector3.up * buttonPressDepth;
        }
        else
        {
            buttonTop = transform;
            buttonInitialPos = transform.localPosition;
            buttonPressedPos = buttonInitialPos - Vector3.up * buttonPressDepth;
        }

        if (door != null)
        {
            doorInitialPos = door.transform.position;

            if (doorSlideUp)
            {
                doorTargetPos = doorInitialPos + Vector3.up * doorMoveDistance;
            }
            else
            {
                // Ouvre sur le côté (rotation)
                doorTargetPos = doorInitialPos + door.transform.right * doorMoveDistance;
            }
        }

        // Matériaux
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
        {
            originalMaterial = buttonRenderer.material;
        }

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Animer le bouton
        if (buttonTop != null)
        {
            Vector3 targetPos = isPressed ? buttonPressedPos : buttonInitialPos;
            buttonTop.localPosition = Vector3.Lerp(
                buttonTop.localPosition,
                targetPos,
                Time.deltaTime * buttonAnimSpeed
            );
        }

        // Animer la porte
        if (isDoorMoving && door != null)
        {
            doorAnimProgress += Time.deltaTime * doorAnimSpeed;
            float curveValue = doorCurve.Evaluate(Mathf.Clamp01(doorAnimProgress));

            door.transform.position = Vector3.Lerp(
                doorInitialPos,
                doorTargetPos,
                curveValue
            );

            if (doorAnimProgress >= 1f)
            {
                isDoorMoving = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPressed)
        {
            ActivateButton();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !stayActivated)
        {
            Invoke("DeactivateButton", resetDelay);
        }
    }

    private void ActivateButton()
    {
        isPressed = true;
        isDoorMoving = true;
        doorAnimProgress = 0f;

        // Changer la couleur du bouton
        if (buttonRenderer != null && activatedMaterial != null)
        {
            buttonRenderer.material = activatedMaterial;
        }

        // Effet visuel
        if (activationEffect != null)
        {
            Instantiate(activationEffect, transform.position, Quaternion.identity);
        }

        // Sons
        if (buttonSound != null)
        {
            audioSource.PlayOneShot(buttonSound);
        }

        if (doorSound != null)
        {
            audioSource.PlayOneShot(doorSound, 0.7f);
        }

        Debug.Log("🔘 Bouton activé - Porte en mouvement !");
    }

    private void DeactivateButton()
    {
        isPressed = false;

        // Restaurer le matériau original
        if (buttonRenderer != null && originalMaterial != null)
        {
            buttonRenderer.material = originalMaterial;
        }

        // Réinitialiser la porte
        if (door != null)
        {
            doorAnimProgress = 0f;
            isDoorMoving = true;
            Vector3 temp = doorInitialPos;
            doorInitialPos = doorTargetPos;
            doorTargetPos = temp;
        }

        Debug.Log("🔘 Bouton désactivé - Porte se referme !");
    }

    // Pour activer le bouton depuis un autre script
    public void ForceActivate()
    {
        if (!isPressed)
        {
            ActivateButton();
        }
    }
}