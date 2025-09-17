using System.Collections;
using System.Collections.Generic;
using EditorTools;
using UnityEngine;

public class Board : MonoBehaviour, IBoard
{
    // Inspector

    [Header("Game Settings")]
    [SerializeField] private int _boardSize = 9;
    [SerializeField] private Vector2Int _squareCount = new Vector2Int(3, 3);
    [SerializeField] private Vector2Int _quadrantCount;

    [Header("Visual Settings")]
    [SerializeField] private Color _normalColour = Color.white;
    [SerializeField] private Color _warningColour = Color.red;
    [SerializeField] private float _quadrantAnchorPadding = 0.01f;
    [SerializeField] private float _squareAnchorPadding = 0.005f;

    [Header("References")]
    [SerializeField] private Square _squarePrefab;
    [SerializeField] private Transform _quadrantsParent;

    [Header("Debug")]
    [SerializeField] private bool _initialized = false;
    [SerializeField] private bool _validated = false;
    [SerializeField] private bool _solved = false;

    [Space]
    [SerializeField] private SquareGroup[,] _quadrants;
    [SerializeField] private SquareGroup[] _columns;
    [SerializeField] private SquareGroup[] _rows;
    [SerializeField] private Square[] _allSquares;
    [SerializeField] private bool _noteMode = false;
    [SerializeField] private int _noteNumber = 1;

    // Privates

    // Properties
    public bool Initialized => _initialized;
    public int BoardSize => _boardSize;
    public Vector2Int SquareCount => _squareCount;
    public Color NormalColour => _normalColour;
    public Color WarningColour => _warningColour;
    public bool NoteMode => _noteMode;
    public int NoteNumber => _noteNumber;

    public SquareGroup[,] Quadrants => _quadrants;
    public SquareGroup[] Rows => _rows;
    public SquareGroup[] Columns => _columns;
    public ISquare[] AllSquares => _allSquares;

    // Events
    public event System.Action<bool> OnNoteModeToggle;

    // Lifecycle

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleNoteMode();
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            ValidateSolved();
        }

        for (int n = 1; n < _boardSize + 1; n++)
        {
            if (Input.GetKeyDown(n.ToString()))
                _noteNumber = n;
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown("u"))
        {
            foreach (ISquare square in AllSquares)
            {
                square.Locked = false;
            }
        }
#endif
    }
    public void Init(IBoard.State state)
    {
        _boardSize = state.Numbers.GetLength(0);

        float sqrtSize = Mathf.Sqrt(_boardSize);
        _squareCount = new Vector2Int(
            Mathf.CeilToInt(sqrtSize),
            Mathf.FloorToInt(sqrtSize)
        );

        _quadrantCount = new Vector2Int(_squareCount.y, _squareCount.x);

        RectTransform[,] canvasQuadrants = new RectTransform[_quadrantCount.x, _quadrantCount.y];

        _quadrants = new SquareGroup[_quadrantCount.x, _quadrantCount.y];
        _columns = new SquareGroup[_boardSize];
        _rows = new SquareGroup[_boardSize];
        List<Square> allSquares = new List<Square>();

        int qX = 0;
        for (int x = 0; x < _boardSize; x++)
        {
            if (_columns[x] == null)
            {
                _columns[x] = new SquareGroup(this, 0);
            }

            if ((x - (_squareCount.x * qX)) == _squareCount.x)
                qX++;

            int qY = 0;
            for (int y = 0; y < _boardSize; y++)
            {
                if ((y - (_squareCount.y * qY)) == _squareCount.y)
                    qY++;

                if (!canvasQuadrants[qX, qY])
                {
                    GameObject newQuadrantGO = new GameObject($"Quadrant ({qX}, {qY})");
                    RectTransform newQuadrant = newQuadrantGO.AddComponent<RectTransform>();
                    newQuadrant.SetParent(_quadrantsParent);
                    newQuadrant.localScale = Vector3.one;
                    SetAnchors(newQuadrant, qX, qY, _quadrantCount.x, _quadrantCount.y, _quadrantAnchorPadding);

                    canvasQuadrants[qX, qY] = newQuadrant;

                    // Init the SquareGroup
                    _quadrants[qX, qY] = new SquareGroup(this, 1);
                }
                if (_rows[y] == null)
                {
                    _rows[y] = new SquareGroup(this, 2);
                }

                Square newSquare = GameObject.Instantiate(_squarePrefab, canvasQuadrants[qX, qY]);
                newSquare.name = $"({x}, {y})";

                int number = state.Numbers[_boardSize - 1 - y, x];
                newSquare.Init(this, number, number != 0);

                RectTransform squareRect = newSquare.transform as RectTransform;
                SetAnchors(squareRect, x - (_squareCount.x * qX), y - (_squareCount.y * qY), _squareCount.x, _squareCount.y, _squareAnchorPadding);

                _quadrants[qX, qY].PushSquare(newSquare, false);
                _columns[x].PushSquare(newSquare, false);
                _rows[y].PushSquare(newSquare, false);
                allSquares.Add(newSquare);

                newSquare.OnChanged += OnSquareChanged;
            }
        }

        foreach (SquareGroup group in _quadrants)
            group.UpdateGroupState();
        foreach (SquareGroup group in _rows)
            group.UpdateGroupState();
        foreach (SquareGroup group in _columns)
            group.UpdateGroupState();

        _allSquares = allSquares.ToArray();

        _solved = state.Solved;
        _validated = true;
        _initialized = true;
    }

    // Utility
    public static void SetAnchors(RectTransform rect, int x, int y, int xCount, int yCount, float padding)
    {
        rect.anchorMin = new Vector2(x * (1f / xCount), y * (1f / yCount)) + (Vector2.one * padding);
        rect.anchorMax = new Vector2((x + 1) * (1f / xCount), (y + 1) * (1f / yCount)) - (Vector2.one * padding);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    public void ToggleNoteMode(bool state)
    {
        _noteMode = state;
        _noteNumber = Mathf.Clamp(_noteNumber, 1, _boardSize);
        OnNoteModeToggle?.Invoke(_noteMode);
    }
    public void ToggleNoteMode() => ToggleNoteMode(!_noteMode);
    public void SetNoteNumber(int number) => _noteNumber = number;

    // Interface
    public bool ValidateSolved()
    {
        if (_validated)
            return _solved;

        bool solved = true;

        solved = solved && IBoard.ValidateGroups(Quadrants);
        solved = solved && IBoard.ValidateGroups(Rows);
        solved = solved && IBoard.ValidateGroups(Columns);

        _solved = solved;
        _validated = true;

        return solved;
    }
    public bool HasViolation()
    {
        bool isValid = true;

        isValid = isValid && IBoard.ValidateIncompleteGroups(Quadrants);
        isValid = isValid && IBoard.ValidateIncompleteGroups(Rows);
        isValid = isValid && IBoard.ValidateIncompleteGroups(Columns);

        return !isValid;
    }

    public IBoard.State GetState()
    {
        IBoard.State export = new IBoard.State();
        export.Numbers = new int[BoardSize, BoardSize];

        int x = 0, y = 0;
        for (int i = 0; i < _allSquares.Length; i++)
        {
            if (x >= _boardSize)
            {
                y++;
                x = 0;
            }

            export.Numbers[_boardSize - 1 - x, y] = AllSquares[i].Number;

            x++;
        }

        export.Solved = ValidateSolved();

        return export;
    }
    public void SetState(IBoard.State state)
    {
        if (!_initialized)
        {
            this.Log("Board not initialized. Call Init() instead");
            return;
        }

        if (state.Numbers.GetLength(0) > _boardSize)
        {
            this.LogError($"Wrong size state passed to board. Current: {_boardSize}, State: {state.Numbers.GetLength(0)}");
            return;
        }

        int x = 0, y = 0;
        for (int i = 0; i < _allSquares.Length; i++)
        {
            if (x >= _boardSize)
            {
                y++;
                x = 0;
            }

            AllSquares[i].Number = state.Numbers[_boardSize - 1 - x, y];

            x++;
        }

        _solved = state.Solved;
    }
    private void OnSquareChanged(int a, int b) => _validated = false;

    public static implicit operator DataOnlyBoard(Board board) => new DataOnlyBoard(board.GetState());
}

[System.Serializable]
public class DataOnlyBoard : IBoard
{
    [SerializeField] private int _boardSize = 9;
    [SerializeField] private Vector2Int _squareCount = new Vector2Int(3, 3);
    [SerializeField] private Vector2Int _quadrantCount;

    [SerializeField] private bool _validated = false;
    [SerializeField] private bool _solved = false;

    [Space]
    [SerializeField] private SquareGroup[,] _quadrants;
    [SerializeField] private SquareGroup[] _columns;
    [SerializeField] private SquareGroup[] _rows;
    [SerializeField] private DataOnlySquare[] _allSquares;

    // Privates

    // Properties
    public int BoardSize => _boardSize;
    public Vector2Int SquareCount => _squareCount;

    public SquareGroup[,] Quadrants => _quadrants;
    public SquareGroup[] Rows => _rows;
    public SquareGroup[] Columns => _columns;
    public ISquare[] AllSquares => _allSquares;

    public DataOnlyBoard(IBoard.State state)
    {
        _boardSize = state.Numbers.GetLength(0);

        float sqrtSize = Mathf.Sqrt(_boardSize);
        _squareCount = new Vector2Int(
            Mathf.CeilToInt(sqrtSize),
            Mathf.FloorToInt(sqrtSize)
        );

        _quadrantCount = new Vector2Int(_squareCount.y, _squareCount.x);

        _quadrants = new SquareGroup[_quadrantCount.x, _quadrantCount.y];
        _columns = new SquareGroup[_boardSize];
        _rows = new SquareGroup[_boardSize];

        List<DataOnlySquare> allSquares = new List<DataOnlySquare>();

        int qX = 0;
        for (int x = 0; x < _boardSize; x++)
        {
            if (_columns[x] == null)
            {
                _columns[x] = new SquareGroup(this, 0);
            }

            if ((x - (_squareCount.x * qX)) == _squareCount.x)
                qX++;

            int qY = 0;
            for (int y = 0; y < _boardSize; y++)
            {
                if ((y - (_squareCount.y * qY)) == _squareCount.y)
                    qY++;

                if (_quadrants[qX, qY] == null)
                {
                    _quadrants[qX, qY] = new SquareGroup(this, 1);
                }
                if (_rows[y] == null)
                {
                    _rows[y] = new SquareGroup(this, 2);
                }

                int number = state.Numbers[_boardSize - 1 - y, x];

                DataOnlySquare newSquare = new DataOnlySquare(
                    this,
                    $"({x}, {y})",
                    number,
                    number != 0);

                _quadrants[qX, qY].PushSquare(newSquare, false);
                _columns[x].PushSquare(newSquare, false);
                _rows[y].PushSquare(newSquare, false);
                allSquares.Add(newSquare);

                newSquare.OnChanged += OnSquareChanged;
            }
        }

        foreach (SquareGroup group in _quadrants)
            group.UpdateGroupState();
        foreach (SquareGroup group in _rows)
            group.UpdateGroupState();
        foreach (SquareGroup group in _columns)
            group.UpdateGroupState();

        _allSquares = allSquares.ToArray();

        _solved = state.Solved;
        _validated = true;
    }

    // Interface
    public bool ValidateSolved()
    {
        if (_validated)
            return _solved;

        bool solved = true;

        solved = solved && IBoard.ValidateGroups(Quadrants);
        solved = solved && IBoard.ValidateGroups(Rows);
        solved = solved && IBoard.ValidateGroups(Columns);

        _solved = solved;
        _validated = true;

        return solved;
    }
    public bool HasViolation()
    {
        bool isValid = true;

        isValid = isValid && IBoard.ValidateIncompleteGroups(Quadrants);
        isValid = isValid && IBoard.ValidateIncompleteGroups(Rows);
        isValid = isValid && IBoard.ValidateIncompleteGroups(Columns);

        return !isValid;
    }
    public IBoard.State GetState()
    {
        IBoard.State export = new IBoard.State();
        export.Numbers = new int[BoardSize, BoardSize];

        int x = 0, y = 0;
        for (int i = 0; i < _allSquares.Length; i++)
        {
            if (x >= _boardSize)
            {
                y++;
                x = 0;
            }

            export.Numbers[_boardSize - 1 - x, y] = AllSquares[i].Number;

            x++;
        }

        export.Solved = ValidateSolved();

        return export;
    }
    public void SetState(IBoard.State state)
    {
        if (state.Numbers.GetLength(0) > _boardSize)
        {
            this.LogError($"Wrong size state passed to board. Current: {_boardSize}, State: {state.Numbers.GetLength(0)}");
            return;
        }

        int x = 0, y = 0;
        for (int i = 0; i < _allSquares.Length; i++)
        {
            if (x >= _boardSize)
            {
                y++;
                x = 0;
            }

            AllSquares[i].Number = state.Numbers[_boardSize - 1 - x, y];

            x++;
        }

        _solved = state.Solved;
    }
    private void OnSquareChanged(int a, int b) => _validated = false;

    public static implicit operator IBoard.State(DataOnlyBoard board) => board.GetState();
}

public interface IBoard
{
    public int BoardSize { get; }
    public Vector2Int SquareCount { get; }

    public SquareGroup[,] Quadrants { get; }
    public SquareGroup[] Rows { get; }
    public SquareGroup[] Columns { get; }
    public ISquare[] AllSquares { get; }

    // Utility
    public bool ValidateSolved();
    public bool HasViolation();
    public State GetState();
    public void SetState(State state);

    protected static bool ValidateGroups(IEnumerable groups)
    {
        bool valid = true;
        foreach (SquareGroup group in groups)
        {
            valid = valid && group.AllSquaresFilled() && group.IsValid;
        }
        return valid;
    }
    protected static bool ValidateIncompleteGroups(IEnumerable groups)
    {
        bool valid = true;
        foreach (SquareGroup group in groups)
        {
            valid = valid && group.IsValid;
        }
        return valid;
    }
    // Support
    [System.Serializable]
    public class State
    {
        public string Difficulty;
        public int[,] Numbers;
        public bool Solved = false;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();

        public static State GenerateEmpty(int size)
        {
            State state = new State();
            state.Numbers = new int[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    state.Numbers[x, y] = 0;
                }
            }

            state.Difficulty = "Empty";

            return state;
        }
    }
}