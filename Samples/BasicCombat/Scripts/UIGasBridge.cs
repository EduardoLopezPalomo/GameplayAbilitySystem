using UnityEngine;
using supercachola.AbilitySystem.Attributes;
using supercachola.AbilitySystem;
using supercachola.AbilitySystem.Tags;
using UnityEngine.UI;

public class UIGasBridge : MonoBehaviour
{
    [SerializeField] private AbilitySystemComponent target;
    [SerializeField] private GameplayTag attributeTag;
    [SerializeField] private GameplayTag maxAttributeTag;
    [SerializeField] private Image fillImage;
    
    private GameplayAttribute _attribute;
    private GameplayAttribute _maxAttribute;
    
    void OnEnable()
    {
        if (!target.TryGetAttribute(attributeTag, out _attribute))
        {
            Debug.LogWarning("No attribute found for " + attributeTag);
            return;
        }

        if (maxAttributeTag != GameplayTag.None && !target.TryGetAttribute(maxAttributeTag, out _maxAttribute))
        {
            Debug.LogWarning("No attribute found for " + attributeTag);
            return;
        }
        
        _attribute.ValueChanged += OnAttributeValueChanged;
        if(_maxAttribute != null)
            _maxAttribute.ValueChanged += OnAttributeValueChanged;
        
        OnAttributeValueChanged(_attribute, _attribute.currentValue);
    }

    void OnDisable()
    {
        if (_attribute != null)
            _attribute.ValueChanged -= OnAttributeValueChanged;
        
        if (_maxAttribute != null)
            _maxAttribute.ValueChanged -= OnAttributeValueChanged;

        _attribute = null;
        _maxAttribute = null;
    }
    
    private void OnAttributeValueChanged(GameplayAttribute attribute, float oldValue)
    {
        if (_attribute == null) return;
        
        FillImage(_attribute.currentValue, _attribute.MaxValue);
    }

    private void FillImage(float currentValue, float maxValue)
    {
        fillImage.fillAmount = currentValue / maxValue;
    }
}
