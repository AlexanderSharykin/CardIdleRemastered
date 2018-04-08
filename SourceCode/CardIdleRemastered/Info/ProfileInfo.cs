using System;
using System.Collections.Generic;
using System.Linq;

namespace CardIdleRemastered
{
    public class ProfileInfo
    {
        public string BackgroundUrl { get; set; }
        public string AvatarUrl { get; set; }

        public string UserName { get; set; }
        public string Level { get; set; }

        public string BadgeUrl { get; set; }
        public string BadgeTitle { get; set; }
    }

    public class CardIdleProfileInfo : ProfileInfo
    {
        public string ProfileUrl { get; set; }

        public string MessageTitle { get; set; }
        public IEnumerable<MessageLine> MessageLines { get; private set; }

        public void SetMessage(string message)
        {
            var lines = message.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);

            MessageLines = lines.Select(s => s.Trim())
                .Select
                (
                    s => new MessageLine
                         {
                             Text = s,
                             IsLink = s.StartsWith("http"),
                             IsEnumeration = s.StartsWith("- ")
                         }
                ).ToList();
        }
    }

    public class MessageLine
    {
        public bool IsLink { get; set; }

        public bool IsEnumeration { get; set; }

        public string Text { get; set; }
    }
}
