using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private InteractionManager manager;

    private void Start()
    {
        if (manager == null) manager = FindObjectOfType<InteractionManager>();
        promptPanel?.SetActive(false);
    }

    private void Update()
    {
        var target = manager.CurrentTarget;
        if (target != null)
        {
            promptPanel?.SetActive(true);
            promptText.text = $"[E] {target.PromptText}";
            
        }
        else
        {
            promptPanel?.SetActive(false);
        }
    }
}