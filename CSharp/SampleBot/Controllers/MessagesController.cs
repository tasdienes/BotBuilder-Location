﻿namespace SampleBot.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Location;
    using Microsoft.Bot.Connector;

#if !DEBUG
    [BotAuthentication]
#endif
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new MainDialog(activity.ChannelId));
            }

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        [Serializable]
        private class MainDialog : IDialog<string>
        {
            private readonly string channelId;

            public MainDialog(string channelId)
            {
                this.channelId = channelId;
            }

            public Task StartAsync(IDialogContext context)
            {
                context.Wait(this.MessageReceivedAsync);
                return Task.FromResult(0);
            }

            private Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
            {
                context.Call(new LocationSelectionDialog(
                    this.channelId,
                    "Hi, where would you like me to ship to your widget?",
                    LocationOptions.None),
                    async (dialogContext, result) =>
                    {
                        var place = await result;
                        if (place != null)
                        {
                            var address = place.GetPostalAddress();
                            string name = place.Name ?? address?.FormattedAddress ?? "the pinned location";
                            await dialogContext.PostAsync($"OK, I will ship it to {name}");
                        }
                        else
                        {
                            await dialogContext.PostAsync("OK, I won't be shipping it");
                        }

                        dialogContext.Done<string>(null);
                    });

                return Task.FromResult(0);
            }

        }
    }
}