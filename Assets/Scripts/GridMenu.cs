using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GridMenu : MonoBehaviour
{
    [Header("Puzzles to Import")]
    [SerializeField] private ImportedBoards[] _importedBoards;

    [Header("Prefabs")]
    [SerializeField] private BoardButton _buttonPrefab;
    [SerializeField] private GridLayoutGroup _buttonsParent;
    [SerializeField] private Board _boardPrefab;

    [Header("References")]
    [SerializeField] private Solver _solver;
    [SerializeField] private RectTransform _gameView;
    [SerializeField] private Button _returnToMenuButton;
    [SerializeField] private RectTransform _boardsSelectorView;

    [Header("Debug")]
    [SerializeField] private List<BoardButton> _boardButtons;

    private Board _board;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        foreach (ImportedBoards boards in _importedBoards)
            ImportBoards(boards);

        _gameView.gameObject.SetActive(false);

        _returnToMenuButton.onClick.AddListener(OnReturnToMenu);

        SortByDifficulty(true);
    }
    private void ImportBoards(ImportedBoards boards)
    {
        var states = boards.GetBoardStates();

        foreach (var state in states)
        {
            BoardButton newButton = GameObject.Instantiate(_buttonPrefab, _buttonsParent.transform);
            newButton.Board = state;
            newButton.OnClicked += OnBoardSelected;

            _boardButtons.Add(newButton);
        }
    }

    private void OnBoardSelected(IBoard.State state)
    {
        _board = GameObject.Instantiate(_boardPrefab);

        _board.Init(state);

        _boardsSelectorView.gameObject.SetActive(false);
        _gameView.gameObject.SetActive(true);

        _solver.OnBoardCreated(_board);
    }
    private void OnReturnToMenu()
    {
        _gameView.gameObject.SetActive(false);
        _boardsSelectorView.gameObject.SetActive(true);
        Destroy(_board.gameObject);

        _solver.OnBoardDestroyed();
    }
    private void ApplyListOrderToBoardButtons()
    {
        for (int i = 0; i < _boardButtons.Count; i++)
            _boardButtons[i].transform.SetSiblingIndex(i);
    }

    public void SortByDifficulty(bool easyFirst)
    {
        _boardButtons.Sort((x, y) =>
        {
            return x.Difficulty <= y.Difficulty ? 1 : -1;
        });

        if (easyFirst)
            _boardButtons.Reverse();

        ApplyListOrderToBoardButtons();
    }
}
