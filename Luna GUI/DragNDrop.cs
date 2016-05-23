using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using WindowsInput.Native;
using Cursor = System.Windows.Forms.Cursor;

namespace Luna_GUI
{
    internal static class DragNDrop
    {
        private const int leftDown = 0x02;
        private const int leftUp = 0x04;

        private static int lastx;
        private static int lasty;
        public static double pixelsPerItem { get; set; }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        /// <summary>
        ///     into firebrid
        /// </summary>
        /// <param name="tnspath">full path to lua file in explorer</param>
        /// <param name="fileNameNoExtension">file name without extension</param>
        public static void DragFileNDropTo_CX_CAS(string tnspath, string fileNameNoExtension)
        {
            WindowManager.BringLuaFileExplorerToFront(tnspath);
            Thread.Sleep(1000);

            var defaultDragPoint = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.LuaFilePosInExplorer);
            var dragClickX = defaultDragPoint.Item1;
            var dragClickY = defaultDragPoint.Item2 + pixelsPerItem; //from lua to tns file (one above)

            var destDrop = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.screen_mid);

            //WindowManager.MouseClick(dragClickX, dragClickY);
            Thread.Sleep(100);
            GrabForDragNDrop((int) dragClickX, (int) dragClickY, (int) destDrop.Item1, (int) destDrop.Item2);
        }

        public static void GrabForDragNDrop(int xPos, int yPos, int destX, int destY)
        {
            lastx = xPos;
            lasty = yPos;

            Cursor.Position = new Point(xPos, yPos);

            WindowManager.MouseClick(xPos, yPos);
            Thread.Sleep(800);

            mouse_event(leftDown, (uint) xPos, (uint) yPos, 0, 0);

            var t = new Thread(() => OnDragging(destX, destY));
            t.Start();
            t.Join();

            var onFileCloseThread = new Thread(() =>
            {
                while (true)
                {
                    if (Keyboard.IsKeyDown(Key.Escape))
                        break;
                }

                WindowManager.TiKeyboardActionPress(OffsetReader.SpecialClickPoint.docKey);
                Thread.Sleep(100);
                WindowManager.TiKeyPress(VirtualKeyCode.VK_1);
                Thread.Sleep(100);
                WindowManager.TiKeyPress(VirtualKeyCode.VK_3);
                Thread.Sleep(100);
                WindowManager.TiKeyPress(VirtualKeyCode.RIGHT);
                Thread.Sleep(100);
                WindowManager.TiKeyPress(VirtualKeyCode.RETURN);
            });
            onFileCloseThread.SetApartmentState(ApartmentState.STA);
            onFileCloseThread.Start();
        }

        private static void OnDragging(int destx, int desty)
        {
            Thread.Sleep(500);

            var trayPos = OffsetReader.GetSpecialClickPoint(OffsetReader.SpecialClickPoint.firebirdTrayIconPos);
            var emuTaskPosX = trayPos.Item1;
            var emuTaskPosY = trayPos.Item2;

            int tempx = Cursor.Position.X, tempy = Cursor.Position.Y;

            while (Math.Abs(Cursor.Position.X - emuTaskPosX) > 2 || Math.Abs(Cursor.Position.Y - emuTaskPosY) > 2)
            {
                tempx += Cursor.Position.X > emuTaskPosX ? -1 : 1;
                tempy += Cursor.Position.Y > emuTaskPosY ? -1 : 1;
                Cursor.Position = new Point(tempx, tempy);
            }
            Thread.Sleep(1500);

            while (Math.Abs(Cursor.Position.X - destx) > 2 || Math.Abs(Cursor.Position.Y - desty) > 2)
            {
                lastx += Cursor.Position.X > destx ? -1 : 1;
                lasty += Cursor.Position.Y > desty ? -1 : 1;
                Cursor.Position = new Point(lastx, lasty);
            }

            Thread.Sleep(200);
            mouse_event(leftUp, (uint) lastx, (uint) lasty, 0, 0);

            /*enter to confirm sending*/
            Thread.Sleep(2000);
            WindowManager.TiMouseClick(Cursor.Position.X, Cursor.Position.Y);
            Thread.Sleep(100);
            WindowManager.TiKeyPress(VirtualKeyCode.RETURN);
            /*enter to confirm sending*/

            Thread.Sleep(100);
            WindowManager.TiKeyPress(VirtualKeyCode.VK_2);

            /*run file*/
            Thread.Sleep(100);
            WindowManager.TiKeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(100);
            WindowManager.TiKeyPress(VirtualKeyCode.DOWN);
            Thread.Sleep(100);
            WindowManager.TiKeyPress(VirtualKeyCode.RETURN);
            /*run file*/
        }
    }
}