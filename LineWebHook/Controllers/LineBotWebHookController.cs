using System;
using System.Linq;
using System.Web.Configuration;
using System.Web.Http;

using isRock.LineBot;
using isRock.LineBot.Conversation;

using LineWebHook.Models;

namespace LineWebHook.Controllers {
    public class LineBotWebHookController : ApiController {
        string channelAccessToken = WebConfigurationManager.AppSettings["LX22pzM/pJdR2V3qAC+6Nx7nQewBYtqtj3l/EMMuSQi638nwby9aaPNim+BwEb8676AaMMPeE+9lndMuEjz04aCN2eU8Bpq/++GV48lx8Pij9z8OxH/MI6Q/UgSBf28ReRQXrqfqY7BB1y+nWnmhRAdB04t89/1O/w1cDnyilFU="];
        Bot bot { get; set; }
        
        public LineBotWebHookController() { 
            bot = new Bot();
        }

        IHttpActionResult returnJoin(Event lineEvent) {
            var replyToken   = lineEvent.replyToken;
            var replyMessage = $"有人把我加入 {lineEvent.source.type} 中了，大家好啊～";
            
            bot.ReplyMessage(replyToken, replyMessage);
            
            return Ok();    
        }

        IHttpActionResult replyMessage(Event lineEvent) {
            var messageType = lineEvent.message.type;
            var replayToken = lineEvent.replyToken;
            
            switch (messageType) {
                case "text":                    
                    var sourceType   = lineEvent.source.type.ToLower();
                    var receivedText = lineEvent.message.text;

                    if (receivedText == "bye") {
                        bot.ReplyMessage(replayToken, "bye-bye");

                        if (sourceType == "room")                            
                            Utility.LeaveRoom(lineEvent.source.roomId, channelAccessToken);

                        if (sourceType == "group")
                            Utility.LeaveGroup(lineEvent.source.groupId, channelAccessToken);
                    }
                    else {
                        LineUserInfo userInfo = null;
                        var responseMessage = "你說了：" + receivedText;

                        switch (sourceType) {
                            case "room":
                                userInfo = Utility.GetRoomMemberProfile(lineEvent.source.roomId, lineEvent.source.userId, channelAccessToken);
                                break;

                            case "group":
                                userInfo = Utility.GetGroupMemberProfile(lineEvent.source.groupId, lineEvent.source.userId, channelAccessToken);
                                break;

                            case "user":
                                userInfo = Utility.GetUserInfo(lineEvent.source.userId, channelAccessToken);
                                break;

                            default:
                                break;
                        }

                        responseMessage += "\n你是：" + userInfo.displayName;

                        bot.ReplyMessage(replayToken, responseMessage);
                    }
                    
                    break;

                case "sticker":
                    var packageId = lineEvent.message.packageId;
                    var stickerId = lineEvent.message.stickerId;

                    bot.ReplyMessage(replayToken, packageId, stickerId);

                    break;

                default:
                    break;
            }

            return Ok();    
        }

        IHttpActionResult leaveRequestConversation(Event lineEvent) {
            //var cic = new InformationCollector<LeaveRequest>(channelAccessToken);
            //ProcessResult<LeaveRequest> result;
            return NotFound();
        }

        [HttpPost]
        public IHttpActionResult POST() {
            if (Utility.SignatureValidation(this)) {
                var postData  = Request.Content.ReadAsStringAsync().Result;

                try {
                    var received  = Utility.Parsing(postData);                
                    var lineEvent = received.events.FirstOrDefault();                                
                    var eventType = lineEvent.type;

                    switch (eventType) {
                        case "join":
                            return returnJoin(lineEvent);

                        case "message":
                            if (lineEvent.message.text != "我要請假")
                                return replyMessage(lineEvent);
                            else
                                return leaveRequestConversation(lineEvent);

                        default:
                            return BadRequest();
                    }
                }
                catch {
                    return Ok();    
                }
            }
            else {
                return Unauthorized();
            }            
        }
    }
}
