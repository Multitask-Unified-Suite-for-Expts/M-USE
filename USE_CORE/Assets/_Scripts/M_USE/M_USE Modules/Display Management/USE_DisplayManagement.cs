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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace USE_DisplayManagement {

    /// <summary>
    /// Class detailing the size and resolution of a monitor.
    /// </summary>
    public class MonitorDetails
    {
        /// <summary>
        /// Gets or sets the display number.
        /// </summary>
        /// <value>The display number.</value>
        public int DisplayNumber { get; set; }
        /// <summary>
        /// Gets or sets the pixel resolution.
        /// </summary>
        /// <value>The pixel resolution.</value>
        public Vector2 PixelResolution { get; set; }
        /// <summary>
        /// Gets or sets the size of the monitor.
        /// </summary>
        /// <value>The size of the monitor.</value>
        public Vector2 CmSize { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:USE_DisplayManagement.MonitorDetails"/> class.
        /// </summary>
        /// <param name="disp">Display number.</param>
        /// <param name="pixRes">Pixel resolution.</param>
        /// <param name="cmSize">Size in cm.</param>
        public MonitorDetails(Vector2 pixRes, Vector2 cmSize, int disp = 0){
            DisplayNumber = disp;
            PixelResolution = pixRes;
            CmSize = cmSize;
        }
    }

    /// <summary>
    /// Class detailing the position of the Unity window on the screen.
    /// </summary>
    public class ScreenDetails
    {
        /// <summary>
        /// Gets or sets the position of the lower left corner.
        /// </summary>
        /// <value>Position of the lower left corner, in cm from the lower left corner of the monitor.</value>
        public Vector2 LowerLeft_Cm { get; set; }
        /// <summary>
        /// Gets or sets the position of the upper right corner.
        /// </summary>
        /// <value>Position of the upper right corner, in cm from the lower left corner of the monitor.</value>
        public Vector2 UpperRight_Cm { get; set; }

        public Vector2 PixelResolution { get; set; }

        public float ScreenWidth_Cm { get; set; }
        public float ScreenHeight_Cm { get; set; }

        public ScreenDetails(Vector2 lowerLeft, Vector2 upperRight, Vector2 resolution)
        {
            LowerLeft_Cm = lowerLeft;
            UpperRight_Cm = upperRight;
            PixelResolution = resolution;

            ScreenWidth_Cm = UpperRight_Cm.x - LowerLeft_Cm.x;
            Debug.Log("SCREEN WIDTH_ CM: " +  ScreenWidth_Cm);
            ScreenHeight_Cm = UpperRight_Cm.y - LowerLeft_Cm.y;
            Debug.Log("SCREEN HEIGHT_ CM: " + ScreenHeight_Cm);

        }
    }

    /// <summary>
    /// Class that auto-converts between different 2D coordinate types.
    /// </summary>
    public class DisplayCoordinate
    {
        //Monitor coordinates all start at the lower left corner of the monitor's active display area and extend to the upper right corner.
        //Screen coordinates all start at the lower left corner of Unity's window and extend to the upper right corner of the same window.
        //Monitor and screen coordinates are the same if this is a build (not the editor) running in full screen. In all other cases they are different, 
        //and the user will have to specify in ScreenDetails what the position of the lower left and upper right corners are.

        /// <summary>
        /// Gets the coordinate in pixels from lower left of the monitor.
        /// </summary>
        /// <value>The pixel coordinate.</value>
        public Vector2? MonitorPixel;
        /// <summary>
        /// Gets the coordinate as a proportion of total monitor size from lower left of the monitor.
        /// </summary>
        /// <value>The coordinate as a proportion.</value>
        public Vector2? MonitorProportion;
        /// <summary>
        /// Gets the coordinate as centimetres from lower left of the monitor.
        /// </summary>
        /// <value>The coordinate in cm.</value>
        public Vector2? MonitorCm;
        /// <summary>
        /// Gets the coordinate as degrees visual angle from lower left of the monitor.
        /// </summary>
        /// <value>The coordinate as DVA.</value>
        public Vector2? MonitorDVA;
        public Vector2? ScreenPixel;
        public Vector2? ScreenProportion;
        public Vector2? ScreenCm;
        public Vector2? ScreenDVA;
        public Vector2? ACDS;

        private MonitorDetails MonitorDetails;
        private ScreenDetails ScreenDetails;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:USE_DisplayManagement.DisplayCoordinate"/> class.
        /// </summary>
        /// <param name="coord">Coordinate.</param>
        /// <param name="coord_type">Coordinate type.</param>
        /// <param name="monitorDetails">Monitor details.</param>
        public DisplayCoordinate(Vector2 coord, string coord_type, MonitorDetails monitorDetails)
        {
            MonitorDetails = monitorDetails;
            ScreenDetails = new ScreenDetails(new Vector2(0, 0), monitorDetails.CmSize, monitorDetails.PixelResolution);
            CalculateCoordinates(coord, coord_type);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="T:USE_DisplayManagement.DisplayCoordinate"/> class.
        /// </summary>
        /// <param name="monitorDetails">Monitor details.</param>
        /// <param name="screenDetails">Screen details.</param>
        public DisplayCoordinate(Vector2 coord, string coord_type, MonitorDetails monitorDetails, ScreenDetails screenDetails)
        {
            MonitorDetails = monitorDetails;
            ScreenDetails = screenDetails;
            CalculateCoordinates(coord, coord_type);
        }
        /* /// <summary>
         /// Initializes a new instance of the <see cref="T:USE_DisplayManagement.DisplayCoordinate"/> class.
         /// </summary>
         /// <param name="coord">Coordinate.</param>
         /// <param name="coord_type">Coordinate type.</param>
         /// <param name="monitorDetails">Monitor details.</param>
         /// <param name="eyeDist">Distance of the eye from the monitor, in cm.</param>
         public DisplayCoordinate(Vector2 coord, string coord_type, MonitorDetails monitorDetails, float eyeDist)
         {
             MonitorDetails = monitorDetails;
             ScreenDetails = new ScreenDetails(new Vector2(0, 0), monitorDetails.CmSize, monitorDetails.PixelResolution);
             CalculateCoordinates(coord, coord_type, eyeDist);
         }
         /// <summary>
         /// Initializes a new instance of the <see cref="T:USE_DisplayManagement.DisplayCoordinate"/> class.
         /// </summary>
         /// <param name="coord">Coordinate.</param>
         /// <param name="coord_type">Coordinate type.</param>
         /// <param name="monitorDetails">Monitor details.</param>
         /// <param name="screenDetails">Screen details.</param>
         /// <param name="eyeDist">Distance of the eye from the monitor, in cm.</param>
         public DisplayCoordinate(Vector2 coord, string coord_type, MonitorDetails monitorDetails, ScreenDetails screenDetails, float eyeDist)
         {
             MonitorDetails = monitorDetails;
             ScreenDetails = screenDetails;
             CalculateCoordinates(coord, coord_type, eyeDist);
         }
         /// <summary>
         /// Initializes a new instance of the <see cref="T:USE_DisplayManagement.DisplayCoordinate"/> class.
         /// </summary>
         /// <param name="coord">Coordinate.</param>
         /// <param name="coord_type">Coordinate type.</param>
         /// <param name="monitorDetails">Monitor details.</param>
         /// <param name="eyeDist">Distance of the eye from the monitor, in cm.</param>
         /// <param name="screenDetails">Screen details.</param>
         public DisplayCoordinate(Vector2 coord, string coord_type, MonitorDetails monitorDetails, float eyeDist, ScreenDetails screenDetails)
         {
             MonitorDetails = monitorDetails;
             ScreenDetails = screenDetails;
             CalculateCoordinates(coord, coord_type, eyeDist);
         }*/

        private void CalculateCoordinates(Vector2 coord, string coord_type, float eyeDist = 60f)
        {
            MonitorPixel = null;
            MonitorProportion = null;
            MonitorCm = null;
            MonitorDVA = null;
            ScreenPixel = null;
            ScreenProportion = null;
            ScreenCm = null;
            ScreenDVA = null;
            ACDS = null;

            //constants
            float monitorPixelsPerCm = (MonitorDetails.PixelResolution.x / MonitorDetails.CmSize.x + MonitorDetails.PixelResolution.y / MonitorDetails.CmSize.y) / 2;
            float monitorCmPerPixel = (MonitorDetails.CmSize.x / MonitorDetails.PixelResolution.x + MonitorDetails.CmSize.y / MonitorDetails.PixelResolution.y) / 2;
            float pixelsPerDegree = 2 * Mathf.Tan(0.5f / Mathf.Rad2Deg) * eyeDist;
            float degreesPerPixel = 2 * Mathf.Atan((monitorCmPerPixel / 2) / eyeDist);
            float cmPerDegree = 1f;

            // screen constants

            float screenPixelsPerCm = ((ScreenDetails.PixelResolution.x / ScreenDetails.ScreenWidth_Cm) + (ScreenDetails.PixelResolution.y / ScreenDetails.ScreenHeight_Cm))/2;
            float screenCmPerPixel = ((ScreenDetails.ScreenWidth_Cm / ScreenDetails.PixelResolution.x) + (ScreenDetails.ScreenHeight_Cm / ScreenDetails.PixelResolution.y))/2;
            Vector2 screenLowerLeftPixels = ScreenDetails.LowerLeft_Cm * monitorPixelsPerCm;
            Vector2 screenSizeCm = new Vector2(ScreenDetails.ScreenWidth_Cm, ScreenDetails.ScreenHeight_Cm);

           // float EPSILON = 0.000001f;

            switch (coord_type.ToLower())
            {
                case "monitorpixel":
                    MonitorPixel = coord;
                    break;
                case "monitorproportion":
                    MonitorProportion = coord;
                    MonitorPixel = Vector2.Scale((Vector2)MonitorProportion, MonitorDetails.PixelResolution);
                    break;
                case "monitorcm":
                    MonitorCm = coord;
                    MonitorPixel = MonitorCm * monitorPixelsPerCm;
                    break;
                case "monitordva":
                    MonitorDVA = coord;
                    MonitorPixel = MonitorDVA * pixelsPerDegree;
                    break;
                case "screenpixel":
                    ScreenPixel = coord;
                    ScreenCm = coord * screenCmPerPixel;
                    MonitorCm = ScreenDetails.LowerLeft_Cm + ScreenCm;
                    MonitorPixel = MonitorCm * monitorPixelsPerCm;
                    break;
                case "screenproportion":
                    ScreenProportion = coord;
                    ScreenCm = Vector2.Scale(screenSizeCm, (Vector2)ScreenProportion);
                    MonitorCm = ScreenDetails.LowerLeft_Cm + ScreenCm;
                    MonitorPixel = MonitorCm * monitorPixelsPerCm;
                    break;
                case "screencm":
                    ScreenCm = coord;
                    MonitorCm = ScreenDetails.LowerLeft_Cm + ScreenCm;
                    MonitorPixel = MonitorCm * monitorPixelsPerCm;
                    break;
                case "screendva":
                    ScreenDVA = coord;
                    ScreenCm = ScreenDVA * cmPerDegree;
                    MonitorCm = ScreenDetails.LowerLeft_Cm + ScreenCm;
                    MonitorPixel = MonitorCm * monitorPixelsPerCm;
                    break;
                case "acds":
                    ACDS = coord;
                    MonitorProportion = new Vector2(((Vector2)ACDS).x, 1 - ((Vector2)ACDS).y);
                    MonitorPixel = Vector2.Scale((Vector2)MonitorProportion, MonitorDetails.PixelResolution);
                    break;
                default:
                    Debug.LogError("Unknown display coordinate type \"" + coord_type + "\" specified in DisplayCoordinate creation.");
                    break;
            }

            if (MonitorProportion == null)
            {
                MonitorProportion = new Vector2(((Vector2)MonitorPixel).x / MonitorDetails.PixelResolution.x, ((Vector2)MonitorPixel).y / MonitorDetails.PixelResolution.y);
            }
            if (MonitorProportion == null)
            {
                MonitorCm = Vector2.Scale((Vector2)MonitorProportion, MonitorDetails.CmSize);
            }
            if (MonitorDVA == null)
            {
                MonitorDVA = MonitorPixel * degreesPerPixel;
            }
            if (ScreenPixel == null)
            {
                MonitorCm = MonitorPixel * monitorCmPerPixel;
                ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
                ScreenPixel = ScreenCm * screenPixelsPerCm;
            }
            if (ScreenProportion == null)
            {
                ScreenProportion = new Vector2(((Vector2)ScreenPixel).x / ScreenDetails.PixelResolution.x, ((Vector2)ScreenPixel).y / ScreenDetails.PixelResolution.y);
            }
            if (ScreenCm == null)
            {
                ScreenCm = Vector2.Scale(((Vector2)ScreenProportion), screenSizeCm);
            }
            if (ScreenDVA == null)
            {
                ScreenDVA = MonitorDVA - screenLowerLeftPixels * degreesPerPixel;
            }
            if (ACDS == null)
            {
                ACDS = new Vector2(((Vector2)MonitorProportion).x, 1 - ((Vector2)MonitorProportion).y);
            }
        }
    }

}
