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

    [Header("Importing")]
    [SerializeField] private string _filePath = "soduku-boards.json";
    [SerializeField] private Board.BoardNumbers[] _boards;

    [Header("References")]
    [SerializeField] private Board _boardPrefab;
    [SerializeField] private Board _board;

    public event System.Action<Board> OnBoardCreated;
    public event System.Action OnBoardDestroyed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _boards = ImportBoards(_filePath);

        foreach (var board in _boards)
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

    private Board.BoardNumbers[] ImportBoards(string path)
    {
        if (!File.Exists(path))
        {
            this.LogError($"File at \"{path}\" doesn't exist");
            return new Board.BoardNumbers[0];
        }

        string fileContents;

        using StreamReader file = new StreamReader(path);
        {
            fileContents = file.ReadToEnd();
            file.Close();
        }

        Board.BoardNumbers[] boards;
        try
        {
            boards = JsonConvert.DeserializeObject<Board.BoardNumbers[]>(fileContents);
        }
        catch (System.Exception e)
        {
            this.Log($"Something went wrong while importing boards from \"{path}\" =>\n{e}");
            return new Board.BoardNumbers[0];
        }

        return boards;
    }
    private void OnBoardSelected(Board.BoardNumbers boardNumbers)
    {
        _board = GameObject.Instantiate(_boardPrefab);

        _board.Init(boardNumbers);
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
