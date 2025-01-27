using System;
using System.Collections.Generic;
[System.Serializable]
public class Template
{
    public string name;
    public string lastname;
    public string newsType;
    public string sourceType;

    public Template(string name, string lastname, string newsType, string sourceType)
    {
        this.name = name;
        this.lastname = lastname;
        this.newsType = newsType;
        this.sourceType = sourceType;
    }
}