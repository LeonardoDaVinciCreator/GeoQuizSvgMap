using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectSvgElementUI : MonoBehaviour
{
    [SerializeField] private SelectElementSvg _selectElementSvg;
    [SerializeField] private TextMeshProUGUI _inputField;
    [SerializeField, Range(1, 10)] private float _scale = 10f;

    public void OnButtonClick()
    {
        string raw = _inputField.text;
        string cleaned = raw.Trim();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^a-zA-Z0-9_-]", "");

        Debug.Log($"[DEBUG] Trying to select country with ID: '{cleaned}'");
        Debug.Log($"[DEBUG] Length: {cleaned.Length}");
        Debug.Log($"[DEBUG] Bytes: {string.Join(", ", System.Text.Encoding.UTF8.GetBytes(cleaned))}");

        string countryId = _inputField.text;
        _selectElementSvg.SelectCountry(cleaned, _scale);


    }
}
