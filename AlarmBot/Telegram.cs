﻿using System;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.Timers;
using System.Configuration;

namespace AlarmBot
{
	public sealed class Telegram : Messenger
	{
        private static readonly string BOT_TOKEN = ConfigurationManager.AppSettings["TelegramBotToken"];
        private static readonly string API_URL = $"https://api.telegram.org/bot{BOT_TOKEN}";
        private readonly HttpClient mClient = new HttpClient();

        private readonly List<System.Timers.Timer> drawNotificationTimers = new List<System.Timers.Timer>();

        public override void SetNotification(List<ProductInfo> drawProducts)
        {
            Debug.Assert(drawNotificationTimers.Count == 0);

            foreach (ProductInfo product in drawProducts)
            {
                Debug.Assert(DateTime.Now < product.StartTime);

                System.Timers.Timer drawTimer = new System.Timers.Timer();
                double remainingTime = (product.StartTime - DateTime.Now).TotalMilliseconds;

                drawTimer.Interval = remainingTime;
                drawTimer.Elapsed += (sender, e) => sendMessageToAllUsers(drawTimer, product);
                drawTimer.AutoReset = false;
                drawTimer.Start();

                drawNotificationTimers.Add(drawTimer);
            }
        }

        protected override async void sendMessageToAllUsers(System.Timers.Timer timer, ProductInfo product)
        {
            StringBuilder uriBuilder = new StringBuilder(256);
            List<User> users = DB.GetUsersByMessenger(EMessenger.Telegram);

            foreach(var u in users)
            {
                uriBuilder.Clear();

                uriBuilder.Append(API_URL)
                    .Append("/sendPhoto")
                    .Append($"?chat_id={u.ChatID}")
                    .Append($"&photo={product.ImgUrl}")
                    .Append("&reply_markup={\"inline_keyboard\":[[{ \"text\": \"지금 응모하기\", \"url\":")
                    .Append($"\"{product.Url}\"")
                    .Append("}]]}");

                await mClient.GetAsync(uriBuilder.ToString());
            }

            bool bHasTimer = drawNotificationTimers.Remove(timer);
            Debug.Assert(bHasTimer);
        }
    }
}
