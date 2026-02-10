using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Représente un slot de sauvegarde dans l'UI
/// Affiche les infos de la save et permet de la charger/supprimer
/// Le slot entier est cliquable (pas besoin de bouton séparé)
/// </summary>
public class SaveSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [Tooltip("Texte nom de la save")]
    [SerializeField] private TextMeshProUGUI saveNameText;

    [Tooltip("Texte jour actuel")]
    [SerializeField] private TextMeshProUGUI dayText;

    [Tooltip("Texte score")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Texte date de sauvegarde")]
    [SerializeField] private TextMeshProUGUI dateText;

    [Tooltip("Bouton pour charger/sélectionner ce slot")]
    [SerializeField] private Button loadButton;

    [Tooltip("Bouton pour supprimer ce slot (optionnel)")]
    [SerializeField] private Button deleteButton;

    [Header("Empty Slot")]
    [Tooltip("GameObject affiché quand le slot est vide")]
    [SerializeField] private GameObject emptySlotIndicator;

    [Tooltip("GameObject affiché quand le slot contient une save")]
    [SerializeField] private GameObject usedSlotContent;

    // Données
    private int slotIndex;
    private SaveData slotData;
    private bool isEmpty;

    // Callbacks
    private System.Action<int> onSelectCallback;
    private System.Action<int> onDeleteCallback;

    private void Awake()
    {
        // Rechercher les éléments par nom
        FindUIElements();

        // S'assurer qu'on a un composant Image pour recevoir les clics
        Image img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Transparent mais cliquable
        }

        // Le Raycast Target doit être activé pour recevoir les clics
        img.raycastTarget = true;

        // Setup bouton load si présent
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OnLoadClicked);
        }

        // Setup bouton delete si présent
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
    }

    private void OnLoadClicked()
    {
        Debug.Log($"[SaveSlotUI] Load button clicked for slot {slotIndex}");
        onSelectCallback?.Invoke(slotIndex);
    }

    private void FindUIElements()
    {
        // Trouver emptySlotIndicator
        if (emptySlotIndicator == null)
        {
            Transform t = transform.Find("EmptySlotIndicator") ?? transform.Find("EmptySlot") ?? transform.Find("Empty");
            if (t != null) emptySlotIndicator = t.gameObject;
        }

        // Trouver usedSlotContent
        if (usedSlotContent == null)
        {
            Transform t = transform.Find("UsedSlotContent") ?? transform.Find("UsedSlot") ?? transform.Find("Content");
            if (t != null) usedSlotContent = t.gameObject;
        }

        // Trouver les textes
        foreach (TextMeshProUGUI txt in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (saveNameText == null && (txt.name.Contains("Name") || txt.name.Contains("Save")))
                saveNameText = txt;
            else if (dayText == null && txt.name.Contains("Day"))
                dayText = txt;
            else if (scoreText == null && txt.name.Contains("Score"))
                scoreText = txt;
            else if (dateText == null && txt.name.Contains("Date"))
                dateText = txt;
        }

        // Trouver les boutons
        foreach (Button btn in GetComponentsInChildren<Button>(true))
        {
            string btnName = btn.name.ToLower();
            if (loadButton == null && (btnName.Contains("load") || btnName.Contains("select") || btnName.Contains("confirm")))
            {
                loadButton = btn;
            }
            else if (deleteButton == null && (btnName.Contains("delete") || btnName.Contains("remove") || btnName.Contains("cancel")))
            {
                deleteButton = btn;
            }
        }
    }

    /// <summary>
    /// Appelé quand on clique n'importe où sur le slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ne pas réagir si on a cliqué sur le bouton delete
        if (deleteButton != null && eventData.pointerCurrentRaycast.gameObject == deleteButton.gameObject)
            return;

        Debug.Log($"[SaveSlotUI] Slot {slotIndex} cliqué (isEmpty: {isEmpty})");
        onSelectCallback?.Invoke(slotIndex);
    }

    /// <summary>
    /// Configure le slot avec les données d'une sauvegarde
    /// </summary>
    public void Setup(int index, SaveData data, System.Action<int> onSelect, System.Action<int> onDelete)
    {
        slotIndex = index;
        slotData = data;
        onSelectCallback = onSelect;
        onDeleteCallback = onDelete;

        isEmpty = (data == null || !data.isUsed);

        RefreshDisplay();
    }

    /// <summary>
    /// Rafraîchit l'affichage du slot
    /// </summary>
    private void RefreshDisplay()
    {
        if (isEmpty)
        {
            ShowEmptySlot();
        }
        else
        {
            ShowUsedSlot();
        }
    }

    /// <summary>
    /// Affiche le slot comme vide
    /// </summary>
    private void ShowEmptySlot()
    {
        // Afficher indicateur de slot vide
        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(true);

        // Cacher contenu de save
        if (usedSlotContent != null)
            usedSlotContent.SetActive(false);

        // Désactiver les boutons pour slot vide
        if (loadButton != null)
            loadButton.gameObject.SetActive(false);
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Affiche le slot avec les données de la save
    /// </summary>
    private void ShowUsedSlot()
    {
        // Cacher indicateur de slot vide
        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(false);

        // Afficher contenu de save
        if (usedSlotContent != null)
            usedSlotContent.SetActive(true);

        // Activer les boutons
        if (loadButton != null)
            loadButton.gameObject.SetActive(true);
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(true);

        // Afficher les infos
        if (saveNameText != null)
            saveNameText.text = slotData.saveName;

        if (dayText != null)
            dayText.text = slotData.GetCurrentDayName();

        if (scoreText != null)
            scoreText.text = $"Score: {slotData.GetAverageScore():F1}/20";

        if (dateText != null)
            dateText.text = $"Last saved: {slotData.lastSaveDate}";
    }

    /// <summary>
    /// Appelé quand on clique sur le bouton delete
    /// </summary>
    private void OnDeleteClicked()
    {
        onDeleteCallback?.Invoke(slotIndex);
    }

    /// <summary>
    /// Retourne si ce slot est vide
    /// </summary>
    public bool IsEmpty()
    {
        return isEmpty;
    }

    /// <summary>
    /// Retourne l'index de ce slot
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }
}
