using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HiddenMaze
{
    [Serializable]
    public struct Coords
    {
        public int X;
        public int Y;

        public Coords (int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
        public static bool operator ==(Coords a, Coords b) 
        {
            return (a.X == b.X && a.Y == b.Y);
        }

        public static bool operator !=(Coords a, Coords b) 
        {
            return (a.X != b.X || a.Y != b.Y);
        }

        public override bool Equals(object rhs) 
        {
            if (rhs is Coords) {
                Coords c = (Coords) rhs;
                return X == c.X && Y == c.Y;
            }

            return false;
        }

        public override int GetHashCode() 
        {
            return X * Y + X + Y;
        }

        public bool isAdjacentTo(Coords rhs) 
        {
            return ((this.X == rhs.X + 1 && this.Y == rhs.Y) || (this.X == rhs.X - 1 && this.Y == rhs.Y)
            || (this.X == rhs.X && this.Y == rhs.Y + 1) || (this.X == rhs.X && this.Y == rhs.Y - 1));
        }
    }

    [Serializable]
    public class Maze
    {
        public Configs mConfigs;
        public List<Coords> mPath;
        public Coords mStart;
        public Coords mFinish;
        public Coords mNextStep;
        public int mNumSquares;
        public int mNumTurns;

        public Maze(Configs configs, List<Coords> path, Coords start, Coords finish) 
        {
            this.mConfigs = configs;
            this.mPath = path;
            this.mStart = start;
            this.mFinish = finish;
            this.mNextStep = start;
            this.mNumSquares = path.Count;
            this.mNumTurns = countTurns();
        }

        // Constructor w Configs obj
        public Maze(Configs configs) 
        {
            this.mConfigs = configs;
            this.mPath = new List<Coords>();
            this.mStart = new Coords();
            this.mFinish = new Coords();
            initStartAndFinish();
            findAndSetPath();

            this.mNextStep = this.mStart;
            this.mNumSquares = mPath.Count;
            this.mNumTurns = countTurns();
        }

        // Default constructor
        public Maze() 
        {
            this.mConfigs = new Configs();
            this.mPath = new List<Coords>();
            this.mStart = new Coords();
            this.mFinish = new Coords();
            initStartAndFinish();
            findAndSetPath();

            this.mNextStep = this.mStart;
            this.mNumSquares = mPath.Count;
            this.mNumTurns = countTurns();
        }

        // Copy constructor
        public Maze(Maze rhs) {
            this.mConfigs = rhs.mConfigs;
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

            this.mConfigs = jsonMaze.mConfigs;
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

        // initStartAndFinish
        // Finds random start and finish maze square coordinates
        // Ensures start and finish are not the same square
        // Configs:
        // sideRestricted - boolean: T if start square is restricted to top row of maze and
        //                           finish square is restricted to bottom row of maze
        //                           F if start and finish squares can be located anywhere
        private void initStartAndFinish()
        {
            Coords start = new Coords();
            Coords finish = new Coords();
            int dim = mConfigs.dim;
            var rand = new System.Random();

            // If dim > 1, properly set start and finish
            //   else set start and finish at (0, 0)
            if (dim > 1) {
                start.X = rand.Next(0, dim);
                finish.X = rand.Next(0, dim);

                // Limit start square to the bottom row of the maze
                // and the finish square to the top row
                if (mConfigs.sideRestricted) {
                    start.Y = 0;
                    finish.Y = dim - 1;
                
                } else { // Start and finish squares can be anywhere in the grid
                    do {
                        start.Y = rand.Next(0, dim);
                        finish.Y = rand.Next(0, dim);
                    } while (start == finish);
                }
            }

            mStart = start;
            mFinish = finish;
        }

        // findAndSetPath
        // 
        // RESETS CURRENT PATH!!
        // findAndSetPath returns a List of Coords that create a rule-abiding path from start to finish
        // The rules are that the path must never cross itself, not go out of bounds,
        // and each proceeding square must be side-adjacent to the last.
        private void findAndSetPath()
        {
            if (mStart == mFinish) {
                List<Coords> defaultPath = new List<Coords>();
                defaultPath.Add(mStart);
                mPath = defaultPath;
            }

            var rand = new System.Random();

            // Create current path and add start to it so we can't travel back onto it
            List<Coords> tempPath = new List<Coords>();
            tempPath.Add(mStart);

            Coords curr = mStart;
            Coords next;
            
            while (curr != mFinish) {
                // Pick a random direction to move WITHIN maze bounds from curr and not already on the path
                next = findAdjSquare(curr, tempPath);

                // whether we should backtrack is the opposite of whether we find the end from next
                Boolean backtrack = !DFS(next, mFinish, tempPath);

                // if there exists a valid path from next to finish...
                if (!backtrack) {
                    // Put next on path and treat next as an obstacle (we cannot move onto this square anymore)
                    tempPath.Add(next);
                    curr = next;
                }
            }

            // Assign to member path
            mPath = tempPath;
        }

        // Helper DFS to check if there is still a valid path from pos1 to pos2 without crossing
        // existing path
        private Boolean DFS(Coords pos1, Coords pos2, List<Coords> path)
        {
            Coords check;
            var visited = new HashSet<Coords>();
            Stack stack = new Stack();

            stack.Push(pos1);

            while (stack.Count > 0) 
            {
                check = (Coords)stack.Pop();

                if (visited.Contains(check))
                    continue;

                if (check.Equals(pos2)) {
                    return true;
                }

                visited.Add(check);

                Coords N = new Coords(check.X, check.Y + 1);
                Coords W = new Coords(check.X - 1, check.Y);
                Coords S = new Coords(check.X, check.Y - 1);
                Coords E = new Coords(check.X + 1, check.Y);
        
                if (dfsCheck(N, path)) { stack.Push(N); }
                if (dfsCheck(W, path)) { stack.Push(W); }
                if (dfsCheck(S, path)) { stack.Push(S); }
                if (dfsCheck(E, path)) { stack.Push(E); }
            }

            // DFS was unable to find finish square from the tested square
            return false;
        }

        // Helper check function for DFS, ensures that coord is within the maze and is not on path
        private Boolean dfsCheck(Coords coord, List<Coords> path) 
        {
            return validateSquare(coord) && !path.Contains(coord);
        }

        // Helper to return a square adj to curr that isn't on path
        private Coords findAdjSquare(Coords curr, List<Coords> path)
        {
            Coords next;
            int direction;
            var rand = new System.Random();

            do {
                next = curr;
                direction = rand.Next(0, 4);

                switch (direction)
                {
                    case 0:
                        next.X++;
                        break;
                    case 1:
                        next.X--;
                        break;
                    case 2:
                        next.Y++;
                        break;
                    case 3:
                        next.Y--;
                        break;
                    default:
                        Console.WriteLine("Invalid direction!");
                        break;
                }

            } while (!validateSquare(next) || path.Contains(next));

            return next;
        }

        // validateSquare
        // Returns T if the parameter Coords obj is within maze bounds and F if not
        private Boolean validateSquare(Coords coord)
        {
            return (coord.X < mConfigs.dim && coord.X >= 0 && coord.Y < mConfigs.dim && coord.Y >= 0);
        }

        // Returns a string graphical representation of maze
        private String prettyPrintMaze() {
            int dim = mConfigs.dim;
            int squareCounter = 2;  // Starts at 2 because Start is the first square
            // To allow for enough space to include multiple places for numbered paths
            int placesPerSquare = (int)Math.Floor(Math.Log10(mPath.Count)) + 1;

            if (dim <= 0) {
                String invalidMaze = String.Format("The maze is uninitialized or dimension was invalid (<=0).\nDebug Info:\nConfigs = {0}\nPath = {1}\nStart = {2}\nFinish = {3}\nNumSquares = {4}\nNumTurns = {5}", mConfigs, mPath, mStart, mFinish, mNumSquares, mNumTurns);
                return(invalidMaze);
            }

            StringBuilder sb = new StringBuilder();

            sb.Append($"Start: {mStart}  Finish: {mFinish}\n");
            sb.Append($"Number of Squares: {mNumSquares}\n");
            sb.Append($"Number of Turns: {mNumTurns}\n");
            sb.Append("\n");

            for (int y = dim - 1; y >= 0; --y) {
                for (int x = 0; x < dim; ++x) {

                    Coords testCoord = new Coords(x, y);

                    if (mStart == testCoord) {
                        sb.Append("S".PadLeft(placesPerSquare, ' ') + " ");
                    }
                    else if (mFinish == testCoord) {
                        sb.Append("F".PadLeft(placesPerSquare, ' ') + " ");
                    }
                    else if (mPath.Contains(new Coords(x, y))) {
                        int stepNum = mPath.IndexOf(testCoord);
                        // We add 1 to stepNum because the first step after start should be numbered 2, not 1.
                        sb.Append((stepNum + 1).ToString().PadLeft(placesPerSquare, '0') + " ");
                        squareCounter++;
                    } 
                    else {
                        sb.Append("-".PadLeft(placesPerSquare, ' ') + " ");
                    }
                }
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return prettyPrintMaze();
        }

        private int countTurns()
        {
            // If path only consists of S and F, impossible to have any turns
            if (mPath.Count <= 2) { return 0; }

            bool watchingForY; // F if watching for X changes, T if watching for Y changes

            int numTurns = 0;

            watchingForY = (mPath[0].X != mPath[1].X);

            for (int i = 1; i < mPath.Count - 1; ++i)
            {
                if (watchingForY && (mPath[i].Y != mPath[i + 1].Y)) {
                    watchingForY = false; // switch to looking for X changes
                    numTurns++;
                }

                if (!watchingForY && (mPath[i].X != mPath[i + 1].X)) {
                    watchingForY = true; // switch to looking for Y changes
                    numTurns++;
                }
            }

            return numTurns;
        }
    }
}