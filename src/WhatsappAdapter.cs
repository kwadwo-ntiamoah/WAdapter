using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wadapter.src.Converter;
using Wadapter.src.Models.Incoming;
using Wapi.src;
using ErrorOr;
using Wapi.src.MessageResponse;

namespace Wadapter.src
{
    public class WhatsappAdapter(IConverter converter, IWApi wapi) : CloudAdapter, IWhatsappAdapter
    {

        public new async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(httpRequest);
            ArgumentNullException.ThrowIfNull(httpResponse);
            ArgumentNullException.ThrowIfNull(bot);

            try
            {
                InMessage? incomingMessage;

                // get body stream
                using var stream = new StreamReader(httpRequest.Body);
                var rawContent = await stream.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                // convert incoming request to an incoming message
                incomingMessage = converter.RawContentToInMessage(rawContent);
                ArgumentNullException.ThrowIfNull(incomingMessage);

                var activity = converter.InMessageToActivity(incomingMessage);

                using var context = new TurnContext(this, activity);
                context.TurnState.Add("httpStatus", HttpStatusCode.OK.ToString("D"));

                await RunPipelineAsync(context, bot.OnTurnAsync, cancellationToken).ConfigureAwait(false);

                var statusCode = Convert.ToInt32(context.TurnState.Get<string>("httpStatus"), CultureInfo.InvariantCulture);
                var responseText = context.TurnState.Get<object>("httpBody")?.ToString() ?? string.Empty;

                await WriteAsync(httpResponse, statusCode, responseText, Encoding.UTF8, cancellationToken);
            }
            catch (Exception ex)
            {
                httpResponse.StatusCode = 401;
                Console.WriteLine(ex);
            }
        }

        public static async Task WriteAsync(HttpResponse response, int code, string text, Encoding encoding, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(response);

            ArgumentNullException.ThrowIfNull(text);

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            response.ContentType = "text/plain";
            response.StatusCode = code;

            var data = encoding.GetBytes(text);

            await response.Body.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();

            foreach (var activity in activities)
            {
                if (activity.Type != ActivityTypes.Message)
                {
                    Console.WriteLine($"Unsupported Activity Type: '{activity.Type}'. Only Activities of type 'Message' are supported.");
                    responses.Add(new ResourceResponse { Id = activity.Id });
                    continue;
                }

                var metadata = activity.ChannelData as ActivityChannelData;
                var messageType = metadata?.Type.ToLower() ?? "text";

                ErrorOr<OutBoundMessageResponse> res;
                switch (messageType)
                {
                    case WhatsappMessageTypes.LIST:
                        var listMessage = converter.ActivityToListMessage(activity);
                        res = await wapi.SendMessage(activity.Recipient.Id, listMessage);
                        break;

                    case WhatsappMessageTypes.QUICK_REPLY:
                        var quickReply = converter.ActivityToQuickReplyMessage(activity);
                        res = await wapi.SendMessage(activity.Recipient.Id, quickReply);
                        break;

                    case WhatsappMessageTypes.FLOW:
                        var flowMessage = converter.ActivityToFlowMessage(activity);
                        res = await wapi.SendMessage(activity.Recipient.Id, flowMessage);
                        break;

                    default:
                        var textMessage = converter.ActivityToTextMessage(activity);
                        res = await wapi.SendMessage(activity.Recipient.Id, textMessage);
                        break;
                }

                var responseId = !res.IsError
                    ? res.Value.Messages?.FirstOrDefault()?.Id
                    : activity.Id;

                responses.Add(new ResourceResponse { Id = responseId });
            }

            return [.. responses];
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public class WhatsappMessageTypes
        {
            public const string TEXT = "text";
            public const string LIST = "list_buttons";
            public const string QUICK_REPLY = "quick_reply";
            public const string FLOW = "flow";
        }
    }
}
