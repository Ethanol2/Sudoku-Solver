using System.Collections.Generic;
using EditorTools;
using UnityEngine;

[CreateAssetMenu(fileName = "ImportedBoards", menuName = "Scriptable Objects/ImportedBoards")]
public class ImportedBoards : ScriptableObject
{
    [SerializeField] private List<Board> _boards;
    [SerializeField] private TextAsset _importAsset;

    public

    void OnValidate()
    {
        if (_importAsset)
        {
            string text = _importAsset.text;
            _importAsset = null;
            var temp = ImporterExporter.ImportBoardLinesToImportedBoards(text);
            this.Log(temp.Count);
            _boards = temp;
        }
    }

    public List<IBoard.State> GetBoardStates()
    {
        List<IBoard.State> states = new List<IBoard.State>();

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
                Properties = board.GetProperties(),
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
        public List<string> Keys = new List<string>();
        public List<string> Values = new List<string>();

        public Dictionary<string, string> GetProperties()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            for (int i = 0; i < Keys.Count; i++)
            {
                props.Add(Keys[i], Values[i]);
            }

            return props;
        }
    }
}
