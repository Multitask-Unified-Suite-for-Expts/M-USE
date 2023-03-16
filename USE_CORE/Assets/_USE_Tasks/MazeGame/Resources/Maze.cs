using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    public class Maze
    {
        public List<String> mPath;
        public string mStart;
        public string mFinish;
        public string mNextStep;
        public int mNumSquares;
        public int mNumTurns;
        public Vector2 mDims;
        public string mName;
        public bool sideRestricted;
       
        // Copy Constructor
        public Maze(Maze rhs) {
            this.mDims = rhs.mDims;
            this.mPath = rhs.mPath;
            this.mStart = rhs.mStart;
            this.mFinish = rhs.mFinish;
            this.mNextStep = rhs.mNextStep;
            this.mNumSquares = rhs.mNumSquares;
            this.mNumTurns = rhs.mNumTurns;
        }

        // Create Maze object from JSON representation
        public Maze(string jsonString)
        {
            Maze jsonMaze = JsonUtility.FromJson<Maze>(jsonString);

            // Required because maze generator does not create a field for next step, but the game requires a next step
            jsonMaze.mNextStep = jsonMaze.mStart;
            this.mDims = jsonMaze.mDims;
            this.sideRestricted = jsonMaze.sideRestricted;
            this.mPath = jsonMaze.mPath;
            this.mStart = jsonMaze.mStart;
            this.mFinish = jsonMaze.mFinish;
            this.mNextStep = jsonMaze.mNextStep;
            this.mNumSquares = jsonMaze.mNumSquares;
            this.mNumTurns = jsonMaze.mNumTurns;
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