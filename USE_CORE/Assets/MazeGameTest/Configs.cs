using System;
using UnityEngine;

namespace HiddenMaze
{
    [Serializable]
    // Configuration settings for variability between mazes
    public class Configs
    {
        public int dim;
        public Boolean sideRestricted;
        public Configs()
        {
            this.dim = 0;
            this.sideRestricted = true;
        }
        public Configs(int dim, Boolean sideRestricted)
        {
            this.dim = dim;
            this.sideRestricted = sideRestricted;
        }
    }
}