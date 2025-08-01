using UnityEngine;
using System.Collections; // Nécessaire pour les Coroutines

public class NewReactive : MonoBehaviour
{
    [Header("Paramètres du Réactif")]
    [Tooltip("Le Tag du joueur (e.g., 'Player'). Assurez-vous que votre joueur a ce Tag.")]
    public string playerTag = "Player";
    [Tooltip("Le matériau à appliquer au joueur pour le rendre lumineux (brillant).")]
    public Material luminousMaterial;
    [Tooltip("Le matériau d'origine du joueur, pour le réinitialiser après l'effet.")]
    public Material originalPlayerMaterial;
    [Tooltip("La durée pendant laquelle le joueur reste lumineux (matériau + lumière) après contact avec le réactif. Mettez 0 pour une lumière permanente.")]
    public float luminousDuration = 5f;

    [Header("Paramètres de la Lumière Emise par le Joueur")]
    [Tooltip("La couleur de la lumière émise par le joueur.")]
    public Color lightColor = Color.yellow;
    [Tooltip("L'intensité maximale de la lumière émise par le joueur.")]
    public float lightIntensity = 2f;
    [Tooltip("La portée de la lumière émise par le joueur.")]
    public float lightRange = 10f;
    [Tooltip("Type de lumière à utiliser (Point est généralement bon pour un joueur mobile).")]
    public LightType lightType = LightType.Point;
    [Tooltip("Vitesse à laquelle l'intensité de la lumière augmente/diminue (smoothness du fade).")]
    public float lightFadeSpeed = 5f;

    [Header("Effets Visuels du Réactif (Optionnel)")]
    [Tooltip("Désactive le réactif après qu'il ait été utilisé une fois.")]
    public bool deactivateOnUse = true;

    private Renderer playerRenderer; // Référence au Renderer du joueur
    private Light playerLight;       // Référence au composant Light du joueur

    void OnTriggerEnter(Collider other)
    {
        // Vérifie si l'objet qui entre en contact a le Tag du joueur.
        if (other.CompareTag(playerTag))
        {
           
            // Tente d'obtenir le composant Renderer du joueur.
            playerRenderer = other.GetComponent<Renderer>();

            // Tente d'obtenir le composant Light du joueur. S'il n'existe pas, l'ajoute.
            playerLight = other.GetComponent<Light>();
            if (playerLight == null)
            {
                playerLight = other.gameObject.AddComponent<Light>();
            }

            // Si le joueur a un Renderer et que nous avons un matériau lumineux assigné...
            if (playerRenderer != null && luminousMaterial != null)
            {
                // Sauvegarde le matériau actuel du joueur avant de le changer.
                if (originalPlayerMaterial == null)
                {
                    originalPlayerMaterial = playerRenderer.material;
                }
                
                // Applique le matériau lumineux au joueur.
                playerRenderer.material = luminousMaterial;

                // Configure et active la lumière sur le joueur.
                SetupPlayerLight(playerLight);
                
                Debug.Log(other.name + " est maintenant lumineux et éclaire !");

                // Si une durée est spécifiée, lance une coroutine pour désactiver la luminosité et la lumière.
                if (luminousDuration > 0)
                {
                    StartCoroutine(ResetLuminosityAndLightAfterDelay(playerRenderer, playerLight, luminousDuration));
                }

                // Si le réactif doit être désactivé après utilisation, on le fait.
                if (deactivateOnUse)
                {
                    gameObject.SetActive(false); // Désactive le GameObject du réactif.
                }
            }
            else
            {
                if (playerRenderer == null)
                    Debug.LogWarning("Le joueur n'a pas de composant Renderer. Le matériau lumineux ne peut pas être appliqué.");
                if (luminousMaterial == null)
                    Debug.LogWarning("Le 'Luminous Material' n'est pas assigné sur le script NewReactive de " + gameObject.name);
            }
        }
    }

    // Configure les propriétés de la lumière du joueur et lance le fade-in.
    void SetupPlayerLight(Light lightComponent)
    {
        if (lightComponent == null) return;

        lightComponent.type = lightType;
        lightComponent.color = lightColor;
        lightComponent.range = lightRange;
        lightComponent.intensity = 30f; // Commence à 0 pour le fade-in
        lightComponent.enabled = true; // S'assure que la lumière est activée

        StartCoroutine(FadeLightIntensity(lightComponent, lightIntensity, lightFadeSpeed));
    }

    // Coroutine pour réinitialiser la luminosité du matériau et la lumière du joueur après un délai.
    System.Collections.IEnumerator ResetLuminosityAndLightAfterDelay(Renderer rendererToReset, Light lightToReset, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Si le Renderer existe toujours et que son matériau est toujours le matériau lumineux.
        if (rendererToReset != null && rendererToReset.material == luminousMaterial)
        {
            rendererToReset.material = originalPlayerMaterial;
        }

        // Fait disparaître la lumière progressivement avant de la désactiver.
        if (lightToReset != null)
        {
            StartCoroutine(FadeLightIntensity(lightToReset, 0f, lightFadeSpeed, () => {
                // Désactive la lumière une fois que son intensité est à 0.
                if (lightToReset != null) lightToReset.enabled = false;
            }));
        }
        
        Debug.Log(rendererToReset.gameObject.name + " n'est plus lumineux.");
    }

    // Coroutine pour faire varier l'intensité de la lumière progressivement.
    System.Collections.IEnumerator FadeLightIntensity(Light lightToFade, float targetIntensity, float speed, System.Action onComplete = null)
    {
        float currentIntensity = lightToFade.intensity;
        float startTime = Time.time;
        // Calculer la durée nécessaire pour atteindre la cible en fonction de la vitesse
        float duration = Mathf.Abs(targetIntensity - currentIntensity) / speed; 
        if (duration == 0) duration = 0.01f; // Éviter division par zéro si intensité = cible

        while (Time.time < startTime + duration)
        {
            if (lightToFade == null) yield break; // Gérer le cas où l'objet serait détruit pendant le fade
            float t = (Time.time - startTime) / duration;
            lightToFade.intensity = Mathf.Lerp(currentIntensity, targetIntensity, t);
            yield return null;
        }

        if (lightToFade != null)
        {
            lightToFade.intensity = targetIntensity; // Assurer la valeur finale exacte
        }
        onComplete?.Invoke(); // Appeler l'action de complétion si elle existe
    }

    // Permet de réinitialiser manuellement le matériau du joueur (utile pour le débogage ou d'autres scripts).
    public void ResetPlayerMaterial(GameObject player)
    {
        Renderer playerRend = player.GetComponent<Renderer>();
        if (playerRend != null && originalPlayerMaterial != null)
        {
            playerRend.material = originalPlayerMaterial;
        }
    }
}