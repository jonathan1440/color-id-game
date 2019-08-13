using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Vuforia;
using Random = System.Random;

public class GameEngine : MonoBehaviour
{
    public TextMesh targetColorText;
    public TextMesh scoreText;
    public TextMesh percentText;
    public TextMesh instructionText;
    public TextMesh percentColorText;
    public TextMesh clockText;
    
    [Tooltip("Use \"grayscale\" if running in Unity Editor, otherwise use \"rgb\"")]
    public string type;

    [Tooltip("percent of image that has to match _targetColor for the player to win the round")]
    public float goalPercentage;

    private string _instructionp1;
    private string _instructionp2;

    // Vuforia stuff
    private static PIXEL_FORMAT _mPixelFormat = PIXEL_FORMAT.UNKNOWN_FORMAT;
    private static bool _mFormatRegistered;
    private static Image _lastImage;
    
    // colour user is trying to find
    private Colour _targetColor;
    // index of _targetColor in _colours array
    private int _targetIndex;
    private int _score = 0;
    // what value did the clock start at this round
    private float _maxClock;
    // time limit for next round is equal to how long it took the user to find the color in the last round
    private float _clock;
    
    // named colors with associated rgb values
    private readonly Colour[] _colours =
    {
        new Colour("black", new Col(0, 0, 0), new Col(0, 0, 0), new Col(10, 10, 10)),
        new Colour("grey", new Col(89, 89, 89), new Col(0, 0, 105), new Col(119, 119, 119)),
        new Colour("silver", new Col(122, 122, 122), new Col(0, 0, 149), new Col(153, 153, 153)),
        new Colour("white", new Col(204, 204, 204), new Col(0, 0, 255), new Col(255, 255, 255)),
        new Colour("red", new Col(191, 0, 0), new Col(0, 255, 255), new Col(255, 38, 38)),
        new Colour("brown", new Col(99, 45, 45), new Col(15, 170, 153), new Col(163, 112, 46)),
        new Colour("gold", new Col(204, 164, 46), new Col(22, 189, 212), new Col(230, 193, 71)),
        new Colour("orange", new Col(218, 124, 0), new Col(20, 255, 235), new Col(255, 179, 33)),
        new Colour("yellow", new Col(213, 186, 0), new Col(30, 255, 255), new Col(217, 255, 32)),
        new Colour("lime", new Col(66, 82, 0), new Col(38, 255, 110), new Col(90, 135, 4)),
        new Colour("green", new Col(0, 217, 33), new Col(60, 255, 255), new Col(94, 255, 43)),
        new Colour("cyan", new Col(0, 217, 198), new Col(90, 255, 255), new Col(33, 230, 255)),
        new Colour("blue", new Col(33, 0, 217), new Col(120, 255, 255), new Col(43, 63, 255)),
        new Colour("purple", new Col(118, 0, 128), new Col(150, 255, 128), new Col(153, 21, 138)),
        new Colour("violet", new Col(186, 0, 213), new Col(150, 255, 255), new Col(255, 33, 217))
    };
    
    #region VUFORIA_IMAGE_GRAB_METHODS

    private static void OnVuforiaStarted()
    {
        // Try to register camera image format
        if (CameraDevice.Instance.SetFrameFormat(_mPixelFormat, true))
        {
            //Debug.Log("Successfully registered pixel format " + mPixelFormat);

            _mFormatRegistered = true;
        }
        else
        {
            Debug.LogError(
                "Failed to register pixel format " + _mPixelFormat +
                "\n the format may be unsupported by your device;" +
                "\n consider using a different pixel format.");

            _mFormatRegistered = false;
        }
    }

    /// <summary>
    ///  Called each time the Vuforia state is updated
    /// </summary>
    private static void OnTrackablesUpdated()
    {
        if (!_mFormatRegistered) return;
        
        var image = CameraDevice.Instance.GetCameraImage(_mPixelFormat);
            
        if (image != null) _lastImage = image;
    }

    private static void OnPause(bool paused)
    {
        if (paused) UnregisterFormat();
        else RegisterFormat();
    }

    /// <summary>
    /// Register the camera pixel format
    /// </summary>
    private static void RegisterFormat()
    {
        _mFormatRegistered = CameraDevice.Instance.SetFrameFormat(_mPixelFormat, true);
    }

    /// <summary>
    /// Unregister the camera pixel format (e.g. call this when app is paused)
    /// </summary>
    private static void UnregisterFormat()
    {
        CameraDevice.Instance.SetFrameFormat(_mPixelFormat, false);
        _mFormatRegistered = false;
    }
    
    #endregion // VUFORIA_IMAGE_GRAB_METHODS
    
    #region COLOR_ID_METHODS
    
    /// <summary>
    /// Calculate percent of image that is white
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>percent as 0.xx (vs xx%)</returns>
    private static double CalcImgWeight(IReadOnlyCollection<byte> bytes)
    {
        return (double) bytes.Sum(pixel => pixel == Convert.ToByte(0xFF) ? 1 : 0) / bytes.Count;
    }
    
    /// <summary>
    /// had to make this bc for some reason System.Drawing.Color wasn't working
    /// I'm guessing it might have something to do with the existence of Unity's Color class 
    /// </summary>
    private class Colour
    {
        // lower and upper bounds of rgb values to fall under this Name
        public readonly Col LowerBound, UpperBound;
        // definition rgb values of Name
        public readonly Col Value;
        // name of color
        public readonly string Name;

        public Colour(string name, Col lowerBound, Col value, Col upperBound)
        {
            Name = name;
            LowerBound = lowerBound;
            Value = value;
            UpperBound = upperBound;
        }

        /// <summary>
        /// Return if color is within the bounds of this object
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public bool Contains(Col color)
        {
            return color > LowerBound && color < UpperBound;
        }
    }

    /// <summary>
    /// This class exists because making my own less than / greater than definitions was easier than looking up how to use an existing class
    /// Also exists bc of issues with the .NET Color class
    /// </summary>
    private class Col
    {
        public readonly int r, g, b;

        public Col(int r, int g, int b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }
        
        public static bool operator <(Col a, Col b)
        {
            return a.r < b.r && a.g < b.g && a.b < b.b;
        }
        public static bool operator >(Col a, Col b)
        {
            return a.r > b.r && a.g > b.g && a.b > b.b;
        }
    }

    /// <summary>
    /// Record which pixels from bytes match colour.
    /// Store those pixels in new array as the color white.
    /// All other pixels are black. 
    /// </summary>
    /// <param name="bytes">image data</param>
    /// <param name="colour"></param>
    /// <param name="type">pixel info type: either "grayscale" or "rgb"</param>
    /// <returns>array with all pixels falling within bounds represented as 0xFFFFFF</returns>
    private static byte[] FindNamedColor(IReadOnlyList<byte> bytes, Colour colour, string type)
    {
        // If grayscale, than each element of bytes will be its own pixel.
        // If rgb, then every three elements of bytes will be their own pixel.
        int val;
        switch (type)
        {
            case "grayscale":
                val = 1;
                break;
            case "rgb":
                val = 3;
                break;
            default:
                val = 1;
                break;
        }
        
        // regardless of pixel type of bytes, the output image will be grayscale
        var newImgData = new byte[bytes.Count / val];
        
        for (var pixel = 0; pixel < bytes.Count; pixel+=val)
        {
            // create Col object from current pixel to compare with colour
            Col col;
            switch (type)
            {
                case "grayscale":
                    col = new Col(bytes[pixel], bytes[pixel], bytes[pixel]);
                    break;
                case "rgb":
                    col = new Col(bytes[pixel], bytes[pixel + 1], bytes[pixel + 2]);
                    break;
                default:
                    col = new Col(bytes[pixel], bytes[pixel], bytes[pixel]);
                    break;
            }
            
            if (colour.Contains(col))
                newImgData[pixel / val] = Convert.ToByte(0xFF);
        }

        return newImgData;
    }

    #endregion // COLOR_ID_METHODS

    /// <summary>
    /// Pick the next colour to have the user guess
    /// </summary>
    /// <param name="lastIndex">last index chosen, used to prevent picking the same colour twice in a row</param>
    /// <param name="choices">array of possible colours</param>
    /// <returns>index of element from choices</returns>
    private static int PickRandomColor(int lastIndex, IReadOnlyCollection<Colour> choices)
    {
        var rand = new Random();
        var ind = lastIndex;
        while (ind == lastIndex)
            ind = rand.Next(choices.Count);
        return ind;
    }
    
    /// <summary>
    /// Update clock, score, targetIndex, targetColor, and UI text (excluding percentageText)
    /// </summary>
    /// <param name="point">Did the player get a point?</param>
    private void UpdateState(bool point)
    {
        // set clock if not the first round
        if (_targetIndex == _colours.Length)
        {
            // we want the clock to start really high to give the user plenty of time for the first round to figure out
            // how the game works
            _clock = 300f;
            _maxClock = _clock;
        }
        else
        {
            // After the first round, the time limit is equal to how long it took the user to find the target color in
            // the previous round
            var temp = _clock;
            _clock = _maxClock - _clock;
            _maxClock = _clock;
        }

        // update variables
        if(point)
            _score += 1;
        _targetIndex = PickRandomColor(_targetIndex, _colours);
        _targetColor = _colours[_targetIndex];
        
        // update UI text
        targetColorText.text = _targetColor.Name;
        scoreText.text = _score.ToString();
        percentColorText.text = _targetColor.Name;
        instructionText.text = _instructionp1 + _targetColor.Name + _instructionp2;
        clockText.text = $"{_clock:0.}";
    }
    
    // Start is called before the first frame update
    private void Start()
    {
        switch (type)
        {
            case "grayscale":
                _mPixelFormat = PIXEL_FORMAT.GRAYSCALE;
                break;
            case "rgb":
                _mPixelFormat = PIXEL_FORMAT.RGB888;
                break;
            default:
                _mPixelFormat = PIXEL_FORMAT.GRAYSCALE;
                break;
        }
        
        CameraDevice.Instance.SetFrameFormat(_mPixelFormat, true);

        // Register Vuforia life-cycle callbacks:
        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
        VuforiaARController.Instance.RegisterTrackablesUpdatedCallback(OnTrackablesUpdated);
        VuforiaARController.Instance.RegisterOnPauseCallback(OnPause);
        
        // set up to choose random color for first round
        _targetIndex = _colours.Length;
        
        // set instruction text
        _instructionp1 = "Fill " + goalPercentage + "% of the screen with the\ncolor ";
        _instructionp2 = " before the timer runs out!";

        // update game state
        UpdateState(false);
    }

    // Update is called once per frame
    private void Update()
    {
        // update clock
        _clock -= Time.deltaTime;
        clockText.text = $"{_clock:0.000}";
        
        // if true, player loses round
        if (_clock <= 0)
            UpdateState(false);
        
        // determine what percent of image is of targetColor
        if (_lastImage == null) return;
        var data = FindNamedColor(_lastImage.Pixels, _targetColor, type);
        var percentage = CalcImgWeight(data) * 100;
        
        // update percentText
        percentText.text = $"{percentage:0.}%";
        
        // if true, player wins round
        if (percentage >= goalPercentage)
            UpdateState(true);
    }
}
