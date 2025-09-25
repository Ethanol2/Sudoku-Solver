using System.Collections;
using System.Collections.Generic;
using EditorTools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Square : MonoBehaviour, ISquare
{
    // Inspector
    [Header("Setup")]
    [SerializeField] private Transform _notesParent;
    [SerializeField] private TMP_Text _notePrefab;
    [SerializeField] private TMP_Text _numberDisplay;
    [SerializeField] private Button _button;
    [SerializeField] private RightClickButton _rightClickButton;

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
            int oldNum = _number;

            _number = value;

            if (_number < 0)
                _number = _board.BoardSize;
            else if (_number > _board.BoardSize)
                _number = 0;

            _numberDisplay.gameObject.SetActive(_number != 0);
            _numberDisplay.text = _number.ToString();

            _notesParent.gameObject.SetActive(_number == 0);

            OnChanged?.Invoke(oldNum, _number);
        }
    }
    public ISquare.Notepad Notes => _notes;
    public Color Colour { get => _button.image.color; set => _button.image.color = value; }
    public bool Locked
    {
        get => _locked;
        set
        {
            _locked = value;
            _button.targetGraphic.raycastTarget = _numberDisplay.raycastTarget = !_locked;
            _numberDisplay.fontStyle = _locked ? FontStyles.Bold : FontStyles.Normal;
        }
    }
    public int GroupCount => _groups.Length;

    // Events
    public event System.Action<int, int> OnChanged;

    void OnValidate()
    {
        _button = this.GetComponentInChildren<Button>();
        Debug.Assert(_button);
        Debug.Assert(_notesParent);
    }
    void OnEnable()
    {
        _button.onClick.AddListener(OnClicked);
        _rightClickButton.OnClick.AddListener(OnRightClicked);

        if (_board)
            _board.OnNoteModeToggle += OnNoteModeToggle;
    }
    void OnDisable()
    {
        _button.onClick.RemoveListener(OnClicked);
        _rightClickButton.OnClick.RemoveListener(OnRightClicked);

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
    [ContextMenu("Set Notes")]
    public void SetNotes()
    {
        if (_number > 0)
            return;

        for (int i = 0; i < _board.BoardSize; i++)
        {
            _notes[i + 1] = !GetGroupsContain(i);
        }
    }
    public int GetValidNumbersCount()
    {
        int count = 0;
        for (int i = 0; i < _board.BoardSize; i++)
        {
            foreach (SquareGroup group in _groups)
            {
                if (group.ContainsByIndex(i))
                {
                    break;
                }
            }
            count++;
        }
        return count;
    }

    private void OnClicked()
    {
        if (_board.NoteMode)
        {
            _notes[_board.NoteNumber] = !_notes[_board.NoteNumber];
        }
        else
        {
            Number++;
        }
    }
    private void OnRightClicked()
    {
        if (_board.NoteMode)
        {
            _notes[_board.NoteNumber] = !_notes[_board.NoteNumber];
        }
        else
        {
            Number--;
        }
    }

    private void OnNoteModeToggle(bool state)
    {
        if (_number > 0)
            _button.interactable = !state;
        else
            _button.interactable = true;
    }
    private bool GetGroupsContain(int numIndex)
    {
        foreach (SquareGroup group in _groups)
        {
            if (group.ContainsByIndex(numIndex))
            {
                return true;
            }
        }
        return false;
    }

    [ContextMenu("Check for Unique")]
    public bool CheckForUniqueNotes() => CheckForUniqueNotes(true);
    public bool CheckForUniqueNotes(bool apply)
    {
        for (int n = 0; n < _board.BoardSize; n++)
        {
            foreach (SquareGroup group in _groups)
            {
                if (group.GetNumberCount(n) > 1)
                    continue;

                if (group.NotedNumbers[n] == 1 && _notes[n + 1])
                {
                    _notes.Clear();
                    _notes[n + 1] = true;
                    return true;
                }
            }
        }
        return false;
    }
    [ContextMenu("Set Group Notes")]
    public void SetGroupNotes()
    {
        foreach (SquareGroup group in _groups)
        {
            foreach (ISquare square in group.Squares)
                square.SetNotes();
        }
    }
    [ContextMenu("Set Group Unique")]
    public void SetGroupUnique()
    {
        foreach (SquareGroup group in _groups)
        {
            foreach (ISquare square in group.Squares)
                square.CheckForUniqueNotes(true);
        }
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
        public override void Clear()
        {
            base.Clear();

            foreach (TMP_Text text in Texts)
                text.gameObject.SetActive(false);
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
            int oldNum = _number;
            _number = Mathf.Clamp(value, 0, _board.BoardSize);

            OnChanged?.Invoke(oldNum, _number);
        }
    }
    public ISquare.Notepad Notes => _notes;
    public bool Locked { get => _locked; set => _locked = value; }
    public int GroupCount => _groups.Length;

    // Events
    public event System.Action<int, int> OnChanged;

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
    public void SetNotes()
    {
        if (_number > 0)
            return;

        for (int i = 0; i < _board.BoardSize; i++)
        {
            _notes[i + 1] = !GetGroupsContain(i);
        }
    }
    public int GetValidNumbersCount()
    {
        int count = 0;
        for (int i = 0; i < _board.BoardSize; i++)
        {
            foreach (SquareGroup group in _groups)
            {
                if (group.ContainsByIndex(i))
                {
                    break;
                }
            }
            count++;
        }
        return count;
    }
    public bool CheckForUniqueNotes(bool apply)
    {
        for (int n = 0; n < _board.BoardSize; n++)
        {
            foreach (SquareGroup group in _groups)
            {
                if (group.NotedNumbers[n] == 1 && _notes[n + 1])
                {
                    _notes.Clear();
                    _notes[n + 1] = true;
                    return true;
                }
            }
        }
        return false;
    }
    public void SetGroupNotes()
    {
        foreach (SquareGroup group in _groups)
        {
            foreach (ISquare square in group.Squares)
                square.SetNotes();
        }
    }
    private bool GetGroupsContain(int numIndex)
    {
        foreach (SquareGroup group in _groups)
        {
            if (group.ContainsByIndex(numIndex))
            {
                return true;
            }
        }
        return false;
    }
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
    public event System.Action<int, int> OnChanged;

    // Methods
    public void AddGroup(SquareGroup group, int index);
    public SquareGroup GetGroup(int index);
    public void SetNotes();
    public int GetValidNumbersCount();
    public bool CheckForUniqueNotes(bool apply);
    public void SetGroupNotes();

    [System.Serializable]
    public class Notepad
    {
        public bool[] Numbers;
        private int _count = 0;

        public int Count => _count;
        public event System.Action<int> OnNoteAdded;
        public event System.Action<int> OnNoteRemoved;

        public Notepad(int size)
        {
            Numbers = new bool[size];
        }
        public int GetSmallestNote()
        {
            for (int i = 0; i < Numbers.Length; i++)
            {
                if (Numbers[i])
                {
                    return i + 1;
                }
            }

            return 0;
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
        public virtual void Clear()
        {
            for (int n = 0; n < Numbers.Length; n++)
                if (Numbers[n])
                    OnNoteRemoved?.Invoke(n + 1);

            Numbers = new bool[Numbers.Length];
            _count = 0;
        }

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
                throw new System.Exception($"Attempted to note number outside the scope of the game. Range: 1 -> {Numbers.Length + 1} Number: {i + 1}");

            if (Numbers[i] == value)
                return;

            if (Numbers[i] && !value)
            {
                _count--;
                OnNoteRemoved?.Invoke(i + 1);
            }
            else if (!Numbers[i] && value)
            {
                _count++;
                OnNoteAdded?.Invoke(i + 1);
            }

            Numbers[i] = value;
        }
    }
}