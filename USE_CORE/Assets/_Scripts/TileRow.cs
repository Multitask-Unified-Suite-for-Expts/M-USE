using UnityEngine;
using System.Collections;

public class TileRow
{
    public Tile[] mTiles;
    public int mDim;

    public TileRow() {
        mDim = 0;
        mTiles = new Tile[mDim];
    }
    
    public TileRow(int dim) {
        mDim = dim;
        mTiles = new Tile[mDim];
    }

   /*  ~TileRow()
    {
        mDim = 0;
        mTiles.setActive(false);
    }*/

    public TileRow(int dim, Tile[] tiles) {
        mDim = dim;
        mTiles = tiles;
    }
}