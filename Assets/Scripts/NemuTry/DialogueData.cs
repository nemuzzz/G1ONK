using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(3, 10)]
        public string content;
    }

    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}