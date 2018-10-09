// http://www.jsoneditoronline.org/
// http://pressonegames.com/parsing-json-files-in-unity/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using LitJson;
using UnityEngine.SceneManagement;
using USE_Common_Namespace;
using FLU_Common_Namespace;
using Newtonsoft.Json;

public class ConfigReader : MonoBehaviour
{
    public LocateFile.LocateFile locatefile;
    public ExperimentInfo experimentInfo;
    public static string filePrefix;

    public static Settings sessionSettings;
    public static Settings replaySettings;

    public static Settings exptParameterSettings;
    public static Settings blockDefSettings;

    public static List<int[]> rulesets;
    public static List<int[]> transitions;
    public static List<int[]> usedRulesets;


    void OnEnable()
    {
        filePrefix = experimentInfo.ExperimentName + "__" + experimentInfo.SubjectName + "__" + DateTime.Today.ToString("dd_MM_yyyy") + "__" + DateTime.Now.ToString("HH_mm_ss");

        string rootPath = SetRootPaths();

        string basePath = rootPath + Path.DirectorySeparatorChar + "Config" + Path.DirectorySeparatorChar;

        string configSessionPath;
        string configReplayPath;
        //string rulePath;
        //string transitionPath;
        //string usedRulePath;
        string blockfilePath;
        string exptParamfilePath;

        configSessionPath = basePath + "config_flu_session.txt";
        configReplayPath = basePath + "config_replayer.txt";
        //rulePath = basePath + "Ruleset Details.txt";
        //transitionPath = basePath + "Transition Details.txt";
        //usedRulePath = basePath + "Used_Rulesets.txt";

        //parse for settings
        sessionSettings = parseConfigFile(configSessionPath);
        replaySettings = parseConfigFile(configReplayPath);

        //FLU-specific
        blockfilePath = locatefile.GetPath("Block Definition");
        exptParamfilePath = locatefile.GetPath("Experiment Parameter");
        blockDefSettings = parseConfigFile(blockfilePath);
        exptParameterSettings = parseConfigFile(exptParamfilePath);


        //set some paths
        string dataPath;
        if (!sessionSettings.String.Keys.Contains("dataPath"))
        {
            dataPath = rootPath + Path.DirectorySeparatorChar + "logs";
        }
        else
        {
            dataPath = sessionSettings.String["dataPath"];
        }


        //update with the paths
        sessionSettings.String.Add("basePath", basePath);
        sessionSettings.String.Add("configSessionPath", configSessionPath);
        sessionSettings.String.Add("rootPath", rootPath);
        //sessionSettings.String.Add("usedRulePath", usedRulePath);


        //save the config files...easier to do it here since all info is here and blockdef and Exptparameters are seperate
        if (sessionSettings.Bool["storeData"])
        {
            string configFolderOut = sessionSettings.String["dataPath"] + filePrefix + "/RuntimeData/" + "/ConfigCopy/";
            Directory.CreateDirectory(configFolderOut);

            File.Copy(configSessionPath, configFolderOut + Path.DirectorySeparatorChar + "config_flu_session.txt");
            File.Copy(configReplayPath, configFolderOut + Path.DirectorySeparatorChar + "config_replayer.txt");
            //File.Copy(rulePath, configFolderOut + Path.DirectorySeparatorChar + "Ruleset Details.txt");
            //File.Copy(transitionPath, configFolderOut + Path.DirectorySeparatorChar + "Transition Details.txt");
            //File.Copy(usedRulePath, configFolderOut + Path.DirectorySeparatorChar + "Used_Rulesets.txt");

            File.Copy(blockfilePath, configFolderOut + Path.DirectorySeparatorChar + Path.GetFileName(blockfilePath));
            File.Copy(exptParamfilePath, configFolderOut + Path.DirectorySeparatorChar + Path.GetFileName(exptParamfilePath));
        }

    }


    private string SetRootPaths()
    {
        string rootPath = Application.dataPath;
        // either Unity path, or same level as the built game path
        if (rootPath.Substring(rootPath.Length - 6, 6).Equals("Assets"))
        {
            rootPath = rootPath + Path.DirectorySeparatorChar + "Textfiles";
        }
        else if (rootPath.Substring(rootPath.Length - 8, 8).Equals("Contents"))
        {
            rootPath = Directory.GetParent(Directory.GetParent(rootPath).FullName).FullName;
        }

        return rootPath;
    }


    // Parses the text from textfile
    public Settings parseConfigFile(string configPath)
    {
        Settings settingsOut = new Settings();

        //indices for config parser
        int ivartype = 0;
        int ivarname = 1;
        int ivarvalue = 2;

        //read the file
        StreamReader textFile;
        try
        {
            //read in all data and parse it
            textFile = new StreamReader(configPath);
        }
        catch (Exception e)
        {
            print("The file could not be read");

            string err = e.Message + "\t" + e.StackTrace;
            throw new System.ArgumentException(err);
        }

        //parse for settings
        if (!(textFile == null || textFile == (default(StreamReader))))
        {
            string line;
            List<Token> tokens = new List<Token>();

            int iline = 0;
            while ((line = textFile.ReadLine()) != null)
            {



                if (!(line.StartsWith("//") || String.IsNullOrEmpty(line)))
                { //ignore commenetd out lines
                    line = line.Trim();
                    while (line.EndsWith("..."))
                    {
                        line = line.Remove(line.Length - 3);
                        string newLine = textFile.ReadLine().Trim();
                        iline++;
                        while (String.IsNullOrEmpty(newLine))
                        {
                            newLine = textFile.ReadLine().Trim();
                            iline++;
                        }
                        line = line + newLine;
                    }

                    string[] fields = line.Split('\t');

                    try
                    {
                        string v_type = fields[ivartype];
                        string v_name = fields[ivarname];
                        string v_val = fields[ivarvalue];

                        Token tok = new Token(v_name, v_type, v_val);
                        tokens.Add(tok);
                    }
                    catch (Exception e)
                    {
                        Debug.Log("cant parse line " + iline + " in " + configPath);
                        Debug.Log(line);
                        Debug.Log(fields[0]);
                    }
                }

                iline++;
            }

            //create json string
            Settings tmpsettings = new Settings();

            string jsonstring = "";
            Dictionary<string, string> settingFields = new Dictionary<string, string>(tmpsettings.typestring2streamstring);

            foreach (KeyValuePair<string, string> KV in settingFields)
            {
                string supportedType = KV.Key;
                string targetStream = KV.Value;

                //format the value string first
                string newval = "";
                foreach (Token tok in tokens)
                {
                    //Token tok = tokens[ii];

                    if (tmpsettings.IsTypeSupported(tok.type))
                    {
                        if (supportedType.Equals(tok.type))
                        {
                            //format string
                            newval = newval + '"' + tok.name + '"' + ":" + tok.value + ",";

                            //tokens.RemoveAt(ii);
                            //tokens.Remove(tok);
                        }
                    }
                    else
                    {
                        throw new Exception("Variable " + tok.name + " is of type " + tok.type + ", which is not supported. Update the Settings class");
                    }
                }

                //finish formatting the value
                if (newval.Length != 0)
                {
                    newval = "{" + newval.Substring(0, newval.Length - 1) + "}";
                    jsonstring = jsonstring + '"' + targetStream + '"' + ":" + newval + ",";
                }

            }

            //finish formatting the json string
            if (jsonstring.Length != 0)
            {
                jsonstring = "{" + jsonstring.Substring(0, jsonstring.Length - 1) + "}";

                jsonstring = jsonstring.Replace('“', '"');
                jsonstring = jsonstring.Replace('”', '"');

                //desereialize
                try
                {
                    settingsOut = JsonMapperWrapper<Settings>(jsonstring);
                }
                catch (Exception e)
                {
                    Debug.Log(jsonstring);
                    Debug.Log("Error reading file " + configPath + ".\n" + e);
                }
            }
            else
            {
                Debug.Log("WARNING: empty config file");
            }

        }
        textFile.Close();

        settingsOut.fullPath = configPath;
        return settingsOut;
    }

    public class Token
    {
        public string name;
        public string type;
        public string value;

        public Token(string name, string type, string value)
        {
            this.name = name;
            this.type = type;
            this.value = value;
        }
    }

    private T JsonMapperWrapper<T>(string jsonstr)
    {

        //string newstr = ConvertConfigString (jsonstr);
        //string newstr = jsonstr;
        //T obj = JsonMapper.ToObject<T>(newstr);
        //T obj = JsonMapper.ToObject<T>(jsonstr);
        T obj = JsonConvert.DeserializeObject<T>(jsonstr);
        return obj;
        //return obj;
    }


    // class to store settings
    // - definitions: data stream=the fields that are dictionaries that will hold particular types of data
    // - each dictionary has key,value pair "variable name" and value
    // - the parser will read the config files, figure out the types, and add them to the appropriate data stream
    // - Although not strictily necessary, for ease of readability, keep the fields names sensibly named
    //		- the proposed way isto name the fields in the same way that a type is declared. 
    //		- For example, a Dictionary<string,Dictionary<Int>> type would be encoded in a field called
    //		  DictionaryStringDictionaryInt
    [Serializable]
    public class Settings
    {

        public string basePath;
        public string fullPath;

        public List<string> supportedTypes;
        public Dictionary<string, string> typestring2streamstring;

        // fields for data streams. This is what the JSON reader will target
        public Dictionary<string, List<string>> ListString;
        public Dictionary<string, List<bool>> ListBool;
        public Dictionary<string, List<int>> ListInt;
        public Dictionary<string, List<float>> ListFloat;
        public Dictionary<string, string> String;
        public Dictionary<string, bool> Bool;
        public Dictionary<string, int> Int;
        public Dictionary<string, float> Float;
        public Dictionary<string, string[]> StringArray;
        public Dictionary<string, bool[]> BoolArray;
        public Dictionary<string, int[]> IntArray;
        public Dictionary<string, float[]> FloatArray;
        public Dictionary<string, Dictionary<string, int>> DictionaryStringInt;
        public Dictionary<string, Dictionary<string, List<string>>> DictionaryStringListString;
        public Dictionary<string, SortedList<string, List<string>>> SortedListStringListString;
        public Dictionary<string, List<string[]>> ListStringArray;
        public Dictionary<string, MonitorDetails> MonitorDetails;

        //Flu-specific details
        public Dictionary<string, ExptParameters> ExptParameters;
        public Dictionary<string, BlockDef> BlockDef;
        public Dictionary<string, BlockDef[]> BlockDefArray;
        public Dictionary<string, RuleDef> RuleDef;
        public Dictionary<string, TrialDef> TrialDef;
        public Dictionary<string, StimDef> StimDef;

        //constructor
        public Settings()
        {

            // update this dictionary if you add new data types
            typestring2streamstring = new Dictionary<string, string>()
            {
                {"List<string>","ListString"},
                {"List<bool>","ListBool"},
                {"List<int>","ListInt"},
                {"List<float>","ListFloat"},
                {"string","String"},
                {"bool","Bool"},
                {"int","Int"},
                {"float","Float"},
                {"string[]","StringArray"},
                {"bool[]","BoolArray"},
                {"int[]","IntArray"},
                {"float[]","FloatArray"},
                {"Dictionary<string,List<string>>","DictionaryStringListString"},
                {"Dictionary<string,int>","DictionaryStringInt"},
                {"SortedList<string,List<string>>","SortedListStringListString"},
                {"List<string[]>>","ListStringArray"},
                {"MonitorDetails","MonitorDetails"},
                {"ExptParameters", "ExptParameters"},
                {"BlockDef", "BlockDef"},
                {"BlockDef[]", "BlockDefArray"},
                {"RuleDef", "RuleDef"},
                {"TrialDef", "TrialDef"},
                {"StimDef", "StimDef"}
                };

            supportedTypes = new List<string>(typestring2streamstring.Keys.ToList());
        }

        // METHODS
        public bool IsTypeSupported(string targetType)
        {
            bool flag = this.supportedTypes.Contains(targetType);

            return flag;
        }
    }


    List<int[]> ReadIntArrayFile(string filePath)
    {
        List<int[]> newList = new List<int[]>();
        //read the file
        StreamReader textFile;
        try
        {
            //read in all data and parse it
            textFile = new StreamReader(filePath);
        }
        catch (Exception e)
        {
            print("The file could not be read");

            string err = e.Message + "\t" + e.StackTrace;
            throw new System.ArgumentException(err);
        }

        string line;
        while ((line = textFile.ReadLine()) != null)
        {
            if (!(line.StartsWith("//")))
            {
                string[] fields = line.Split('\t');
                int[] lineData = new int[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    lineData[i] = int.Parse(fields[i]);
                }
                newList.Add(lineData);
            }
        }
        return newList;
    }
}
