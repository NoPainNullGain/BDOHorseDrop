using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using BotCoreProxy;

namespace VioletHorseDrop
{
    class Program
    {
        public static bool DropFailed = true;
        public static bool HorseInventorOpen;
        public static bool TravelReady;
        public static int AttemptsToSetPath = 10;
        public static int AttemptsToMoveLoot = 5;
        public static bool MoveTrashFailed;

        static void Main()
        {
            HookManager.AddInternalFunctionCallBack(InternalHooks.Pre_OpenWareHouse, typeof(Program).GetMethod("Pre_OpenWareHouse")); // needs to return null in funcResult when we dont use Post_CoreInitialize anymore for testing
            HookManager.AddInternalFunctionCallBack(InternalHooks.Pre_Drop, typeof(Program).GetMethod("Pre_Drop"));
        }

        // hook pre_OpenWarehouse
        public static bool Pre_OpenWareHouse(object[] args, out object funcResult)
        {
            DropFailed = true;
            HorseInventorOpen = false;
            TravelReady = false;

            try
            {
                PathToHorse();

                //If true, we have opend the inventory and can start moving trash to horse.
                if (HorseInventorOpen)
                {
                    //Drop from horse to char
                    MoveItem(MoveFrom.Horse);

                    //Drop from char(always first slot in inventory) to horse
                    MoveItem(MoveFrom.Inventory);

                    //Cleans screen from open UI
                    Util.CleanScreen();

                    DropFailed = false;
                }

                PathFromHorse();
            }
            catch
            {
            }

            funcResult = null; // set to true if you wanna test with hook Post_CoreInitialize 
            return DropFailed;
        }

        public static bool Pre_Drop(object[] args, out object funcResult)
        {
            funcResult = null;

            //This returns true if dropping trash to horse failed, then drop to storage will be initiated
            if (DropFailed)
            {
                return true;
            }

            //This returns false if dropping trash to horse sucessful, then drop to storage will be skipped.
            return false;
        }

        public enum MoveFrom
        {
            Horse,
            Inventory
        }

        private static void MoveItem(MoveFrom from)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(200);
            var attemptsCounter = 0;
            var pos = new Point();

            if (from == MoveFrom.Horse)
            {
                pos = ExternalConfig.Resolution.StartsWith("1920") ? new Point(1072, 289) : new Point(1713, 469);
            }
            else if (from == MoveFrom.Inventory)
            {
                pos = ExternalConfig.Resolution.StartsWith("1920") ? new Point(1487, 344) : new Point(2125, 517);
            }

            // check if inventory is open if not, open it.
            while (!Util.CheckInventory())
            {
                Input.KeyPress(Keys.F5);
                Thread.Sleep(200);
            }

            // check if cursor is hidden
            if (!Mouse.CursorPresents())
            {
                // press ctrl to activate cursor if its hidden
                Mouse.SetCursor();
            }

            do
            {
                if (attemptsCounter >= AttemptsToMoveLoot)
                {
                    MoveTrashFailed = true;
                    break;
                }

                Mouse.MoveMouse(pos, span);
                Thread.Sleep(500);

                Input.MouseClickR();
                Thread.Sleep(500);

                attemptsCounter++;

            } while (!CheckDropDialog());

            if (!MoveTrashFailed)
            {
                Input.KeyPress(Keys.F);
                Thread.Sleep(200);

                if (from == MoveFrom.Inventory)
                {
                    pos.Y += 10;
                    pos.X += 10;
                    Mouse.MoveMouse(pos, TimeSpan.FromMilliseconds(50));

                    for (int i = 0; i < 10; i++)
                    {
                        Input.MouseScrollDown();
                    }
                }

                Thread.Sleep(200);
                Input.KeyPress(Keys.SPACE, 200);
            }

            MoveTrashFailed = false;
        }

        public static bool CheckDropDialog()
        {
            Point p = Mouse.GetCursorPosition();
            using (Bitmap bmp = ImageWorker.GetScreenshot(p.X + 20, p.Y + 5, p.X + 21, p.Y + 6))
            {
                if (ImageWorker.NearblySameColors(bmp.GetPixel(0, 0), Color.FromArgb(112, 87, 53)))
                    return true;
            }
            return false;
        }

        private static void PathToHorse()
        {
            TimeSpan span = TimeSpan.FromMilliseconds(200);
            var attemptsCounter = 0;
            var horseIcon = FindHorseIcon();

            do
            {
                if (attemptsCounter >= AttemptsToSetPath)
                    break;

                if (!TravelReady)
                {
                    // check if cursor is hidden
                    if (!Mouse.CursorPresents())
                    {
                        // press ctrl to activate cursor if its hidden
                        Mouse.SetCursor();
                    }

                    Mouse.MoveMouse(new Point(horseIcon.x, horseIcon.y), span);
                    Thread.Sleep(500);
                    Input.MouseClickR();
                }
                attemptsCounter++;

            } while (!Travelready());

            if (Mouse.CursorPresents())
            {
                // press ctrl to activate cursor if its hidden
                Mouse.SetCursor();
            }

            if (TravelReady)
            {
                // Hit shortcut T to go along the path
                Input.KeyPress(Keys.T);
                Thread.Sleep(200);

                //sleep some seconds until reach horse
                Thread.Sleep(10000);

                if (OpenHorseInventory())
                    HorseInventorOpen = true;
            }

            TravelReady = false;
        }

        private static void PathFromHorse()
        {
            TimeSpan span = TimeSpan.FromMilliseconds(200);
            var attemptsCounter = 0;

            do
            {
                if (attemptsCounter >= AttemptsToSetPath)
                    break;

                if (!TravelReady)
                {
                    // check if cursor is hidden
                    // Hit shortcut T to go along the path
                    Input.KeyDown(Keys.ALT);
                    Thread.Sleep(50);
                    Input.KeyPress(Keys.Three);
                    Input.KeyUp(Keys.ALT);
                }
                attemptsCounter++;

            } while (!Travelready());

            if (Mouse.CursorPresents())
            {
                // press ctrl to activate cursor if its hidden
                Mouse.SetCursor();
            }
            Input.KeyPress(Keys.F7);
            do
            {
                // Hit shortcut T to go along the path
                Input.KeyDown(Keys.ALT);
                Thread.Sleep(50);
                Input.KeyPress(Keys.Three);
                Input.KeyUp(Keys.ALT);

            } while (!Travelready());

            Input.KeyPress((Keys.T));

            //sleep 10 seconds until reach tent
            Thread.Sleep(10000);

            // Hit shortcut T to go along the path
            Input.KeyPress(Keys.F7);
            Thread.Sleep(200);

        }

        public static bool OpenHorseInventory()
        {
            bool found = false;

            for (int i = 0; i < 12; i++)
            {
                try
                {
                    Input.KeyPress(Keys.F5);
                    Thread.Sleep(200);

                    if (!Util.CheckInventory())
                    {
                        Mouse.Rotate30R();
                        Thread.Sleep(200);
                    }
                    else
                    {
                        found = true;
                        break;
                    }
                }
                catch
                {
                }

            }
            return found;
        }

        public static (int x, int y, bool iconFound) FindHorseIcon()
        {
            int x_ = ExternalConfig.Resolution.StartsWith("1920") ? 180 : 190;
            int y_ = ExternalConfig.Resolution.StartsWith("1920") ? 110 : 130;
            var hitList = new List<Point>();
            var hits = 5;
            var counter = 0;

            using (var bmp = ImageWorker.GetDirectBitmap(0, y_, x_, y_ + 600))
            {
                for (int x = 0; x < bmp.Width; x++)
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        Color r = bmp.GetPixel(x, y);
                        Color colorToFind = Color.FromArgb(158, 59, 52);

                        if (r.Equals(colorToFind))
                        {
                            hitList.Add(new Point(x, y));

                            if (counter >= hits)
                            {
                                var lastListPoint = hitList.Last();
                                var coordinates = (x: lastListPoint.X, y: (lastListPoint.Y + y_), iconFound: true);

                                return coordinates;
                            }
                            counter++;
                        }
                    }
                return (x: 0, y: 0, iconFound: false);
            }
        }

        public static bool Travelready()
        {
            int x_ = ExternalConfig.Resolution.StartsWith("1920") ? 1420 : 2060;
            using (Bitmap bmp = ImageWorker.GetScreenshot(x_, 40, x_ + 475, 500))
            {
                for (int x = 0; x < bmp.Width; x++)
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        Color r = bmp.GetPixel(x, y);
                        if (r.R == 0 && r.B > 200)
                        {
                            TravelReady = true;
                            return true;
                        }

                        if (r.R < 50 && r.G > 100 && r.B > 150)
                        {
                            TravelReady = true;
                            return true;
                        }
                    }
                return false;
            }
        }
    }
}
