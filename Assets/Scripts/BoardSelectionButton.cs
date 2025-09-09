using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardSelectionButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _diffText;
    [SerializeField] private TMP_Text _sizeText;
    [SerializeField] private TMP_Text _solvedText;
    [SerializeField] private IBoard.State _board;
    [SerializeField] private MinifiedBoard _miniBoard;
    [SerializeField] private float _heightRatio = 2f;

    public string Difficulty { get => _diffText.text; set => _diffText.text = value; }
    public bool Solved { set => _solvedText.text = value ? "Solved" : ""; }
    public IBoard.State Board
    {
        get => _board;
        set
        {
            _board = value;
            if (value != null)
            {
                _sizeText.text = $"{value.Numbers.GetLength(0)}x{value.Numbers.GetLength(1)}";
                _miniBoard.Init(_board);
            }
        }
    }

    public event System.Action<IBoard.State> OnClicked;

    void OnEnable()
    {
        _button.onClick.AddListener(OnClick);

        RectTransform rectTransform = this.transform as RectTransform;

        Vector2 size = rectTransform.sizeDelta;
        size.y = size.x / _heightRatio;
        rectTransform.sizeDelta = size;
    }
    void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
    }
    private void OnClick() => OnClicked?.Invoke(_board);
}
