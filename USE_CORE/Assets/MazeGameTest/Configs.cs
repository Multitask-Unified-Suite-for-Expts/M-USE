using System;
using UnityEngine;

namespace HiddenMaze
{
    [Serializable]
    // Configuration settings for variability between mazes
    public class Configs
    {
        public Vector2 dim;
        public Boolean sideRestricted;
        public Configs()
        {
            this.dim = new Vector2(0,0);
            this.sideRestricted = true;
        }
        public Configs(Vector2 dim, Boolean sideRestricted)
        {
            this.dim = dim;
            this.sideRestricted = sideRestricted;
        }
    }
}