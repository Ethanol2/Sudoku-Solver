using System.Collections.Generic;
using EditorTools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button _noteNumberButtonPrefab;
    [SerializeField] private RectTransform _noteNumbersParent;
    [SerializeField] private float _noteNumbersPadding = 0.05f;

    [Space]
    [SerializeField] private ToggleSwitch _notesToggle;
    [SerializeField] private GridMenu _gridMenu;

    [Header("Settings")]
    [SerializeField] private Vector2Int _4x4Dimensions = Vector2Int.one * 2;
    [SerializeField] private Vector2Int _6x6Dimensions = new Vector2Int(3, 2);
    [SerializeField] private Vector2Int _9x9Dimensions = Vector2Int.one * 3;
    [SerializeField] private Vector2Int _12x12Dimensions = new Vector2Int(3, 4);
    [SerializeField] private Vector2Int _25x25Dimensions = Vector2Int.one * 5;

    [Header("Debug")]
    [SerializeField] private List<Button> _noteNumbers = new List<Button>();
    [SerializeField] private Board _board;

    void OnValidate()
    {
        if (_noteNumberButtonPrefab && _noteNumbersParent && !Application.isPlaying)
        {
            ConfigureNoteButtons(9);
        }
    }

    private void SetRectAnchors(List<Button> buttons, Vector2Int gridDimensions)
    {
        int count = gridDimensions.x * gridDimensions.y;
        if (count > buttons.Count)
        {
            this.LogError("List passed to SetRectAnchors is smaller than the requested grid size");
            return;
        }

        foreach (Button button in buttons)
            button.gameObject.SetActive(false);

        for (int i = 0, x = 0, y = gridDimensions.y - 1; i < count; i++, x++)
        {
            if (x >= gridDimensions.x)
            {
                x = 0;
                y--;
            }

            Board.SetAnchors(buttons[i].transform as RectTransform, x, y, gridDimensions.x, gridDimensions.y, _noteNumbersPadding);
            buttons[i].gameObject.SetActive(true);
            buttons[i].GetComponentInChildren<TMP_Text>().text = (i + 1).ToString();
            buttons[i].name = (1 + i).ToString();
            buttons[i].transform.SetAsLastSibling();

            if (Application.isPlaying)
            {
                buttons[i].onClick.RemoveAllListeners();
                int index = i;
                buttons[i].onClick.AddListener(() => OnNoteButtonClicked(index));
            }
        }
    }
    void OnEnable()
    {
        _gridMenu.OnBoardGenerated.AddListener(OnBoardGenerated);
        _gridMenu.OnBoardLoaded.AddListener(OnBoardLoaded);

        _notesToggle.OnToggled += OnNotesToggle;
    }
    private void OnDisable()
    {
        _gridMenu.OnBoardGenerated.RemoveListener(OnBoardGenerated);
        _gridMenu.OnBoardLoaded.RemoveListener(OnBoardLoaded);

        _notesToggle.OnToggled -= OnNotesToggle;
    }

    private void OnNoteButtonClicked(int index)
    {
        _board.SetNoteNumber(_noteNumbers[index].transform.GetSiblingIndex() + 1);

        foreach (Button button in _noteNumbers)
        {
            button.interactable = true;
            button.image.raycastTarget = true;
            button.image.color = button.colors.normalColor;
        }

        _noteNumbers[index].image.raycastTarget = false;
        _noteNumbers[index].image.color = _noteNumbers[index].colors.pressedColor;
    }

    private void OnBoardGenerated()
    {
        _notesToggle.transform.parent.parent.gameObject.SetActive(false);
        _noteNumbersParent.parent.gameObject.SetActive(false);
    }

    private void OnBoardLoaded()
    {
        _notesToggle.transform.parent.gameObject.SetActive(true);
        _noteNumbersParent.gameObject.SetActive(true);

        _board = _gridMenu.CurrentBoard;
        ConfigureNoteButtons(_board.BoardSize);

    }

    private void OnNotesToggle(bool isOn)
    {
        if (_board != null)
            _board.ToggleNoteMode(isOn);

        foreach (Button button in _noteNumbers)
            button.interactable = isOn;
    }

    private void ConfigureNoteButtons(int boardSize)
    {
        while (_noteNumbers.Count < boardSize)
            _noteNumbers.Add(GameObject.Instantiate(_noteNumberButtonPrefab, _noteNumbersParent));

        switch (boardSize)
        {
            case 4:
                SetRectAnchors(_noteNumbers, _4x4Dimensions);
                break;
            case 6:
                SetRectAnchors(_noteNumbers, _6x6Dimensions);
                break;
            case 9:
                SetRectAnchors(_noteNumbers, _9x9Dimensions);
                break;
            case 12:
                SetRectAnchors(_noteNumbers, _12x12Dimensions);
                break;
            case 25:
                SetRectAnchors(_noteNumbers, _25x25Dimensions);
                break;
            default:
                this.LogError("Unsupported board size for note buttons");
                break;
        }
    }
}
