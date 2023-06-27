﻿using MediatR;

namespace ThreadBot.Notifications
{
    public class MessageReceivedNotification : INotification
    {
        public SocketMessage Message { get; set; }

        public MessageReceivedNotification(SocketMessage message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}