using System.Collections;
using System.Collections.Generic;
using EditorTools;
using UnityEngine;

[System.Serializable]
public class SquareGroup
{
    [Header("References")]
    [SerializeField] private List<ISquare> _squares;
    [SerializeField] private List<int> _numbers;
    [SerializeField] private IBoard _board;

    [Header("State")]
    [SerializeField] private bool _valid = false;

    [Header("Debug")]
    [SerializeField] private int _groupIndex = -1;

    public bool IsValid => _valid;
    public ISquare this[int i] { get => _squares[i]; set => _squares[i] = value; }

    public event System.Action OnValidityChanged;

    public SquareGroup(IBoard board, int groupIndex)
    {
        _board = board;
        _squares = new List<ISquare>();
        _groupIndex = groupIndex;
        _numbers = new List<int>(new int[_board.BoardSize]);
    }
    public void PushSquare(ISquare square, bool refreshState = true)
    {
        square.AddGroup(this, _groupIndex);
        square.OnChanged += OnSquareChanged;
        _squares.Add(square);

        if (square.Number > 0)
            _numbers[square.Number - 1]++;

        if (refreshState)
            UpdateGroupState();
    }
    public ISquare PopSquare(bool refreshState = true) => PopSquare(_squares.Count - 1, refreshState);
    public ISquare PopSquare(int index, bool refreshState = true)
    {
        if (_squares[index].Number > 0)
            _numbers[_squares[index].Number - 1]--;

        ISquare pop = _squares[index];
        _squares.RemoveAt(index);

        if (refreshState)
            UpdateGroupState();

        return pop;
    }

    public bool UpdateGroupState()
    {
        bool valid = _numbers.TrueForAll((x) => x <= 1);

        if (_valid != valid)
        {
            _valid = valid;
            OnValidityChanged?.Invoke();
        }

        return valid;
    }
    public bool AllSquaresFilled() => !_numbers.Contains(0);
    public bool Contains(int num) => _numbers[num - 1] > 0;
    public bool ContainsByIndex(int index) => _numbers[index] > 0;
    public IEnumerator GetEnumerator() => _squares.GetEnumerator();

    private void OnSquareChanged(int oldNum, int newNum)
    {
        if (oldNum > 0)
            _numbers[oldNum - 1]--;
        if (newNum > 0)
            _numbers[newNum - 1]++;

        UpdateGroupState();
    }
}
