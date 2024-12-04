using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellUI : MonoBehaviour
{
    [SerializeField] private Spell spell;
    [SerializeField] private Image img;

    void Update(){
        img.sprite = spell.icon;
    }
}
