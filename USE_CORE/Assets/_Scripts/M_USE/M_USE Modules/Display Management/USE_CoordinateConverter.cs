using HiddenMaze;
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
        monitorPixelsPerCm = (MonitorDetails.PixelResolution.x / MonitorDetails.CmSize.x + MonitorDetails.PixelResolution.y / MonitorDetails.CmSize.y) / 2;
        monitorCmPerPixel = (MonitorDetails.CmSize.x / MonitorDetails.PixelResolution.x +MonitorDetails.CmSize.y / MonitorDetails.PixelResolution.y) / 2;
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
        // Set Default value to null, we're going to want to override default distance on a per calculation basis
        EyeDistance = eyeDistance;
    }

    // Methods that return the given coordinate system, given the coord type that is passed
    public Vector2? GetMonitorPixel(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);

        return MonitorPixel;
    }
    public Vector2? GetScreenPixel(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);
        ScreenPixel = MonitorPixelToScreenPixel(MonitorPixel);

        return ScreenPixel;
    }
    public Vector2? GetMonitorADCS(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);
        MonitorADCS = MonitorPixelToMonitorADCS(MonitorPixel);
        
        return MonitorADCS;
    }
    public Vector2? GetScreenADCS(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);
        ScreenADCS = MonitorPixelToScreenADCS(MonitorPixel);

        return ScreenADCS;
    }
    public Vector2? GetMonitorCm(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);
        MonitorCm = MonitorPixelToMonitorCm(MonitorPixel);

        return MonitorCm;
    }
    public Vector2? GetScreenCm(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);
        ScreenCm = MonitorPixelToScreenCm(MonitorPixel);

        return ScreenCm;
    }
    public Vector2? GetMonitorDVA(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);
        MonitorDVA = MonitorPixelToMonitorDVA(MonitorPixel);

        return MonitorDVA;
    }
    public Vector2? GetScreenDVA(Vector2 coord, string coord_type, float? eyeDist = null)
    {
        SetEyeDistance((float)eyeDist);
        ConvertToMonitorPixels(coord, coord_type);
        ScreenDVA = MonitorPixelToScreenDVA(MonitorPixel);

        return ScreenDVA;
    }

    // Methods that Convert a given coord to Monitor Pixel
    private Vector2? MonitorCmToMonitorPixel(Vector2? monitorCm)
    {
        MonitorPixel = monitorCm * monitorPixelsPerCm;
        return MonitorPixel;
    }
    private Vector2? MonitorDVAToMonitorPixel(Vector2? monitorDVA)
    {
        MonitorPixel = monitorDVA * pixelsPerDegree;
        return MonitorPixel;
    }
    private Vector2? MonitorADCSToMonitorPixel(Vector2? monitorADCS)
    {
        MonitorPixel = monitorADCS * MonitorDetails.PixelResolution;
        MonitorPixel = new Vector2(((Vector2)MonitorPixel).x, MonitorDetails.PixelResolution.y - ((Vector2)MonitorPixel).y);
        return MonitorPixel;
    }
    private Vector2? ScreenADCSToMonitorPixel(Vector2? screenADCS)
    {
        ScreenPixel = screenADCS * ScreenDetails.PixelResolution;
        ScreenPixel = new Vector2(((Vector2)ScreenPixel).x, ScreenDetails.PixelResolution.y - ((Vector2)ScreenPixel).y);
        ScreenCm = ScreenPixel * screenPixelsPerCm;
        MonitorPixel = ScreenCmToMonitorPixel(ScreenCm);
        return MonitorPixel;
    }
    private Vector2? ScreenPixelToMonitorPixel(Vector2? screenPixel)
    {
        ScreenCm = screenPixel * screenPixelsPerCm;
        MonitorPixel = ScreenCmToMonitorPixel(ScreenCm);
        return MonitorPixel;
    }
    private Vector2? ScreenCmToMonitorPixel(Vector2? screenCm)
    {
        MonitorCm = screenCm + ScreenDetails.LowerLeft_Cm;
        MonitorPixel = MonitorCm * monitorPixelsPerCm;
        return MonitorPixel;
    }
    private Vector2? ScreenDVAToMonitorPixel(Vector2? screenDVA)
    {
        ScreenCm = screenDVA * cmPerDegree;
        MonitorPixel = ScreenCmToMonitorPixel(ScreenCm);
        return MonitorPixel;
    }


    // Methods to Convert a Monitor Pixel coord to any other coord
    private Vector2? MonitorPixelToMonitorCm(Vector2? monitorPixel)
    {
        MonitorCm = monitorPixel * monitorCmPerPixel;
        return MonitorCm;
    }
    private Vector2? MonitorPixelToMonitorDVA(Vector2? monitorPixel)
    {
        MonitorDVA = monitorPixel * degreesPerPixel;
        return MonitorDVA;
    }
    private Vector2? MonitorPixelToMonitorADCS(Vector2? monitorPixel)
    {
        MonitorADCS = monitorPixel / MonitorDetails.PixelResolution;
        MonitorADCS = new Vector2(((Vector2)MonitorADCS).x, 1 - ((Vector2)MonitorADCS).y);
        return MonitorADCS;
    }
    private Vector2? MonitorPixelToScreenADCS(Vector2? monitorPixel)
    {
        MonitorCm = monitorPixel * monitorCmPerPixel;
        ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
        ScreenADCS = (ScreenCm * screenPixelsPerCm) / ScreenDetails.PixelResolution;
        ScreenADCS = new Vector2(((Vector2)ScreenADCS).x, 1 - ((Vector2)ScreenADCS).y);
        return ScreenADCS;
    }
    private Vector2? MonitorPixelToScreenPixel(Vector2? monitorPixel)
    {
        MonitorCm = monitorPixel * monitorCmPerPixel;
        ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
        ScreenPixel = ScreenCm * screenPixelsPerCm;
        return ScreenPixel;
    }
    private Vector2? MonitorPixelToScreenCm(Vector2? monitorPixel)
    {
        MonitorCm = monitorPixel * monitorCmPerPixel;
        ScreenCm = MonitorCm - ScreenDetails.LowerLeft_Cm;
        return ScreenCm;
    }
    private Vector2? MonitorPixelToScreenDVA(Vector2? monitorPixel)
    {
        MonitorDVA = monitorPixel * degreesPerPixel;
        ScreenDVA = MonitorDVA - screenLowerLeftPixels * degreesPerPixel;
        return ScreenDVA;
    }
    
    /// <summary>
    /// Convert to Monitor Pixels given the coord type
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="coord_type"></param>
    private void ConvertToMonitorPixels(Vector2 coord, string coord_type)
    {
        switch (coord_type.ToLower())
        {
            case "monitorpixel":
                MonitorPixel = coord;
                break;

            case "monitorcm":
                MonitorPixel = MonitorCmToMonitorPixel(coord);
                break;

            case "monitordva":
                MonitorPixel = MonitorDVAToMonitorPixel(coord);
                break;

            case "screenadcs":
                MonitorPixel = ScreenADCSToMonitorPixel(coord);
                break;

            case "screenpixel":
                MonitorPixel = ScreenPixelToMonitorPixel(coord);
                break;

            case "screencm":
                MonitorPixel = ScreenCmToMonitorPixel(coord);
                break;

            case "screendva":
                MonitorPixel = ScreenDVAToMonitorPixel(coord);

                break;

            default:
                Debug.LogError("Unknown display coordinate type \"" + coord_type + "\" specified in DisplayCoordinate creation.");
                break;
        }
    }


}
