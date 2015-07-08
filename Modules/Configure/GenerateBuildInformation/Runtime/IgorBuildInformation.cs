using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public static class IgorBuildInformation
{		
    static Dictionary<string, string> _buildInformationDictionary;

    public static string Get(string inKey)
    {
        EnsureInitialized();
        string value = "";
        _buildInformationDictionary.TryGetValue(inKey, out value);
        return value;
    }

    public static IEnumerable<KeyValuePair<string, string>> GetAll()
    {
        EnsureInitialized();
        return _buildInformationDictionary;
    }

    static void EnsureInitialized()
    {
        if(_buildInformationDictionary == null)
        {
            _buildInformationDictionary = new Dictionary<string,string>();

            string[] KVPs = ReadTextFile();
            if(KVPs != null)
            {
                foreach(string KVP in KVPs)
                {
                    string[] KeyAndValue = KVP.Split('=');
                    if(KeyAndValue.Length == 2)
                    {
                        _buildInformationDictionary.Add(KeyAndValue[0], KeyAndValue[1]);
                    }
                }
            }
        }
    }

    static string[] ReadTextFile()
    {
        string[] FileContents = null;

        TextAsset text = Resources.Load("IgorBuildInformation_Text") as TextAsset;
        if(text != null)
        {
            FileContents = text.text.Split('\n');
        }

        return FileContents;
    }
}