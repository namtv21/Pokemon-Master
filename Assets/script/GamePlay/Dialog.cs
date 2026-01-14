using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialog")]
public class Dialog : ScriptableObject
{
    [TextArea]
    [SerializeField] List<string> sentences;

    public List<string> Sentences => sentences;
}
