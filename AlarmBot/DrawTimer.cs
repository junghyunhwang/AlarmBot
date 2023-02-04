﻿using System.Diagnostics;

namespace AlarmBot
{
    public static class DrawTimer
    {
        private static readonly System.Timers.Timer newProductTimer = new System.Timers.Timer();
        private static readonly System.Timers.Timer todayDrawTimer = new System.Timers.Timer();
        private static readonly List<Bot> bots = new List<Bot>(16);

        private static readonly int HOURS_TO_CHECK_NEW_PRODUCTS = 17;
        private static readonly int MINUTES_TO_CHECK_NEW_PRODUCTS = 51;

        private static readonly int HOURS_TO_CHECK_TODAY_DRAW = 00;
        private static readonly int MINUTES_TO_CHECK_TODAY_DRAW = 5;

        private static bool IsRunning = false;

        public static int StartTimer()
        {
            if (!BrandManager.IsSetBrand)
            {
                Debug.Assert(false, "BrandManager does not setting!");
                return -1;
            }
            else if (IsRunning)
            {
                Debug.Assert(false, "Already running draw timer!");
                return -1;
            }

            setTimerElapsedEventHandler();
            setMessengerBot();

            startCheckNewProductTimer();
            startCheckTodayDrawTimer();

            IsRunning = true;

            return 1;
        }

        private static void setTimerElapsedEventHandler()
        {
            Debug.Assert(!IsRunning);

            newProductTimer.Elapsed += async (sender, e) => await checkNewProducts();
            todayDrawTimer.Elapsed += (sender, e) => setTodayNotification();
        }

        private static void setMessengerBot()
        {
            Debug.Assert(!IsRunning);
            
            bots.Add(new TelegramBot());
        }

        private static void startCheckNewProductTimer()
        {                                          
            DateTime checkNewProductsTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, HOURS_TO_CHECK_NEW_PRODUCTS, MINUTES_TO_CHECK_NEW_PRODUCTS, 0);

            if (DateTime.Now > checkNewProductsTime)
            {
                checkNewProductsTime = checkNewProductsTime.AddDays(1);
            }

            newProductTimer.Interval = (checkNewProductsTime - DateTime.Now).TotalMilliseconds;

            newProductTimer.Start();
        }

        private static async Task checkNewProducts()
        {
            newProductTimer.Stop();

            var newProducts = await BrandManager.CheckNewProducts();

            if (newProducts.Count > 0)
            {
                DB.InsertProducts(newProducts);
            }

            startCheckNewProductTimer();
        }

        private static void startCheckTodayDrawTimer()
        {
            DateTime checkTodayDrawTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, HOURS_TO_CHECK_TODAY_DRAW, MINUTES_TO_CHECK_TODAY_DRAW, 0);

            if (DateTime.Now > checkTodayDrawTime)
            {
                checkTodayDrawTime = checkTodayDrawTime.AddDays(1);
            }

            todayDrawTimer.Interval = (checkTodayDrawTime - DateTime.Now).TotalMilliseconds;

            todayDrawTimer.Start();
        }

        private static void setTodayNotification()
        {
            newProductTimer.Stop();

            List<ProductInfo> todayDrawProducts = DB.GetTodayDrawProducts();

            foreach (var b in bots)
            {
                b.SetNotification(todayDrawProducts);
            }

            startCheckTodayDrawTimer();
        }
    }
}
