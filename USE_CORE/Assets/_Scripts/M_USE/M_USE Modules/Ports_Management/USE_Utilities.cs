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

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace USE_Utilities {



	public static class Trig{
		public static float hypotenuseLength(float A, float B) {

			float h = Mathf.Sqrt (A * A + B * B);
			return h;
		}

		public static float squareSideLength(float h) {
			float A = Mathf.Sqrt (Mathf.Pow (h, 2) / 2);
			return A;
		}	
	}


	public static class Randomization {

		//return list of indices that have been randomly permuted

		public static List<int> randperm (int n) {
			List<int> iList = randperm (n, n);
			return iList;
		}

		public static List<int> randperm (int n, int k) {
			List<int> ind = new List<int> ();
			for (int ii = 0; ii < n; ii++) {
				ind.Add (ii);
			}

			List<int> iList = new List<int> ();
			int irand=0;
			for (int ii=0; ii < k; ii++){
				int itmp = UnityEngine.Random.Range (0, ind.Count);
				irand = ind [itmp];
				iList.Add(irand);
				ind.RemoveAt (itmp);
			}

			return iList;
		}


		public static Vector2 onUnitCircle(){
			return onUnitCircle (0f, Mathf.PI * 2);
		}
		public static Vector2 onUnitCircle(float min, float max){
			float angle = UnityEngine.Random.Range (min, max);
			float x = Mathf.Sin (angle);
			float y = Mathf.Cos (angle);
			return new Vector2 (x, y);
		}
	}

	public static class ArrayUtils{
		//holy balls everything other than matlab sucks when dealing with arrays

		public static int[] GetRowArray (int[,] MyArray, int rowNum){
			int numCols = MyArray.GetLength (1);
			int[] row = new int[numCols];
			for (int colCount = 0; colCount < numCols; colCount++) {
				row [colCount] = MyArray [rowNum, colCount];
			}
			return row;
		}

		public static List<int> GetRowList (List<List<int>> MyArray, int rowNum){
			int numCols = MyArray[0].Count;
			List<int> row = new List<int>();
			for (int colCount = 0; colCount < numCols; colCount++) {
				row.Add(MyArray [rowNum][colCount]);
			}
			return row;
		}

		public static int[] GetColArray (int[,] MyArray, int colNum){
			int numRows = MyArray.GetLength (0);
			int[] col = new int[numRows];
			for (int rowCount = 0; rowCount < numRows; rowCount++) {
				col [rowCount] = MyArray [rowCount, colNum];
			}
			return col;
		}

		public static List<int> GetColList (List<List<int>> MyArray, int colNum){
			int numRows = MyArray.Count;
			List<int> col = new List<int>();
			for (int rowCount = 0; rowCount < numRows; rowCount++) {
				col.Add(MyArray [rowCount][colNum]);
			}
			return col;
		}

		public static List<int> GetColList (List<int[]> MyArray, int colNum){
			int numRows = MyArray.Count;
			List<int> col = new List<int>();
			for (int rowCount = 0; rowCount < numRows; rowCount++) {
				col.Add(MyArray [rowCount][colNum]);
			}
			return col;
		}

		public static int[,] PlaceRow(int[,] MyArray, int[] row, int rowNum){
			int numCols = MyArray.GetLength (1);
			for (int colCount = 0; colCount < numCols; colCount++) {
				MyArray [rowNum, colCount] = row [colCount];
			}
			return MyArray;
		}

		public static List<int> Find1dIn2d(int[,] TwoDimArray, int[] oneDimArray, int dim){
			List<int> matches = new List<int>();
			for (int i = 0; i < TwoDimArray.GetLength(dim); i++) {
				int[] test = new int[oneDimArray.Length];
				if (dim == 0) {
					test = GetRowArray (TwoDimArray, i);
				} else if (dim == 1) {
					test = GetColArray (TwoDimArray, i);
				}
				if(Enumerable.SequenceEqual(oneDimArray,test)){
					matches.Add (i);
				}
			}
			return matches;
		}

		public static int[] FindAllIndexof<T>(this IEnumerable<T> values, T val){
			return values.Select((b,i) => object.Equals(b, val) ? i : -1).Where(i => i != -1).ToArray();
		}
		
		public static int[] FindAll(int[] Vector, int Target)
		{
			var result = Enumerable.Range(0, Vector.Length).Where(i => Vector[i] == Target).ToArray();
			return result;
		}

		public static int[] ShuffleIntArray(int[] MyArray){
			for (int count = 0; count < MyArray.Length; count++) {
				int tmp = MyArray [count];
				int newIndex = UnityEngine.Random.Range (count, MyArray.Length);
				MyArray [count] = MyArray [newIndex];
				MyArray [newIndex] =tmp;
			}
			return MyArray;
		}

		public static int[,] ShuffleIntArray(int[,] MyArray){
			for (int rowCount = 0; rowCount < MyArray.GetLength(0); rowCount++) {
				int[] tmp = new int[MyArray.GetLength (1)];
				for (int colCount = 0; colCount < MyArray.GetLength (1); colCount++) {
					tmp [colCount] = MyArray [rowCount, colCount];
				}
				int newIndex = UnityEngine.Random.Range (rowCount, MyArray.GetLength(0));
				for (int colCount = 0; colCount < MyArray.GetLength (1); colCount++) {
					MyArray [rowCount,colCount] = MyArray [newIndex,colCount];
					MyArray [newIndex,colCount] = tmp[colCount];
				}
			}
			return MyArray;
		}

		public static int[] ArraySubset(int[] MyArray, int[] indices){
			int[] subset = new int[indices.Length];
			for (int i = 0; i < indices.Length; i++) {
				subset [i] = MyArray [indices [i]];
			}
			return subset;
		}

		public static int[,] ArraySubset(List<int[]> MyArray, int[] indices){
			int[,] subset = new int[indices.Length, MyArray[0].Length];
			for (int i = 0; i < indices.Length; i++) {
				for (int j = 0; j < MyArray[0].Length; j++) {
					subset [i,j] = MyArray [indices [i]][j];
				}
			}
			return subset;
		}

		public static int[,] ArraySubset(int[,] MyArray, int[] indices){
			int[,] subset = new int[indices.Length, MyArray.GetLength(1)];
			for (int i = 0; i < indices.Length; i++) {
				for (int j = 0; j < MyArray.GetLength (1); j++) {
					subset [i,j] = MyArray [indices [i],j];
				}
			}
			return subset;
		}
	}

	public static class TimeStamp{
		public static long ConvertToUnixTimestamp(DateTime date)
		{
			DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			TimeSpan diff = (date.ToUniversalTime() - origin);
			return diff.Ticks;
		}
	}

	public static class UnityExtension{
		public static GameObject GetChildByName (GameObject parent, string childName){
			foreach (Transform child in parent.transform) {
				if (child.name.Equals (childName)) {
					return child.gameObject;
				}
			}

			return null;
		}
	}

	public static class TextStuff{
		public static void ReplaceTextInFile(string filePath, string originalText, string replaceText){
			var fileContents = System.IO.File.ReadAllText(filePath);
			fileContents = fileContents.Replace(originalText, replaceText); 
			File.WriteAllText(filePath, fileContents);
		}
	}

	public static class CombinationTables{

		public static int[,] GenerateCombinationTable(int numCols, int numValues){
			int[,] table;
			int numRows = (int)Math.Pow(numValues, numCols);

			table = new int[numRows, numCols];

			int divider = numRows;

			// iterate by column
			for (int col = 0; col < numCols; col++) {
				divider /= numValues;
				int cell = 0;
				// iterate every row by this column's index:
				for (int row = 0; row < numRows; row++) {
					table[row, col] = cell;
					if ((divider == 1) || ((row + 1) % divider == 0)) {
						cell = Math.Abs(cell-1);
					}
				}
			}
			return table;
		}


		public static int[,] GenerateCombinationTable(int numCols, int[] numValues){
			int[,] table;
			int numRows = 1;

			foreach (int x in numValues) {
				numRows = numRows * x;
			}

			table = new int[numRows, numCols];

			int divider = numRows;

			// iterate by column
			for (int col = 0; col < numCols; col++) {
				divider /= numValues[col];
				int cell = 0;
				// iterate every row by this column's index:
				for (int row = 0; row < numRows; row++) {
					table[row, col] = cell;
					if ((divider == 1) || ((row + 1) % divider == 0)) {
						cell = Math.Abs(cell-1);
					}
				}
			}
			return table;
		}

	}


	public static class DebugExtension{
		public static void ShowFullStack(){
			StackTrace stack = new StackTrace ();
			UnityEngine.Debug.Log (stack.GetFrame (1).GetMethod ().ToString ());
		}
	}
		

}