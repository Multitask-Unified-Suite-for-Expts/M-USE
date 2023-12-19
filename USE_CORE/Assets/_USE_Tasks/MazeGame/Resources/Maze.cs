using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using USE_Utilities;

namespace HiddenMaze
{
    [Serializable]
    public struct Coords
    {
        public string chessCoord;

        public Coords (string chessCoord)
        {
            this.chessCoord = chessCoord;
        }

        public bool IsAdjacent(Coords prevCoord)
        {
            Vector2 previousTouchCartesian = GetCartesian(prevCoord.chessCoord); // finds the cartesian equivalent of the chess notation for comparison
            Vector2 newTouchCartesian = GetCartesian(this.chessCoord); 
            float dx = Math.Abs(previousTouchCartesian[0]- newTouchCartesian[0]);
            float dy = Math.Abs(previousTouchCartesian[1] - newTouchCartesian[1]);

            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }

        public Vector2 GetCartesian(string coord)
        {
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int xCoord = alphabet.IndexOf(coord[0]);
            int yCoord = int.Parse(coord[1].ToString()); // convert char to int
            Vector2 cartesianCoord = new Vector2(xCoord, yCoord);
            return cartesianCoord;
        }

    }

    [Serializable]
    public class MazeJson
    {
        public List<string> mPath;
        public string mStart;
        public string mFinish;
        public string mNextStep;
        public int mNumSquares;
        public int mNumTurns;
        public List<int> mDims;
        [CanBeNull] public List<int> customDims;
        public string mName;
        public bool sideRestricted;
        public string gridStyle;
    }

    public class Maze
    {
        public List<string> mPath;
        public string mStart;
        public string mFinish;
        public string mNextStep;
        public int mNumSquares;
        public int mNumTurns;
        public Vector2 mDims;
        [CanBeNull] public List<int> customDims;
        public string mName;
        public bool sideRestricted;
        public string gridStyle;
        public bool loadingSquareMaze;


        public Maze(string jsonString)
        {
            MazeJson jsonMaze = JsonUtility.FromJson<MazeJson>(jsonString);
            Debug.LogWarning("IS CUSTOM DMS NULL? " + (jsonMaze.customDims == null? "yes": "no"));

            if(jsonMaze.mDims?.Count == 2)
            {
                // Assign values manually to the Maze class
                loadingSquareMaze = true;
                mDims = new Vector2(jsonMaze.mDims[0], jsonMaze.mDims[1]);
                mNumTurns = jsonMaze.mNumTurns;
                mPath = jsonMaze.mPath;
            }
            else
                customDims = jsonMaze.customDims;

            mNextStep = jsonMaze.mStart;

            mNumSquares = jsonMaze.mNumSquares;
            mName = jsonMaze.mName;
           // mPath = jsonMaze.mPath; // figure out how to filter based on free play or provide the shortest path
            mStart = jsonMaze.mStart;
            mFinish = jsonMaze.mFinish;
            sideRestricted = jsonMaze.sideRestricted;

        }


        // Create Maze object from JSON representation
        public static Maze CreateFromJSON(string jsonString)
        {
            Maze jsonMaze = JsonUtility.FromJson<Maze>(jsonString);

            // This is required because maze generator does not create a field for next step, but the game requires a next step
            jsonMaze.mNextStep = jsonMaze.mStart;

            return jsonMaze;
        }
    }
}