using System.Collections.Generic;
using System.IO;
using EditorTools;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class BoardSelection : MonoBehaviour
{
    [Header("Select UI")]
    [SerializeField] private Transform _boardsSelectorView;
    [SerializeField] private LayoutGroup _selectButtonsParent;
    [SerializeField] private BoardSelectionButton _buttonPrefab;

    [Space]
    [SerializeField] private Button _returnButton;

    [Header("References")]
    [SerializeField] private Board _boardPrefab;
    [SerializeField] private Board _board;

    [Space]
    [SerializeField] private TextAsset _defaultBoards;

    public event System.Action<Board> OnBoardCreated;
    public event System.Action OnBoardDestroyed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var boards = ImporterExporter.ParseBoardsJson(_defaultBoards.text);

        foreach (IBoard.State board in boards)
        {
            BoardSelectionButton button = GameObject.Instantiate(_buttonPrefab, _selectButtonsParent.transform);
            button.Board = board;
            button.Solved = board.Solved;
            button.Difficulty = board.Difficulty;
            button.OnClicked += OnBoardSelected;
        }

        _returnButton.onClick.AddListener(CloseBoard);
        _returnButton.gameObject.SetActive(false);
    }
    private void OnBoardSelected(IBoard.State boardNumbers)
    {
        _board = GameObject.Instantiate(_boardPrefab);

        _board.Init(boardNumbers, this);
        _boardsSelectorView.gameObject.SetActive(false);

        _returnButton.gameObject.SetActive(true);

        OnBoardCreated?.Invoke(_board);
    }
    public void CloseBoard()
    {
        Destroy(_board.gameObject);

        _boardsSelectorView.gameObject.SetActive(true);
        _returnButton.gameObject.SetActive(false);

        OnBoardDestroyed?.Invoke();
    }

}
