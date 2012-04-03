using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics ;


namespace KinectHelperMethods
{
    //*****************************
    //Code adapted from Jason Mithell's Kinect SDK extension methods
    //http://jason-mitchell.com/kinect-sdk-extension-methods/
    //*****************************
    public static class KinectExtensions
    {
        
        public static Vector2 GetScreenPosition(this Joint joint, KinectSensor kinectRuntime, int screenWidth, int screenHeight)
        {
            //float depthX;
            //float depthY;

            DepthImagePoint DIPoint = kinectRuntime.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution320x240Fps30);//out depthX, out depthY);
            
            //depthX = Math.Max(0, Math.Min(depthX * screenWidth, screenWidth));  //convert to 320, 240 space
            //depthY = Math.Max(0, Math.Min(depthY * screenHeight, screenHeight));  //convert to 320, 240 space
 
            //int colorX;
            //int colorY;
            // only ImageResolution.Resolution640x480 is supported at this point
            //kinectRuntime.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, new ImageViewArea(), (int)depthX, (int)depthY, (short)0, out colorX, out colorY);
 
            // map back to skeleton.Width & skeleton.Height
            //return new Vector2(screenWidth * colorX / 320.0f, screenHeight * colorY / 240f);
            return new Vector2(DIPoint.X, DIPoint.Y);
        }
    }


    //*****************************
    //Code adapted from Raccoonacoon's Getting True Keyboard Input into your XNA Games
    //http://dream-forever.net/Blog/2011/08/29/getting-true-keyboard-input-into-your-xna-games/
    //*****************************
    public class KeyGrabber
    {
        public class KeyFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                /*
                    These are the message constants we will be watching for.
                */
                const int WM_KEYDOWN = 0x0100;
                const int WCHAR_EVENT = 0x0102;

                if (m.Msg == WM_KEYDOWN)
                {
                    /*
                        The TranslateMessage function requires a pointer to be passed to it.
                        Since C# doesn't typically use pointers, we have to make use of the Marshal
                        class to store the Message into a pointer. We can then pass this pointer
                        over to the native function.
                    */
                    IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf(m));
                    Marshal.StructureToPtr(m, pointer, true);
                    TranslateMessage(pointer);
                }
                else if (m.Msg == WCHAR_EVENT)
                {
                    //The WParam parameter contains the true character value
                    //we are after. Print this out to the screen and call the
                    //InboundCharEvent so any events hooked up to this will be
                    //notifed that there is a char ready to be processed.
                    System.Console.WriteLine(m.WParam);
                    char trueCharacter = (char)m.WParam;
                    Console.WriteLine(trueCharacter);

                    if (InboundCharEvent != null)
                        InboundCharEvent(trueCharacter);
                }

                //Returning false allows the message to continue to the next filter or control.
                return false;
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern bool TranslateMessage(IntPtr message);
        }

        public static event Action<char> InboundCharEvent;
        static KeyGrabber()
        {
            Application.AddMessageFilter(new KeyFilter());
        }
    }
}