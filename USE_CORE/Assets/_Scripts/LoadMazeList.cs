using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using HiddenMaze;

public class LoadMazeList : MonoBehaviour
{
    public List<Maze> mazeList = new List<Maze>();

    void Awake()
    {
        // Load maze from JSON
        TextAsset[] textMazes = Resources.LoadAll<TextAsset>("Mazes");

        foreach (TextAsset textMaze in textMazes) {
            string mazeJson = textMaze.text;
            Maze mazeObj = new Maze(mazeJson);
            Debug.Log(mazeObj);
            mazeList.Add(mazeObj);
        }

        foreach (Maze maze in mazeList) {
            // TODO: Here is where the maze levels can be put in order
        }
    }
}