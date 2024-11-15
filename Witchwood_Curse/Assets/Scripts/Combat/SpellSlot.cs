using UnityEngine;

public class SpellSlot : MonoBehaviour
{
    [SerializeField] private Spell currentSpell;
    public Spell CurrentSpell => currentSpell;
    
    public void EquipSpell(Spell spell)
    {
        currentSpell = spell;
    }
} 