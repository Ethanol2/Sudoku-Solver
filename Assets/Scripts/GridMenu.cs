using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GridMenu : MonoBehaviour
{
    [Header("Puzzles to Import")]
    [SerializeField] private PuzzleBook[] _puzzleBooks;

    [Header("Prefabs")]
    [SerializeField] private GridLayoutGroup _buttonsParent;
    [SerializeField] private Board _boardPrefab;

    [Header("References")]
    [SerializeField] private Solver _solver;
    [SerializeField] private RectTransform _gameView;
    [SerializeField] private RectTransform _boardsSelectorView;

    [Header("UI References")]
    [SerializeField] private BoardButton[] _pageButtons;
    [SerializeField] private Button _returnToMenuButton;
    [SerializeField] private TMP_Text _pageNumberText;

    [Header("Debug")]
    [SerializeField] private List<IBoard.State> _allStates = new List<IBoard.State>();
    [SerializeField] private int _currentPage = 0;

    private Board _board;

    public UnityEvent OnBoardLoaded;
    public UnityEvent OnBoardGenerated;

    public Board CurrentBoard => _board;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _allStates.Clear();

        foreach (PuzzleBook book in _puzzleBooks)
            _allStates.AddRange(book.GetBoardStates());

        foreach (BoardButton button in _pageButtons)
            button.OnClicked += OnBoardSelected;

        _gameView.gameObject.SetActive(false);

        _returnToMenuButton.onClick.AddListener(OnReturnToMenu);

        _pageNumberText.text = $"{_currentPage + 1} / {_allStates.Count / _pageButtons.Length}";
        SortByDifficulty();
    }

    private void OnBoardSelected(IBoard.State state)
    {
        _board = GameObject.Instantiate(_boardPrefab);

        _board.Init(state);

        _boardsSelectorView.gameObject.SetActive(false);
        _gameView.gameObject.SetActive(true);

        _solver.OnBoardCreated(_board);

        OnBoardLoaded.Invoke();
    }
    private void OnReturnToMenu()
    {
        _gameView.gameObject.SetActive(false);
        _boardsSelectorView.gameObject.SetActive(true);
        Destroy(_board.gameObject);

        _solver.OnBoardDestroyed();
    }
    private void UpdatePageButtons()
    {
        for (int i = _currentPage * _pageButtons.Length, k = 0; k < _pageButtons.Length; i++, k++)
        {
            if (i >= _allStates.Count)
                _pageButtons[k].gameObject.SetActive(false);
            else
            {
                _pageButtons[k].gameObject.SetActive(true);
                _pageButtons[k].Board = _allStates[i];
            }
        }
    }

    public void SortByDifficulty(bool hardFirst = false)
    {
        _allStates.Sort((x, y) =>
        {
            return x.Difficulty >= y.Difficulty ? 1 : -1;
        });

        if (hardFirst)
            _allStates.Reverse();

        UpdatePageButtons();
    }
    public void NextPage() => GoToPage(_currentPage + 1);
    public void PreviousPage() => GoToPage(_currentPage - 1);
    public void GoToPage(int page)
    {
        int lastPage = _allStates.Count / _pageButtons.Length;
        _currentPage = (int)Mathf.Repeat(page, lastPage);

        _pageNumberText.text = $"{_currentPage + 1} / {lastPage}";

        UpdatePageButtons();
    }
    public void GenerateEmptyBoard(int boardSize)
    {
        OnBoardSelected(IBoard.State.GenerateEmpty(boardSize));
        OnBoardGenerated.Invoke();
    }
}
