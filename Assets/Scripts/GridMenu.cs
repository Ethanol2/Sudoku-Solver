using System.Collections;
using System.Collections.Generic;
using TMPro;
using UIRangeSliderNamespace;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GridMenu : MonoBehaviour
{
    [Header("Puzzles to Import")]
    [SerializeField] private PuzzleBook[] _puzzleBooks;

    [Header("Filters")]
    [SerializeField] UIRangeSlider _difficultySlider;
    [SerializeField] private Button _sortButton;
    [SerializeField] private TMP_Text _sortButtonText;

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
    [SerializeField] private List<IBoard.State> _filteredStates = new List<IBoard.State>();
    [SerializeField] private bool _sortHardFirst = false;
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

        if (PlayerPrefs.HasKey("SortHardFirst"))
            _sortHardFirst = PlayerPrefs.GetInt("SortHardFirst") == 1;
        _sortButtonText.text = _sortHardFirst ? "Hard to Easy" : "Easy to Hard";

        if (PlayerPrefs.HasKey("DifficultyMin"))
            _difficultySlider.valueMin = PlayerPrefs.GetFloat("DifficultyMin");
        if (PlayerPrefs.HasKey("DifficultyMax"))
            _difficultySlider.valueMax = PlayerPrefs.GetFloat("DifficultyMax");

        SortByDifficulty(_sortHardFirst);

        _sortButton.onClick.AddListener(ToggleSortOrder);
    }
    void OnEnable()
    {
        _difficultySlider.onHandlesReleased.AddListener(UpdatePageButtons);
    }
    void OnDestroy()
    {
        _difficultySlider.onHandlesReleased.RemoveListener(UpdatePageButtons);
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
    public void ToggleSortOrder()
    {
        _sortHardFirst = !_sortHardFirst;
        PlayerPrefs.SetInt("SortHardFirst", _sortHardFirst ? 1 : 0);
        _sortButtonText.text = _sortHardFirst ? "Hard to Easy" : "Easy to Hard";
        SortByDifficulty(_sortHardFirst);
    }
    private void UpdatePageButtons(float diffMin, float diffMax)
    {
        PlayerPrefs.SetFloat("DifficultyMin", diffMin);
        PlayerPrefs.SetFloat("DifficultyMax", diffMax);

        _filteredStates.Clear();
        foreach (IBoard.State state in _allStates)
            if (state.Difficulty >= diffMin && state.Difficulty <= diffMax)
                _filteredStates.Add(state);

        int lastPage = Mathf.Max(1, _filteredStates.Count / _pageButtons.Length);
        if (_currentPage >= lastPage)
            _currentPage = lastPage - 1;
        _pageNumberText.text = $"{_currentPage + 1} / {lastPage}";

        for (int i = _currentPage * _pageButtons.Length, k = 0; k < _pageButtons.Length; i++, k++)
        {
            if (i >= _filteredStates.Count)
                _pageButtons[k].gameObject.SetActive(false);
            else
            {
                _pageButtons[k].gameObject.SetActive(true);
                _pageButtons[k].Board = _filteredStates[i];
            }
        }
    }
    public void UpdatePageButtons() => UpdatePageButtons(_difficultySlider.valueMin, _difficultySlider.valueMax);

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
