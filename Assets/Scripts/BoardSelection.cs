using System.Collections.Generic;
using System.IO;
using EditorTools;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class BoardSelection : MonoBehaviour
{
    [Header("Select UI")]
    [SerializeField] private LayoutGroup selectButtonsParent;
    [SerializeField] private BoardSelectionButton buttonPrefab;

    [Header("Importing")]
    [SerializeField] private string _filePath = "soduku-boards.json";
    [SerializeField] private Board.BoardNumbers[] _boards;

    [Header("References")]
    [SerializeField] private Board _board;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _boards = ImportBoards(_filePath);

        foreach (var board in _boards)
        {
            BoardSelectionButton button = GameObject.Instantiate(buttonPrefab, selectButtonsParent.transform);
            button.Board = board;
            button.Solved = board.Solved;
            button.Difficulty = board.Difficulty;
            button.OnClicked += OnBoardSelected;
        }
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
        _board.gameObject.SetActive(true);
        _board.Init(boardNumbers);
        this.gameObject.SetActive(false);
    }
}
