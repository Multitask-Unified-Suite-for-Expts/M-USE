using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using USE_DisplayManagement;

/// <summary>
/// Represents a coordinate converter that converts between different coordinate systems across Unity Screen and Monitor
/// </summary>
public class USE_CoordinateConverter
{
    // Monitor coordinates all start at the lower left corner of the monitor's active display area and extend to the upper right corner.
    // Screen coordinates all start at the lower left corner of Unity's window and extend to the upper right corner of the same window.
    // Monitor and screen coordinates are the same if this is a build (not the editor) running in full screen. In all other cases, they are different,
    // and the user will have to specify in ScreenDetails what the position of the lower left and upper right corners are.

    /// <summary>
    /// Gets the pixel coordinate from the lower left corner of the monitor.
    /// </summary>
    /// <value>The pixel coordinate.</value>
    public Vector2? MonitorPixel;

    /// <summary>
    /// Gets the coordinate as a proportion of the total monitor size from the lower left corner of the monitor.
    /// </summary>
    /// <value>The coordinate as a proportion.</value>
    public Vector2? MonitorProportion;

    /// <summary>
    /// Gets the coordinate in centimeters from the lower left corner of the monitor.
    /// </summary>
    /// <value>The coordinate in cm.</value>
    public Vector2? MonitorCm;

    /// <summary>
    /// Gets the coordinate as degrees visual angle from the lower left corner of the monitor.
    /// </summary>
    /// <value>The coordinate as DVA.</value>
    public Vector2? MonitorDVA;

    /// <summary>
    /// Gets the coordinate in ADCS (Active Display Coordinate System) from the lower left corner of the monitor.
    /// </summary>
    /// <value>The coordinate in ADCS.</value>
    public Vector2? MonitorADCS;

    /// <summary>
    /// Gets the pixel coordinate from the lower left corner of the screen.
    /// </summary>
    /// <value>The pixel coordinate.</value>
    public Vector2? ScreenPixel;

    /// <summary>
    /// Gets the coordinate as a proportion of the total screen size from the lower left corner of the screen.
    /// </summary>
    /// <value>The coordinate as a proportion.</value>
    public Vector2? ScreenProportion;

    /// <summary>
    /// Gets the coordinate in centimeters from the lower left corner of the screen.
    /// </summary>
    /// <value>The coordinate in cm.</value>
    public Vector2? ScreenCm;

    /// <summary>
    /// Gets the coordinate as degrees visual angle from the lower left corner of the screen.
    /// </summary>
    /// <value>The coordinate as DVA.</value>
    public Vector2? ScreenDVA;

    /// <summary>
    /// Gets the coordinate in ADCS (Active Display Coordinate System) from the lower left corner of the screen.
    /// </summary>
    /// <value>The coordinate in ADCS.</value>
    public Vector2? ScreenADCS;


    // Info necessary to derive constants
    private MonitorDetails MonitorDetails;
    private ScreenDetails ScreenDetails;
    private float EyeDistance;

    // Monitor Constants
    private float monitorPixelsPerCm;
    private float monitorCmPerPixel;
    private float pixelsPerDegree;
    private float degreesPerPixel;
    private float cmPerDegree;

    // Screen Constants
    private float screenPixelsPerCm;
    private float screenCmPerPixel;
    private Vector2 screenLowerLeftPixels;
    private Vector2 screenSizeCm;

    public USE_CoordinateConverter(MonitorDetails monitorDetails, ScreenDetails screenDetails, float eyeDistance = 60f)
    {
        MonitorDetails = monitorDetails;
        ScreenDetails = screenDetails;
        EyeDistance = eyeDistance;

        // Assign Monitor Constants for Calculation
        monitorPixelsPerCm = (MonitorDetails.PixelResolution.x / MonitorDetails.CmSize.x +
                              MonitorDetails.PixelResolution.y / MonitorDetails.CmSize.y) / 2;
        monitorCmPerPixel = (MonitorDetails.CmSize.x / MonitorDetails.PixelResolution.x +
                             MonitorDetails.CmSize.y / MonitorDetails.PixelResolution.y) / 2;
        pixelsPerDegree = 2 * Mathf.Tan(0.5f / Mathf.Rad2Deg) * EyeDistance;
        degreesPerPixel = 2 * Mathf.Atan((monitorCmPerPixel / 2) / EyeDistance);
        cmPerDegree = 1f;

        // Assign Screen Constants for Calculation
        screenPixelsPerCm = ((ScreenDetails.PixelResolution.x / ScreenDetails.ScreenWidth_Cm) +
                             (ScreenDetails.PixelResolution.y / ScreenDetails.ScreenHeight_Cm)) / 2;
        screenCmPerPixel = ((ScreenDetails.ScreenWidth_Cm / ScreenDetails.PixelResolution.x) +
                            (ScreenDetails.ScreenHeight_Cm / ScreenDetails.PixelResolution.y)) / 2;
        screenLowerLeftPixels = ScreenDetails.LowerLeft_Cm * monitorPixelsPerCm;
        screenSizeCm = new Vector2(ScreenDetails.ScreenWidth_Cm, ScreenDetails.ScreenHeight_Cm);
    }

    public void SetScreenDetails(ScreenDetails screenDetails)
    {
        ScreenDetails = screenDetails;

        // Assign Screen Constants for Calculation
        screenPixelsPerCm = ((ScreenDetails.PixelResolution.x / ScreenDetails.ScreenWidth_Cm) +
                             (ScreenDetails.PixelResolution.y / ScreenDetails.ScreenHeight_Cm)) / 2;
        screenCmPerPixel = ((ScreenDetails.ScreenWidth_Cm / ScreenDetails.PixelResolution.x) +
                            (ScreenDetails.ScreenHeight_Cm / ScreenDetails.PixelResolution.y)) / 2;
        screenLowerLeftPixels = ScreenDetails.LowerLeft_Cm * monitorPixelsPerCm;
        screenSizeCm = new Vector2(ScreenDetails.ScreenWidth_Cm, ScreenDetails.ScreenHeight_Cm);
    }

    public void SetMonitorDetails(MonitorDetails monitorDetails)
    {
        MonitorDetails = monitorDetails;

        // Assign Monitor Constants for Calculation
        monitorPixelsPerCm = (MonitorDetails.PixelResolution.x / MonitorDetails.CmSize.x +
                              MonitorDetails.PixelResolution.y / MonitorDetails.CmSize.y) / 2;
        monitorCmPerPixel = (MonitorDetails.CmSize.x / MonitorDetails.PixelResolution.x +
                             MonitorDetails.CmSize.y / MonitorDetails.PixelResolution.y) / 2;
        pixelsPerDegree = 2 * Mathf.Tan(0.5f / Mathf.Rad2Deg) * EyeDistance;
        degreesPerPixel = 2 * Mathf.Atan((monitorCmPerPixel / 2) / EyeDistance);
        cmPerDegree = 1f;
    }

    public void SetEyeDistance(float eyeDistance)
    {
        EyeDistance = eyeDistance;
    }

    public Vector2? GetMonitorADCS(Vector2 coord, string coord_type, float eyeDist = 60f)
    {
        switch (coord_type.ToLower())
        {
            case "monitorpixel":
                MonitorADCS = new Vector2(coord.x / MonitorDetails.PixelResolution.x, 1 - (coord.y / MonitorDetails.PixelResolution.y));
                break;

            case "monitorcm":
                MonitorADCS = (coord * monitorPixelsPerCm) / MonitorDetails.PixelResolution;
                MonitorADCS = new Vector2(((Vector2)MonitorADCS).x, 1 - ((Vector2)MonitorADCS).y);
                break;

            case "monitordva":
                MonitorADCS = (coord * pixelsPerDegree) / MonitorDetails.PixelResolution;
                MonitorADCS = new Vector2(((Vector2)MonitorADCS).x, 1 - ((Vector2)MonitorADCS).y);
                break;
            
            case "screenadcs":
                ScreenPixel = coord * ScreenDetails.PixelResolution;
                ScreenPixel = new Vector2(((Vector2)ScreenPixel).x, ScreenDetails.PixelResolution.y - ((Vector2)ScreenPixel).y);
                ScreenCm = ScreenPixel * screenCmPerPixel;
                MonitorCm = ScreenDetails.LowerLeft_Cm + ScreenCm;
                MonitorADCS = (MonitorCm * monitorPixelsPerCm) / MonitorDetails.PixelResolution;
                MonitorADCS = new Vector2(((Vector2)MonitorADCS).x, 1 - ((Vector2)MonitorADCS).y);
                break;

            case "screenpixel":
                ScreenCm = coord * screenCmPerPixel;
                MonitorCm = ScreenDetails.LowerLeft_Cm + ScreenCm;
                MonitorADCS = (MonitorCm * monitorPixelsPerCm) / MonitorDetails.PixelResolution;
                MonitorADCS = new Vector2(((Vector2)MonitorADCS).x, 1 - ((Vector2)MonitorADCS).y);
                break;

            case "screencm":
                MonitorCm = ScreenDetails.LowerLeft_Cm + coord;
                MonitorADCS = (MonitorCm * monitorPixelsPerCm) / MonitorDetails.PixelResolution;
                MonitorADCS = new Vector2(((Vector2)MonitorADCS).x, 1 - ((Vector2)MonitorADCS).y);
                break;

            case "screendva":
                ScreenDVA = coord;
                ScreenCm = ScreenDVA * cmPerDegree;
                MonitorCm = ScreenDetails.LowerLeft_Cm + ScreenCm;
                MonitorADCS = (MonitorCm * monitorPixelsPerCm) / MonitorDetails.PixelResolution;
                MonitorADCS = new Vector2(((Vector2)MonitorADCS).x, 1 - ((Vector2)MonitorADCS).y);
                break;

            default:
                Debug.LogError("Unknown display coordinate type \"" + coord_type + "\" specified in DisplayCoordinate creation.");
                break;
        }

        return MonitorADCS;
    }
    public Vector2? GetScreenADCS(Vector2 coord, string coord_type, float eyeDist = 60f)
    {
        switch (coord_type.ToLower())
        {
            case "monitorpixel":
                MonitorCm = coord * monitorCmPerPixel;
                ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
                ScreenADCS = (ScreenCm * screenPixelsPerCm) / ScreenDetails.PixelResolution;
                ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
                break;

            case "monitorcm":
                ScreenCm = coord - ScreenDetails.LowerLeft_Cm;
                ScreenADCS = (ScreenCm * screenPixelsPerCm) / ScreenDetails.PixelResolution;
                ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
                break;

            case "monitordva":
                MonitorPixel = coord * pixelsPerDegree;
                MonitorCm = MonitorPixel * monitorCmPerPixel;
                ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
                ScreenADCS = (ScreenCm * screenPixelsPerCm) / ScreenDetails.PixelResolution;
                ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
                break;
            
            case "monitoradcs":
                MonitorPixel = coord * MonitorDetails.PixelResolution;
                MonitorPixel = new Vector2(((Vector2)MonitorPixel).x, MonitorDetails.PixelResolution.y - ((Vector2)MonitorPixel).y);
                MonitorCm = MonitorPixel * monitorCmPerPixel;
                ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
                ScreenADCS = (ScreenCm * screenPixelsPerCm) / ScreenDetails.PixelResolution;
                ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
               break;

            case "screenpixel":
                ScreenADCS = coord / ScreenDetails.PixelResolution;
                ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
                break;

            case "screencm":
                ScreenPixel = coord * screenPixelsPerCm;
                ScreenADCS = ScreenPixel / ScreenDetails.PixelResolution;
                ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
                break;

            case "screendva":
                ScreenDVA = coord;
                ScreenCm = ScreenDVA * cmPerDegree;
                ScreenPixel = ScreenCm * screenPixelsPerCm;
                ScreenADCS = ScreenPixel / ScreenDetails.PixelResolution;
                ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
                break;

            default:
                Debug.LogError("Unknown display coordinate type \"" + coord_type + "\" specified in DisplayCoordinate creation.");
                break;
        }

        return ScreenADCS;
    }
    public Vector2? GetMonitorPixel(Vector2 coord, string coord_type, float eyeDist = 60f)
    {
        switch (coord_type.ToLower())
        {
            // case "monitorpixel":
            //     MonitorCm = coord * monitorCmPerPixel;
            //     ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
            //     ScreenADCS = (ScreenCm * screenPixelsPerCm) / ScreenDetails.PixelResolution;
            //     ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
            //     break;

            case "monitorcm":
                MonitorPixel = coord * monitorPixelsPerCm;
                break;

            case "monitordva":
                MonitorPixel = coord * pixelsPerDegree;
                break;
            
            case "monitoradcs":
                MonitorPixel = coord * MonitorDetails.PixelResolution;
                MonitorPixel = new Vector2(((Vector2)MonitorPixel).x, MonitorDetails.PixelResolution.y - ((Vector2)MonitorPixel).y);
                break;
            
            case "screenadcs":
                ScreenPixel = coord * ScreenDetails.PixelResolution;
                ScreenPixel = new Vector2(((Vector2)ScreenPixel).x, ScreenDetails.PixelResolution.y - ((Vector2)ScreenPixel).y);
                ScreenCm = ScreenPixel * screenPixelsPerCm;
                MonitorCm = ScreenCm + ScreenDetails.LowerLeft_Cm;
                MonitorPixel = MonitorCm * monitorPixelsPerCm;
                break;

            case "screenpixel":
                ScreenCm = coord * screenPixelsPerCm;
                MonitorCm = ScreenCm + ScreenDetails.LowerLeft_Cm;
                MonitorPixel = MonitorCm * monitorPixelsPerCm;
                break;

            case "screencm":
                MonitorCm = coord + ScreenDetails.LowerLeft_Cm;
                MonitorPixel = MonitorCm * monitorPixelsPerCm;
                break;

            case "screendva":
                ScreenCm = coord * cmPerDegree;
                MonitorCm = ScreenCm + ScreenDetails.LowerLeft_Cm;
                MonitorPixel = MonitorCm * monitorPixelsPerCm;
                break;

            default:
                Debug.LogError("Unknown display coordinate type \"" + coord_type + "\" specified in DisplayCoordinate creation.");
                break;
        }

        return ScreenADCS;
    }

}
