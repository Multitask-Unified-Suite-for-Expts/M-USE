/*
This software is part of the Unified Suite for Experiments (USE).
Information on USE is available at
http://accl.psy.vanderbilt.edu/resources/analysis-tools/unifiedsuiteforexperiments/

Copyright (c) <2018> <Marcus Watson>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

1) The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
2) If this software is used as a component of a project that leads to publication
(e.g. a paper in a scientific journal or a student thesis), the published work
will give appropriate attribution (e.g. citation) to the following paper:
Watson, M.R., Voloh, B., Thomas, C., Hasan, A., Womelsdorf, T. (2018). USE: An
integrative suite for temporally-precise psychophysical experiments in virtual
environments for human, nonhuman, and artificially intelligent agents. BioRxiv:
http://dx.doi.org/10.1101/434944

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// http://www.jsoneditoronline.org/
// http://pressonegames.com/parsing-json-files-in-unity/

using UnityEngine;
using System;
//using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using USE_DisplayManagement;
////using System.IO;
//using System.Reflection;
//using System.ComponentModel;
////using LitJson;
////using UnityEngine.SceneManagement;
////using USE_Common_Namespace;
using Newtonsoft.Json;

namespace ConfigParsing {
    public static class ConfigParsing{

        // Parses the text from textfile
        public static Settings parseConfigFile(string configPath){
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
                Debug.Log("The file could not be read");

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
                            string s ="cant parse line " + iline + " in " + configPath; 
                            settingsOut.warning = s;
                            Debug.Log(s);
                            Debug.Log(line);
                            Debug.Log(fields[0]);
                        }
                    }

                    iline++;
                }

                //create json string
                Settings tmpsettings = new Settings();

                string jsonstring = "";
                Dictionary<string, string> settingFields = new Dictionary<string, string>(tmpsettings.GetTypeMapping());

                foreach (KeyValuePair<string, string> KV in settingFields)
                {
                    string supportedType = KV.Key;
                    string targetStream = KV.Value;

                    //format the value string first
                    string newval = "";
                    foreach (Token tok in tokens)
                    {
                        if (tmpsettings.IsTypeSupported(tok.type))
                        {
                            if (supportedType.Equals(tok.type))
                            {
                                //format string
                                newval = newval + '"' + tok.name + '"' + ":" + tok.value + ",";
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


        private static T JsonMapperWrapper<T>(string jsonstr)
        {

            //string newstr = ConvertConfigString (jsonstr);
            //string newstr = jsonstr;
            //T obj = JsonMapper.ToObject<T>(newstr);
            //T obj = JsonMapper.ToObject<T>(jsonstr);
            T obj = JsonConvert.DeserializeObject<T>(jsonstr);
            return obj;
            //return obj;
        }


        static List<int[]> ReadIntArrayFile(string filePath)
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
                Debug.Log("The file could not be read");

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



    // class to store settings
    // - definitions: data stream=the fields that are dictionaries that will hold particular types of data
    // - each dictionary has key,value pair "variable name" and value
    // - the parser will read the config files, figure out the types, and add them to the appropriate data stream
    // - Although not strictily necessary, for ease of readability, keep the fields names sensibly named
    //		- the proposed way isto name the fields in the same way that a type is declared. 
    //		- For example, a Dictionary<string,Dictionary<Int>> type would be encoded in a field called
    //		  DictionaryStringDictionaryInt
    [Serializable]
    public class Settings {
        public string fullPath;
        public string warning = "";

        public List<string> supportedTypes;

        // fields for data streams. This is what the JSON reader will target. Remember to update 
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

        // update this dictionary if you add new data types
        private Dictionary<string,string> typestring2streamstring = new Dictionary<string, string> (){
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
            };
        //constructor
        public Settings(){
            supportedTypes = new List<string>(typestring2streamstring.Keys.ToList());
        }

        public Dictionary<string,string> GetTypeMapping(){
            return typestring2streamstring;
        }
        // METHODS
        public bool IsTypeSupported(string targetType)
        {
            bool flag = this.supportedTypes.Contains(targetType);

            return flag;
        }
    }
}
