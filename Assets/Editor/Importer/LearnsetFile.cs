using System;
using System.Collections.Generic;

[Serializable]
public class LearnsetFile
{
    public string pokemon;
    public Dictionary<string, List<string>> learnset;
}