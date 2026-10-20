using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class SensitiveWordFilter
{
    private static SensitiveWordFilter _instance;
    private HashSet<string> sensitiveWords = new HashSet<string>();
    private bool isInitialized = false;

    // u5355u4f8bu6a21u5f0f
    public static SensitiveWordFilter Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SensitiveWordFilter();
                _instance.Initialize();
            }
            return _instance;
        }
    }

    // u521du59cbu5316u654fu611fu8bcdu5e93
    private void Initialize()
    {
        if (isInitialized) return;

        LoadSensitiveWordsFromResource();
        isInitialized = true;
    }

    // u4eceu8d44u6e90u6587u4ef6u52a0u8f7du654fu611fu8bcd
    private void LoadSensitiveWordsFromResource()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("SensitiveWords");
        if (textAsset == null)
        {
            Debug.LogError("u65e0u6cd5u52a0u8f7du654fu611fu8bcdu5e93u6587u4ef6uff01");
            return;
        }

        string[] lines = textAsset.text.Split('\n');
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            // u8df3u8fc7u7a7au884cu548cu6ce8u91cau884c
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            sensitiveWords.Add(trimmedLine.ToLower());
        }

        Debug.Log($"u5df2u52a0u8f7d {sensitiveWords.Count} u4e2au654fu611fu8bcd");
    }

    // u68c0u67e5u6587u672cu662fu5426u5305u542bu654fu611fu8bcd
    public bool ContainsSensitiveWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        string lowerText = text.ToLower();
        foreach (string word in sensitiveWords)
        {
            if (lowerText.Contains(word))
                return true;
        }
        
        return false;
    }

    // u8fc7u6ee4u654fu611fu8bcduff0cu66ffu6362u4e3a*u53f7
    public string FilterSensitiveWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string result = text;
        string lowerText = text.ToLower();

        foreach (string word in sensitiveWords)
        {
            if (lowerText.Contains(word))
            {
                // u521bu5efau76f8u540cu957fu5ea6u7684*u53f7u5b57u7b26u4e32
                string replacement = new string('*', word.Length);
                
                // u4f7fu7528u6b63u5219u8868u8fbeu5f0fu8fdbu884cu66ffu6362uff0cu5ffdu7565u5927u5c0fu5199
                result = Regex.Replace(result, word, replacement, RegexOptions.IgnoreCase);
            }
        }

        return result;
    }

    // u6dfbu52a0u65b0u7684u654fu611fu8bcd
    public void AddSensitiveWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return;
        sensitiveWords.Add(word.ToLower());
    }

    // u79fbu9664u654fu611fu8bcd
    public void RemoveSensitiveWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return;
        sensitiveWords.Remove(word.ToLower());
    }

    // u83b7u53d6u6240u6709u654fu611fu8bcd
    public List<string> GetAllSensitiveWords()
    {
        return new List<string>(sensitiveWords);
    }
}
