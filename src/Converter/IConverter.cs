using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using Wadapter.src.Models.Incoming;
using Wapi.src.OutgoingMessageModels;

namespace Wadapter.src.Converter
{
    public interface IConverter
    {
        public InMessage? RawContentToInMessage(string rawContent);
        public Activity InMessageToActivity(InMessage message);
        public SendText ActivityToTextMessage(Activity activity);
        public SendListInteractive ActivityToListMessage(Activity activity);
        public SendQuickReply ActivityToQuickReplyMessage(Activity activity);
        public SendFlowInteractive ActivityToFlowMessage(Activity activity);
    }
}
