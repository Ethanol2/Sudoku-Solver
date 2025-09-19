using System.Collections.Generic;
using EditorTools;
using UnityEngine;

[CreateAssetMenu(fileName = "PuzzleBook", menuName = "Scriptable Objects/PuzzleBook")]
public class PuzzleBook : ScriptableObject
{
    [SerializeField] private string _bookName = "New Book";
    [SerializeField] private string _bookSource = string.Empty;

    [Space]
    [SerializeField] private List<Board> _boards;
    [SerializeField] private TextAsset _importAsset;

    public string Name => _bookName;
    public string Source => _bookSource;

    void OnValidate()
    {
        if (_importAsset)
        {
            string text = _importAsset.text;
            _importAsset = null;
            var temp = ImporterExporter.ImportBoardLinesToPuzzleBook(text);
            this.Log(temp.Count);
            _boards = temp;
        }
    }

    public List<IBoard.State> GetBoardStates()
    {
        List<IBoard.State> states = new List<IBoard.State>();
        Dictionary<string, string> props = new(){
                    {"Name", _bookName},
                    {"Source", _bookSource}
                };

        foreach (var board in _boards)
        {
            int boardSize;
            switch (board.Numbers.Length)
            {
                case 16:
                    boardSize = 4;
                    break;
                case 36:
                    boardSize = 6;
                    break;
                case 81:
                    boardSize = 9;
                    break;
                case 144:
                    boardSize = 12;
                    break;
                case 625:
                    boardSize = 25;
                    break;
                default:
                    Debug.Log("Unknown board size");
                    continue;
            }

            IBoard.State newState = new IBoard.State()
            {
                Properties = props,
                Numbers = new int[boardSize, boardSize],
                Difficulty = board.Difficulty
            };

            int x = 0, y = 0;
            for (int i = 0; i < board.Numbers.Length; i++)
            {
                newState.Numbers[x, y] = board.Numbers[i];

                x++;
                if (x >= boardSize)
                {
                    x = 0;
                    y++;
                }
            }
            states.Add(newState);
        }
        return states;
    }


    [System.Serializable]
    public class Board
    {
        public int[] Numbers;
        public float Difficulty;
    }
}
