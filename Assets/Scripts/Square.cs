using System.Collections.Generic;
using EditorTools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Square : MonoBehaviour, ISquare
{
    // Inspector
    [Header("Setup")]
    [SerializeField] private Transform _notesParent;
    [SerializeField] private TMP_Text _notePrefab;
    [SerializeField] private TMP_Text _numberDisplay;
    [SerializeField] private Button _button;

    [Header("Debug")]
    [SerializeField] private int _number = 0;
    [SerializeField] private Board _board;
    [SerializeField] private SquareGroup[] _groups;
    [SerializeField] private bool _locked = false;

    // Privates
    private ISquare.Notepad _notes;

    // Properties
    public string Name => name;
    public int Number
    {
        get => _number;
        set
        {
            _number = Mathf.Clamp(value, 0, _board.BoardSize);

            _numberDisplay.gameObject.SetActive(_number != 0);
            _numberDisplay.text = _number.ToString();

            _notesParent.gameObject.SetActive(_number == 0);

            OnChanged?.Invoke(this);
        }
    }
    public ISquare.Notepad Notes => _notes;
    public Color Colour { get => _button.image.color; set => _button.image.color = value; }
    public bool Locked { get => _locked; set { _locked = value; _button.targetGraphic.raycastTarget = _numberDisplay.raycastTarget = !_locked; } }
    public int GroupCount => _groups.Length;

    // Events
    public event System.Action<ISquare> OnChanged;

    void OnValidate()
    {
        _button = this.GetComponentInChildren<Button>();
        Debug.Assert(_button);
        Debug.Assert(_notesParent);
    }
    void OnEnable()
    {
        _button.onClick.AddListener(OnClicked);

        if (_board)
            _board.OnNoteModeToggle += OnNoteModeToggle;
    }
    void OnDisable()
    {
        _button.onClick.RemoveListener(OnClicked);

        if (_board)
            _board.OnNoteModeToggle -= OnNoteModeToggle;
    }
    public void Init(Board board, int number, bool locked)
    {
        _board = board;
        Number = number;
        _notes = new Notepad(_notePrefab, _notesParent.transform, _board);
        _groups = new SquareGroup[] { null, null, null };

        Locked = locked;

        // On the remote chance the square is initialized twice, unsub from the event first
        board.OnNoteModeToggle -= OnNoteModeToggle;
        board.OnNoteModeToggle += OnNoteModeToggle;
    }
    public void AddGroup(SquareGroup group, int index)
    {
        if (_groups[index] != null)
            _groups[index].OnValidityChanged -= UpdateColour;

        _groups[index] = group;
        group.OnValidityChanged += UpdateColour;
    }
    public SquareGroup GetGroup(int index) => _groups[index];
    public void UpdateColour()
    {
        Colour = _groups[0].IsValid && _groups[1].IsValid && _groups[2].IsValid ? _board.NormalColour : _board.WarningColour;
    }

    private void OnClicked()
    {
        if (_board.NoteMode)
        {
            _notes[_board.NoteNumber] = !_notes[_board.NoteNumber];
        }
        else
        {
            _number++;
            if (_number > _board.BoardSize)
                _number = 0;

            Number = _number;
        }
    }
    private void OnNoteModeToggle(bool state)
    {
        if (_number > 0)
            _button.interactable = !state;
        else
            _button.interactable = true;
    }

    [System.Serializable]
    public class Notepad : ISquare.Notepad
    {
        public TMP_Text[] Texts;

        public Notepad(TMP_Text notePrefab, Transform notesParent, Board board) : base(board.BoardSize)
        {
            Numbers = new bool[board.BoardSize];
            Texts = new TMP_Text[board.BoardSize];

            int y = 0;
            for (int x = 0; x < board.BoardSize; x++)
            {
                Texts[x] = GameObject.Instantiate(notePrefab, notesParent);
                Texts[x].text = (x + 1).ToString();
                Texts[x].name = "Note " + x;
                Texts[x].gameObject.SetActive(false);

                RectTransform rect = Texts[x].GetComponent<RectTransform>();
                rect.localScale = Vector3.one;

                if (x - (board.SquareCount.x * y) == board.SquareCount.x)
                    y++;

                Board.SetAnchors(rect, x - (board.SquareCount.x * y), board.SquareCount.y - 1 - y, board.SquareCount.x, board.SquareCount.y, 0f);
            }
        }
        protected override void SetNote(int i, bool value)
        {
            base.SetNote(i, value);
            Texts[i - 1].gameObject.SetActive(value);
        }
    }
}

public class DataOnlySquare : ISquare
{
    private string _name;
    private int _number = 0;
    private IBoard _board;
    private SquareGroup[] _groups;
    private bool _locked = false;

    // Privates
    private ISquare.Notepad _notes;

    // Properties
    public string Name => _name;
    public int Number
    {
        get => _number;
        set
        {
            _number = Mathf.Clamp(value, 0, _board.BoardSize);
            OnChanged?.Invoke(this);
        }
    }
    public ISquare.Notepad Notes => _notes;
    public bool Locked { get => _locked; set => _locked = value; }
    public int GroupCount => _groups.Length;

    // Events
    public event System.Action<ISquare> OnChanged;

    // Methods
    public DataOnlySquare(IBoard board, string name, int number, bool locked)
    {
        _board = board;
        _name = name;
        Number = number;
        Locked = locked;

        _notes = new ISquare.Notepad(board.BoardSize);
        _groups = new SquareGroup[] { null, null, null };
    }
    public void AddGroup(SquareGroup group, int index) => _groups[index] = group;
    public SquareGroup GetGroup(int index) => _groups[index];
}

public interface ISquare
{
    // Properties
    public string Name { get; }
    public int Number { get; set; }
    public Notepad Notes { get; }
    public bool Locked { get; set; }
    public int GroupCount { get; }

    // Events
    public event System.Action<ISquare> OnChanged;

    // Methods
    public void AddGroup(SquareGroup group, int index);
    public SquareGroup GetGroup(int index);

    [System.Serializable]
    public class Notepad
    {
        public bool[] Numbers;

        public int Count
        {
            get
            {
                int count = 0;
                foreach (bool n in Numbers)
                    if (n)
                        count++;

                return count;
            }
        }

        public Notepad(int size)
        {
            Numbers = new bool[size];
        }
        public List<int> GetActiveNotesList()
        {
            List<int> active = new List<int>();
            for (int i = 1; i < Numbers.Length + 1; i++)
            {
                if (Numbers[i - 1])
                {
                    active.Add(i);
                }
            }

            return active;
        }
        public int[] GetActiveNotes() => GetActiveNotesList().ToArray();

        public bool this[int i] { get => GetNote(i); set => SetNote(i, value); }
        protected virtual bool GetNote(int i)
        {
            i--;
            if (i >= Numbers.Length || i < 0)
                throw new System.Exception($"Attempted get note number outside the scope of the game. Range: 1 -> {Numbers.Length} Number: {i + 1}");
            return Numbers[i];
        }
        protected virtual void SetNote(int i, bool value)
        {
            i--;
            if (i >= Numbers.Length || i < 0)
                throw new System.Exception($"Attempted to note number outside the scope of the game. Range: 1 -> {Numbers.Length} Number: {i + 1}");
            Numbers[i] = value;
        }
    }
}