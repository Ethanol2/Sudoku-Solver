using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardSelectionButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _diffText;
    [SerializeField] private TMP_Text _sizeText;
    [SerializeField] private TMP_Text _solvedText;
    [SerializeField] private Board.State _board;

    public string Difficulty { get => _diffText.text; set => _diffText.text = value; }
    public bool Solved { set => _solvedText.text = value ? "Solved" : ""; }
    public Board.State Board
    {
        get => _board;
        set
        {
             _board = value;
            if (value != null)
                _sizeText.text = $"{value.Numbers.GetLength(0)}x{value.Numbers.GetLength(1)}"; }
    }

    public event System.Action<Board.State> OnClicked;

    void OnEnable()
    {
        _button.onClick.AddListener(OnClick);
    }
    void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
    }
    private void OnClick() => OnClicked?.Invoke(_board);
}
