using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LinkHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private TMP_Text _textMessage;

    public void OnPointerClick(PointerEventData eventData) 
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(_textMessage, eventData.position, eventData.pressEventCamera);
        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = _textMessage.textInfo.linkInfo[linkIndex];
        string selectedLink = linkInfo.GetLinkID();//возращает выбранную ссылку
        if (!string.IsNullOrEmpty(selectedLink))
        {
            Debug.Log(selectedLink);
            Application.OpenURL(selectedLink);
        }

    }
}
