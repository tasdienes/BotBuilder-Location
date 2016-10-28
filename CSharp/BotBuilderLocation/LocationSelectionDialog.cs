﻿namespace Microsoft.Bot.Builder.Location
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Bing;
    using Channels;
    using Connector;
    using Dialogs;
    using Internals.Scorables;
    using Resources;
    using SpecialCommands;

    /// <summary>
    /// Responsible for receiving an address from the user and resolving it.
    /// </summary>
    [Serializable]
    public sealed class LocationSelectionDialog : IDialog<Place>
    {
        private readonly string prompt;
        private readonly LocationOptions options;
        private readonly LocationResourceManager resourceManager;
        private readonly IChannelHandler channelHandler;
        private readonly List<Location> locations;

        public LocationSelectionDialog(
            string channelId,
            string prompt,
            LocationOptions options = LocationOptions.None,
            Assembly resourceAssembly = null,
            string resourceName = null)
        {
            this.prompt = prompt;
            this.options = options;
            this.locations = new List<Location>();
            this.resourceManager = new LocationResourceManager(resourceAssembly, resourceName);
            this.channelHandler = ChannelHandlerFactory.CreateChannelHandler(channelId);
        }

        public async Task StartAsync(IDialogContext context)
        {
            this.locations.Clear();

            if (this.options.HasFlag(LocationOptions.UseNativeControl) && this.channelHandler.HasNativeLocationControl)
            {
                context.Call(
                    this.channelHandler.CreateNativeLocationDialog(this.prompt),
                    async (dialogContext, result) =>
                    {
                        var place = await result;
                        dialogContext.Done(place);
                    });
            }
            else
            {
                await context.PostAsync(this.prompt);
                context.Wait(this.MessageReceivedAsync);
            }
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            var scorables = new List<IScorable<IMessageActivity, double>>
            {
                SpecialCommandsScorables.GetCommand(context, context, this.resourceManager),
                new SelectAddressScorable(context, this.locations)
            };

            var scorable = Scorables.First(scorables);
            int initialFramesCount = context.Frames.Count;

            if (scorable != null && await Scorables.TryPostAsync(scorable, message, CancellationToken.None))
            {
                // TODO: this is a bit of a hack to only call context.Wait if the scorable
                // didn't manipulate the stack. Is there a cleaner way to achieve this?
                if (initialFramesCount == context.Frames.Count)
                {
                    context.Wait(this.MessageReceivedAsync);
                }
            }
            else if (this.locations.Count == 0)
            {
                await TryResolveAddressAsync(context, result);
            }
            else
            {
                await context.PostAsync(
                    this.resourceManager.GetResource(nameof(Strings.InvalidLocationResponse)));

                context.Wait(this.MessageReceivedAsync);
            }
        }

        private async Task TryResolveAddressAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var item = await result;

            // TODO: handle exception
            var locationSet = await new BingGeoSpatialService().GetLocationsByQueryAsync(item.Text);
            var foundLocations = locationSet?.Locations;

            if (foundLocations == null || foundLocations.Count == 0)
            {
                await context.PostAsync(
                    this.resourceManager.GetResource(nameof(Strings.LocationNotFound)));
            }
            else
            {
                this.locations.AddRange(foundLocations);

                var locationsCardReply = context.MakeMessage();
                locationsCardReply.Attachments = AddressCard.CreateLocationsCard(this.locations);
                locationsCardReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                await context.PostAsync(locationsCardReply);

                if (this.locations.Count == 1)
                {
                    this.PromptForSingleAddressSelection(context, item.ChannelId);
                }
                else
                {
                    await this.PromptForMultipleAddressSelection(context, item.ChannelId);
                }
            }
        }

        private void PromptForSingleAddressSelection(IDialogContext context, string channeId)
        {
            PromptStyle style = this.channelHandler.SupportsKeyboard
                        ? PromptStyle.Keyboard
                        : PromptStyle.None;

            PromptDialog.Confirm(
                    context,
                    async (dialogContext, answer) =>
                    {
                        if (await answer)
                        {
                            dialogContext.Done(PlaceExtensions.FromLocation(this.locations.First()));
                        }
                        else
                        {
                            await this.StartAsync(dialogContext);
                        }
                    },
                    prompt: this.resourceManager.GetResource(nameof(Strings.SingleResultFound)),
                    retry: null,
                    attempts: 3,
                    promptStyle: style);
        }

        private async Task PromptForMultipleAddressSelection(IDialogContext context, string channeId)
        {
            var selectText = this.resourceManager.GetResource(nameof(Strings.MultipleResultsFound));

            if (this.channelHandler.SupportsKeyboard)
            {
                var keyboardCardReply = context.MakeMessage();
                keyboardCardReply.Attachments = AddressCard.CreateLocationsKeyboardCard(this.locations, selectText);
                keyboardCardReply.AttachmentLayout = AttachmentLayoutTypes.List;
                await context.PostAsync(keyboardCardReply);
            }
            else
            {
                await context.PostAsync(selectText);
            }

            context.Wait(this.MessageReceivedAsync);
        }
    }
}