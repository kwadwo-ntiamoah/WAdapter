using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wadapter.src.Models.Incoming;
using Wapi.src.OutgoingMessageModels;

namespace Wadapter.src.Converter
{
    public class WhatsappConverter(IConfiguration config) : IConverter
    {
        public SendListInteractive ActivityToListMessage(Activity activity)
        {
            var channelData = activity.ChannelData as ListChannelData;
            ArgumentNullException.ThrowIfNull(channelData);

            var listMessage = new SendListInteractive
            {
                Header = new SendListHeader
                {
                    Text = channelData.Title
                },
                Body = new SendListBody
                {
                    Text = channelData.Heading
                },
                Footer = new SendListFooter
                {
                    Text = channelData.Footer
                },
                Action = new SendListAction
                {
                    Sections = [
                        new() {
                            Title = channelData.ListTitle,
                            Rows = [.. channelData.Menus.Select(x => new SendListRow {
                                Id = x.Id, Description = x.Description, Title = x.Title,
                            })]
                        }
                    ],
                    Button = channelData.ButtonText
                }
            };

            return listMessage;
        }

        public SendFlowInteractive ActivityToFlowMessage(Activity activity)
        {
            var channelData = activity.ChannelData as FlowChannelData;
            ArgumentNullException.ThrowIfNull(channelData);
            var flowMessage = new SendFlowInteractive
            {
                Header = new SendFlowMessageHeader
                {
                    Text = channelData.Heading
                },
                Body = new SendFlowMessageBody
                {
                    Text = channelData.Body
                },
                Footer = new SendFlowMessageFooter
                {
                    Text = "GCB | your bank for life"
                },
                Action = new SendFlowInteractiveAction
                {
                    Parameters = new SendFlowActionParameters()
                    {
                        FlowToken = config["Whatsapp:AccessToken"]!,
                        FlowId = channelData.FlowId,
                        FlowCta = channelData.FlowButtonText,
                        FlowActionPayload = new SendFlowActionPayload
                        {
                            Screen = channelData?.FlowPayload?.Screen!,
                            Data =  channelData?.FlowPayload?.Data
                        }
                    }
                }
            };

            return flowMessage;
        }

        public SendQuickReply ActivityToQuickReplyMessage(Activity activity)
        {
            var channelData = activity.ChannelData as QuickReplyChannelData;
            ArgumentNullException.ThrowIfNull(channelData);

            var quickReply = new SendQuickReply
            {  

                Body = new()
                {
                    Text = channelData.Body
                },
                Footer = new()
                {
                    Text = channelData.Footer
                },
                Action = new SendQuickReplyAction()
                {
                    Buttons = [.. channelData.Buttons.Select(x => new SendQuickReplyActionButton
                    {
                        Reply = new SendQuickReplyActionButtonReply
                        {
                            Id = x.Id,
                            Title = x.Title
                        } 
                    })]
                }
            };

            return quickReply;
        }

        public SendText ActivityToTextMessage(Activity activity)
        {
            var textMessage = new SendText
            {
                PreviewUrl = false,
                Body = activity.Text
            };

            return textMessage;
        }

        public Activity InMessageToActivity(InMessage message)
        {
            var activity = new Activity
            {
                Id = message.Id,
                Timestamp = DateTime.UtcNow,
                Type = ActivityTypes.Message,
                ChannelId = "whatsapp",
                Conversation = new ConversationAccount { Id = message.From },
                From = new ChannelAccount { Id = message.From },
                Recipient = new ChannelAccount { Id = "whatsapp_bot" },
                Text = message.Body,
                ChannelData = message,
                Attachments = []
            };

            return activity;
        }

        public InMessage? RawContentToInMessage(string rawContent)
        {
            return JsonConvert.DeserializeObject<InMessage?>(rawContent);
        }

        public static string GetWaIdFromActivity(Activity activity)
        {
            var temp = activity.From.Id;

            if (temp.Contains("whatsapp"))
                return temp.Replace("whatsapp:", "");
            else return temp;
        }
    }

    public class ActivityChannelData
    {
        public string Type { get; set; } = "text";
    }

    public class ListChannelData: ActivityChannelData
    {
        public string Title { get; set; } = "Browse Services";
        public string Heading { get; set; } = "Welcome";
        public string Footer { get; set; } = "GCB Bank | your bank for life";
        public string ListTitle { get; set; } = "Services";
        public List<ListMenu> Menus { get; set; } = [];
        public string ButtonText { get; set; } = "Try Me";
    }

    public class ListMenu
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class FlowChannelData: ActivityChannelData
    {
        public string Heading { get; set; } = "Welcome";
        public string Body { get; set; } = "Browse Services";
        public string Footer { get; set; } = "GCB Bank | your bank for life";
        public string FlowId { get; set;} = string.Empty;
        public string FlowButtonText { get; set; } = string.Empty;
        public FlowPayload? FlowPayload { get; set; }
    }

    public class FlowPayload
    {
        public string Screen { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } 
    }

    public class QuickReplyChannelData: ActivityChannelData
    {
        public string Body { get; set; } = "Browse Services";
        public string Footer { get; set; } = "GCB Bank | your bank for life";
        public List<QuickReplyButtons> Buttons { get; set; } = [];
    }

    public class QuickReplyButtons
    {
        public string Id { get; set;} = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
