using System;

namespace Ai.SolidWorksAssistant.Chat
{
    public enum ChatRole
    {
        User,
        Assistant,
        System
    }

    public sealed class ChatMessage
    {
        public ChatRole Role { get; private set; }
        public string Text { get; private set; }
        public DateTime Timestamp { get; private set; }
        public bool IsConfirmation { get; private set; }

        public ChatMessage(ChatRole role, string text, bool isConfirmation = false)
        {
            Role = role;
            Text = text ?? string.Empty;
            Timestamp = DateTime.Now;
            IsConfirmation = isConfirmation;
        }

        /// <summary>Very cheap RTL heuristic: does the text contain Persian/Arabic script?</summary>
        public bool IsRtl
        {
            get
            {
                foreach (var ch in Text)
                {
                    var c = (int)ch;
                    if ((c >= 0x0600 && c <= 0x06FF) || (c >= 0x200C && c <= 0x200F) || (c >= 0xFB50 && c <= 0xFEFF))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
