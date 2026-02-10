using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Anti-sèche: affiche les notes de tous les étudiants par matière
/// </summary>
public class CheatSheetUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Transform tableContainer;
    [SerializeField] private GameObject headerRowPrefab;
    [SerializeField] private GameObject studentRowPrefab;
    [SerializeField] private Button closeButton;

    private bool isOpen = false;
    private List<StudentData> currentStudents = new List<StudentData>();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseCheatSheet);
    }

    private void Update()
    {
        // Fermer avec Escape ou Tab
        if (isOpen && Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
            {
                CloseCheatSheet();
            }
        }
    }

    public void OpenCheatSheet(List<StudentData> students)
    {
        if (students == null || students.Count == 0)
        {
            Debug.LogWarning("[CheatSheetUI] No students to display!");
            return;
        }

        currentStudents = students;
        isOpen = true;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowCheatSheet();
        else
            return;

        BuildTable();
    }

    public void CloseCheatSheet()
    {
        if (!isOpen) return;
        isOpen = false;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowHUD();
    }

    public void ToggleCheatSheet(List<StudentData> students)
    {
        if (isOpen)
            CloseCheatSheet();
        else
            OpenCheatSheet(students);
    }

    private void BuildTable()
    {
        if (tableContainer == null || studentRowPrefab == null)
        {
            Debug.LogError("[CheatSheetUI] Missing references!");
            return;
        }

        // Clear existing rows
        foreach (Transform child in tableContainer)
        {
            Destroy(child.gameObject);
        }

        // Title
        if (titleText != null)
            titleText.text = "CHEAT SHEET";

        // Description
        if (descriptionText != null)
            descriptionText.text = "Grades from previous exams. Use this to find the best student to copy from!";

        // Header row (if prefab exists)
        if (headerRowPrefab != null)
        {
            Instantiate(headerRowPrefab, tableContainer);
        }

        // Student rows
        foreach (StudentData student in currentStudents)
        {
            if (student == null) continue;

            GameObject row = Instantiate(studentRowPrefab, tableContainer);
            CheatSheetStudentRow rowScript = row.GetComponent<CheatSheetStudentRow>();

            if (rowScript != null)
                rowScript.Setup(student);
        }
    }

    public bool IsOpen() => isOpen;
}
