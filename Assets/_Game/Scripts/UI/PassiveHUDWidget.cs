using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Widget passif en bas à gauche — icône + label "Passif", tooltip au survol.
/// </summary>
public class PassiveHUDWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Icône")]
    public Image iconImage;

    [Header("Tooltip")]
    public GameObject      tooltipPanel;
    public TextMeshProUGUI tooltipName;
    public TextMeshProUGUI tooltipDesc;

    PassiveData _passive;

    public void SetPassive(PassiveData passive)
    {
        _passive = passive;

        // Auto-récupère l'icône si la référence sérialisée est perdue
        if (iconImage == null)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.name == "Icon" && !img.raycastTarget)
                { iconImage = img; break; }
            }
        }

        if (iconImage != null)
        {
            if (passive == null)
            {
                iconImage.enabled = false;
            }
            else
            {
                Sprite resolvedIcon = passive.icon != null
                    ? passive.icon
                    : OracleHudRuntimeSprites.LoadPassiveIcon();
                iconImage.sprite         = resolvedIcon;
                iconImage.color          = Color.white;
                iconImage.preserveAspect = true;
                iconImage.enabled        = resolvedIcon != null;
            }
        }

        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_passive == null || tooltipPanel == null) return;
        if (tooltipName != null) tooltipName.text = _passive.passiveName;
        if (tooltipDesc  != null) tooltipDesc.text  = _passive.description;
        tooltipPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}
