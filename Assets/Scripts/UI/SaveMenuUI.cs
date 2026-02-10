using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Menu de gestion des sauvegardes (3 slots)
/// Utilisé pour New Game et Continue
/// </summary>
public class SaveMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel du menu de sauvegarde")]
    [SerializeField] private GameObject saveMenuPanel;

    [Tooltip("Texte titre du menu")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Container pour les slots (Vertical Layout Group)")]
    [SerializeField] private Transform slotsContainer;

    [Tooltip("Prefab SaveSlotUI")]
    [SerializeField] private GameObject saveSlotPrefab;

    [Tooltip("Bouton retour")]
    [SerializeField] private Button backButton;

    [Header("Popup Name Input")]
    [Tooltip("Panel popup pour entrer le nom de la save")]
    [SerializeField] private GameObject nameInputPopup;

    [Tooltip("Input field pour le nom")]
    [SerializeField] private TMP_InputField nameInputField;

    [Tooltip("Bouton confirmer nom")]
    [SerializeField] private Button confirmNameButton;

    [Tooltip("Bouton annuler nom")]
    [SerializeField] private Button cancelNameButton;

    [Header("Confirmation Delete")]
    [Tooltip("Panel popup pour confirmer suppression")]
    [SerializeField] private GameObject deleteConfirmPopup;

    [Tooltip("Texte message de confirmation")]
    [SerializeField] private TextMeshProUGUI deleteConfirmText;

    [Tooltip("Bouton confirmer suppression")]
    [SerializeField] private Button confirmDeleteButton;

    [Tooltip("Bouton annuler suppression")]
    [SerializeField] private Button cancelDeleteButton;

    [Header("Scene Settings")]
    [Tooltip("Nom de la scène de jeu à charger")]
    [SerializeField] private string gameSceneName = "GameScene";

    // État
    private SaveMenuMode currentMode;
    private List<SaveSlotUI> slotUIList = new List<SaveSlotUI>();
    private int selectedSlotForDelete = -1;
    private int selectedSlotForNewGame = -1;
    private System.Action onBackCallback;

    private void Awake()
    {
        Debug.Log("[SaveMenuUI] === Awake ===");

        // TOUJOURS rechercher les popups pour éviter les références corrompues
        FindNameInputPopup();
        FindDeleteConfirmPopup();
        FindPopupButtons();

        // Setup boutons
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (confirmNameButton != null)
        {
            confirmNameButton.onClick.AddListener(OnConfirmNameClicked);
        }
        else
        {
            Debug.LogWarning("[SaveMenuUI] confirmNameButton est NULL!");
        }

        if (cancelNameButton != null)
        {
            cancelNameButton.onClick.AddListener(OnCancelNameClicked);
        }
        else
        {
            Debug.LogWarning("[SaveMenuUI] cancelNameButton est NULL!");
        }

        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.AddListener(OnConfirmDeleteClicked);
        }

        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.AddListener(OnCancelDeleteClicked);
        }

        // Cacher par défaut
        if (saveMenuPanel != null) saveMenuPanel.SetActive(false);
        if (nameInputPopup != null)
        {
            nameInputPopup.SetActive(false);
            Debug.Log("[SaveMenuUI] nameInputPopup désactivé par défaut");
        }
        else
        {
            Debug.LogError("[SaveMenuUI] ❌ nameInputPopup est NULL dans Awake!");
        }
        if (deleteConfirmPopup != null) deleteConfirmPopup.SetActive(false);
    }

    #region Find Popup References

    /// <summary>
    /// Recherche le popup NameInput dans la hiérarchie
    /// </summary>
    private void FindNameInputPopup()
    {
        // Chercher dans les enfants directs et récursifs
        string[] possibleNames = { "NameInputPopup", "PopupNameInput", "NamePopup", "InputPopup", "NewSavePopup" };

        foreach (string name in possibleNames)
        {
            // Chercher enfant direct
            Transform found = transform.Find(name);
            if (found != null)
            {
                nameInputPopup = found.gameObject;
                Debug.Log($"[SaveMenuUI] ✅ nameInputPopup trouvé (direct): {name}");
                break;
            }

            // Chercher dans tout le panel
            if (saveMenuPanel != null)
            {
                found = saveMenuPanel.transform.Find(name);
                if (found != null)
                {
                    nameInputPopup = found.gameObject;
                    Debug.Log($"[SaveMenuUI] ✅ nameInputPopup trouvé (dans panel): {name}");
                    break;
                }
            }
        }

        // Chercher récursivement si pas trouvé
        if (nameInputPopup == null)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name.Contains("NameInput") || child.name.Contains("PopupName") || child.name.Contains("NewSave"))
                {
                    // Vérifier que c'est un panel (a un Image ou CanvasGroup)
                    if (child.GetComponent<UnityEngine.UI.Image>() != null || child.GetComponent<CanvasGroup>() != null)
                    {
                        nameInputPopup = child.gameObject;
                        Debug.Log($"[SaveMenuUI] ✅ nameInputPopup trouvé (récursif): {child.name}");
                        break;
                    }
                }
            }
        }

        if (nameInputPopup == null)
        {
            Debug.LogError("[SaveMenuUI] ❌ nameInputPopup introuvable! Noms testés: NameInputPopup, PopupNameInput, NamePopup, etc.");
        }

        // Trouver l'InputField dans le popup
        if (nameInputPopup != null && nameInputField == null)
        {
            nameInputField = nameInputPopup.GetComponentInChildren<TMP_InputField>(true);
            if (nameInputField != null)
            {
                Debug.Log($"[SaveMenuUI] ✅ nameInputField trouvé: {nameInputField.name}");
            }
            else
            {
                Debug.LogError("[SaveMenuUI] ❌ nameInputField introuvable dans le popup!");
            }
        }
    }

    /// <summary>
    /// Recherche le popup DeleteConfirm dans la hiérarchie
    /// </summary>
    private void FindDeleteConfirmPopup()
    {
        string[] possibleNames = { "DeleteConfirmPopup", "ConfirmDeletePopup", "DeletePopup", "ConfirmPopup" };

        foreach (string name in possibleNames)
        {
            Transform found = transform.Find(name);
            if (found == null && saveMenuPanel != null)
            {
                found = saveMenuPanel.transform.Find(name);
            }

            if (found != null)
            {
                deleteConfirmPopup = found.gameObject;
                Debug.Log($"[SaveMenuUI] ✅ deleteConfirmPopup trouvé: {name}");

                // Chercher le texte de confirmation
                if (deleteConfirmText == null)
                {
                    deleteConfirmText = found.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                break;
            }
        }

        if (deleteConfirmPopup == null)
        {
            Debug.LogWarning("[SaveMenuUI] ⚠️ deleteConfirmPopup non trouvé (optionnel)");
        }
    }

    /// <summary>
    /// Recherche les boutons des popups
    /// </summary>
    private void FindPopupButtons()
    {
        // Boutons du popup NameInput
        if (nameInputPopup != null)
        {
            Button[] buttons = nameInputPopup.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                string btnName = btn.name.ToLower();
                if (confirmNameButton == null && (btnName.Contains("confirm") || btnName.Contains("ok") || btnName.Contains("yes") || btnName.Contains("create")))
                {
                    confirmNameButton = btn;
                    Debug.Log($"[SaveMenuUI] ✅ confirmNameButton trouvé: {btn.name}");
                }
                else if (cancelNameButton == null && (btnName.Contains("cancel") || btnName.Contains("back") || btnName.Contains("no") || btnName.Contains("close")))
                {
                    cancelNameButton = btn;
                    Debug.Log($"[SaveMenuUI] ✅ cancelNameButton trouvé: {btn.name}");
                }
            }
        }

        // Boutons du popup DeleteConfirm
        if (deleteConfirmPopup != null)
        {
            Button[] buttons = deleteConfirmPopup.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                string btnName = btn.name.ToLower();
                if (confirmDeleteButton == null && (btnName.Contains("confirm") || btnName.Contains("yes") || btnName.Contains("delete")))
                {
                    confirmDeleteButton = btn;
                    Debug.Log($"[SaveMenuUI] ✅ confirmDeleteButton trouvé: {btn.name}");
                }
                else if (cancelDeleteButton == null && (btnName.Contains("cancel") || btnName.Contains("no") || btnName.Contains("back")))
                {
                    cancelDeleteButton = btn;
                    Debug.Log($"[SaveMenuUI] ✅ cancelDeleteButton trouvé: {btn.name}");
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// Ouvre le menu en mode New Game
    /// </summary>
    public void OpenNewGameMenu(System.Action onBack = null)
    {
        currentMode = SaveMenuMode.NewGame;
        onBackCallback = onBack;

        if (titleText != null)
        {
            titleText.text = "New Game - Select Slot";
        }

        // Activer le slotsContainer AVANT de rafraîchir les slots
        if (slotsContainer != null && !slotsContainer.gameObject.activeSelf)
        {
            slotsContainer.gameObject.SetActive(true);
            Debug.Log("[SaveMenuUI] SlotsContainer activé");
        }

        RefreshSlots();

        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(true);
        }

        Debug.Log("[SaveMenuUI] Menu New Game ouvert");
    }

    /// <summary>
    /// Ouvre le menu en mode Continue
    /// </summary>
    public void OpenContinueMenu(System.Action onBack = null)
    {
        currentMode = SaveMenuMode.Continue;
        onBackCallback = onBack;

        if (titleText != null)
        {
            titleText.text = "Continue - Select Save";
        }

        // Activer le slotsContainer AVANT de rafraîchir les slots
        if (slotsContainer != null && !slotsContainer.gameObject.activeSelf)
        {
            slotsContainer.gameObject.SetActive(true);
            Debug.Log("[SaveMenuUI] SlotsContainer activé");
        }

        RefreshSlots();

        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(true);
        }

        Debug.Log("[SaveMenuUI] Menu Continue ouvert");
    }

    /// <summary>
    /// Ouvre le menu en mode Play (unifié - peut créer ou charger)
    /// </summary>
    public void OpenPlayMenu(System.Action onBack = null)
    {
        currentMode = SaveMenuMode.Play;
        onBackCallback = onBack;

        if (titleText != null)
        {
            titleText.text = "Select Save Slot";
        }

        // Activer le slotsContainer AVANT de rafraîchir les slots
        if (slotsContainer != null && !slotsContainer.gameObject.activeSelf)
        {
            slotsContainer.gameObject.SetActive(true);
            Debug.Log("[SaveMenuUI] SlotsContainer activé");
        }

        RefreshSlots();

        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(true);
        }

        Debug.Log("[SaveMenuUI] Menu Play ouvert");
    }

    /// <summary>
    /// Ferme le menu
    /// </summary>
    public void CloseMenu()
    {
        if (saveMenuPanel != null)
        {
            saveMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Rafraîchit l'affichage des slots
    /// </summary>
    private void RefreshSlots()
    {
        // Clear les slots existants
        foreach (SaveSlotUI slotUI in slotUIList)
        {
            if (slotUI != null)
            {
                Destroy(slotUI.gameObject);
            }
        }
        slotUIList.Clear();

        // Créer les slots
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[SaveMenuUI] SaveManager introuvable!");
            return;
        }

        int numberOfSlots = SaveManager.Instance.GetNumberOfSlots();

        for (int i = 0; i < numberOfSlots; i++)
        {
            CreateSlot(i);
        }
    }

    /// <summary>
    /// Crée un slot UI
    /// </summary>
    private void CreateSlot(int slotIndex)
    {
        if (saveSlotPrefab == null || slotsContainer == null)
        {
            Debug.LogError("[SaveMenuUI] Prefab ou container manquant!");
            return;
        }

        // Instancier le prefab
        GameObject slotObj = Instantiate(saveSlotPrefab, slotsContainer);
        SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();

        if (slotUI == null)
        {
            Debug.LogError("[SaveMenuUI] Prefab manque le component SaveSlotUI!");
            Destroy(slotObj);
            return;
        }

        // Récupérer les données du slot
        SaveData slotData = SaveManager.Instance.GetSlotInfo(slotIndex);

        // Setup le slot
        slotUI.Setup(slotIndex, slotData, OnSlotSelected, OnSlotDeleteRequested);

        slotUIList.Add(slotUI);
    }

    #region Slot Callbacks

    /// <summary>
    /// Appelé quand un slot est sélectionné
    /// </summary>
    private void OnSlotSelected(int slotIndex)
    {
        Debug.Log($"[SaveMenuUI] === Slot {slotIndex + 1} sélectionné (mode: {currentMode}) ===");

        bool slotIsUsed = SaveManager.Instance.IsSlotUsed(slotIndex);

        if (currentMode == SaveMenuMode.NewGame)
        {
            Debug.Log("[SaveMenuUI] Mode: NewGame - Vérification du slot...");

            if (slotIsUsed)
            {
                Debug.LogWarning($"[SaveMenuUI] Slot {slotIndex + 1} déjà utilisé, sera écrasé");
            }
            else
            {
                Debug.Log($"[SaveMenuUI] Slot {slotIndex + 1} est vide, création d'une nouvelle save");
            }

            selectedSlotForNewGame = slotIndex;
            Debug.Log($"[SaveMenuUI] selectedSlotForNewGame = {selectedSlotForNewGame}");
            Debug.Log("[SaveMenuUI] Appel de ShowNameInputPopup()...");
            ShowNameInputPopup();
        }
        else if (currentMode == SaveMenuMode.Continue)
        {
            Debug.Log("[SaveMenuUI] Mode: Continue - Chargement du slot...");

            if (!slotIsUsed)
            {
                Debug.LogWarning($"[SaveMenuUI] Slot {slotIndex + 1} est vide, impossible de continuer");
                return;
            }

            LoadGameFromSlot(slotIndex);
        }
        else if (currentMode == SaveMenuMode.Play)
        {
            Debug.Log("[SaveMenuUI] Mode: Play - Action selon état du slot...");

            if (slotIsUsed)
            {
                // Slot utilisé: charger la partie
                Debug.Log($"[SaveMenuUI] Slot {slotIndex + 1} utilisé, chargement...");
                LoadGameFromSlot(slotIndex);
            }
            else
            {
                // Slot vide: créer une nouvelle partie
                Debug.Log($"[SaveMenuUI] Slot {slotIndex + 1} vide, création nouvelle save...");
                selectedSlotForNewGame = slotIndex;
                ShowNameInputPopup();
            }
        }
    }

    /// <summary>
    /// Appelé quand on demande à supprimer un slot
    /// </summary>
    private void OnSlotDeleteRequested(int slotIndex)
    {
        Debug.Log($"[SaveMenuUI] Demande de suppression slot {slotIndex + 1}");

        selectedSlotForDelete = slotIndex;
        ShowDeleteConfirmPopup(slotIndex);
    }

    #endregion

    #region Name Input Popup

    /// <summary>
    /// Affiche le popup pour entrer le nom de la save (avec animation)
    /// </summary>
    private void ShowNameInputPopup()
    {
        Debug.Log($"[SaveMenuUI] === ShowNameInputPopup appelé pour slot {selectedSlotForNewGame} ===");

        if (nameInputPopup != null)
        {
            // Essayer d'utiliser l'animation
            SaveMenuAnimation anim = GetComponent<SaveMenuAnimation>();
            if (anim != null)
            {
                anim.ShowPopup();
            }
            else
            {
                nameInputPopup.SetActive(true);
            }
            Debug.Log($"[SaveMenuUI] ✅ nameInputPopup activé: {nameInputPopup.name}");
        }
        else
        {
            Debug.LogError("[SaveMenuUI] ❌ nameInputPopup est NULL! Impossible d'afficher le popup!");
            // Fallback: créer la save avec un nom par défaut
            CreateNewGameInSlot(selectedSlotForNewGame, $"Save {selectedSlotForNewGame + 1}");
            return;
        }

        // Focus sur l'input field
        if (nameInputField != null)
        {
            nameInputField.text = $"Save {selectedSlotForNewGame + 1}"; // Nom par défaut
            nameInputField.Select();
            nameInputField.ActivateInputField();
            Debug.Log($"[SaveMenuUI] ✅ Input field activé avec texte: '{nameInputField.text}'");
        }
        else
        {
            Debug.LogError("[SaveMenuUI] ❌ nameInputField est NULL!");
        }
    }

    /// <summary>
    /// Appelé quand on confirme le nom
    /// </summary>
    private void OnConfirmNameClicked()
    {
        string saveName = nameInputField != null ? nameInputField.text : $"Save {selectedSlotForNewGame + 1}";

        if (string.IsNullOrWhiteSpace(saveName))
        {
            saveName = $"Save {selectedSlotForNewGame + 1}";
        }

        // Cacher le popup
        if (nameInputPopup != null)
        {
            nameInputPopup.SetActive(false);
        }

        // Créer la nouvelle save
        CreateNewGameInSlot(selectedSlotForNewGame, saveName);
    }

    /// <summary>
    /// Appelé quand on annule la saisie du nom
    /// </summary>
    private void OnCancelNameClicked()
    {
        if (nameInputPopup != null)
        {
            nameInputPopup.SetActive(false);
        }

        selectedSlotForNewGame = -1;
    }

    #endregion

    #region Delete Confirmation Popup

    /// <summary>
    /// Affiche le popup de confirmation de suppression
    /// </summary>
    private void ShowDeleteConfirmPopup(int slotIndex)
    {
        if (deleteConfirmPopup != null)
        {
            deleteConfirmPopup.SetActive(true);
        }

        // Mettre à jour le message
        if (deleteConfirmText != null)
        {
            SaveData slotData = SaveManager.Instance.GetSlotInfo(slotIndex);
            string saveName = slotData != null ? slotData.saveName : $"Slot {slotIndex + 1}";
            deleteConfirmText.text = $"Delete save '{saveName}'?";
        }
    }

    /// <summary>
    /// Appelé quand on confirme la suppression
    /// </summary>
    private void OnConfirmDeleteClicked()
    {
        if (selectedSlotForDelete >= 0)
        {
            SaveManager.Instance.DeleteSave(selectedSlotForDelete);
            selectedSlotForDelete = -1;

            // Rafraîchir les slots
            RefreshSlots();
        }

        // Cacher le popup
        if (deleteConfirmPopup != null)
        {
            deleteConfirmPopup.SetActive(false);
        }
    }

    /// <summary>
    /// Appelé quand on annule la suppression
    /// </summary>
    private void OnCancelDeleteClicked()
    {
        selectedSlotForDelete = -1;

        if (deleteConfirmPopup != null)
        {
            deleteConfirmPopup.SetActive(false);
        }
    }

    #endregion

    #region Game Loading

    /// <summary>
    /// Crée une nouvelle partie dans un slot
    /// </summary>
    private void CreateNewGameInSlot(int slotIndex, string saveName)
    {
        Debug.Log($"[SaveMenuUI] Création nouvelle partie: '{saveName}' dans slot {slotIndex + 1}");

        // Créer la save
        SaveManager.Instance.CreateNewSave(slotIndex, saveName);

        // Charger la scène de jeu
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Charge une partie depuis un slot
    /// </summary>
    private void LoadGameFromSlot(int slotIndex)
    {
        Debug.Log($"[SaveMenuUI] Chargement partie depuis slot {slotIndex + 1}");

        // Charger la save
        bool success = SaveManager.Instance.LoadFromSlot(slotIndex);

        if (!success)
        {
            Debug.LogError($"[SaveMenuUI] Échec du chargement du slot {slotIndex + 1}");
            return;
        }

        // Charger la scène de jeu
        SceneManager.LoadScene(gameSceneName);
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Appelé quand on clique sur Back
    /// </summary>
    private void OnBackClicked()
    {
        CloseMenu();
        onBackCallback?.Invoke();
    }

    #endregion
}

/// <summary>
/// Mode du menu de sauvegarde
/// </summary>
public enum SaveMenuMode
{
    NewGame,    // Créer une nouvelle partie
    Continue,   // Charger une partie existante
    Play        // Mode unifié (peut créer ou charger)
}
