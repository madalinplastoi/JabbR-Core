﻿using System;
using JabbR_Core.Data.Models;
using JabbR_Core.Services;

namespace JabbR_Core.Commands
{
    [Command("welcome", "Welcome_CommandInfo", "[message]", "room")]
    public class WelcomeCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            string newWelcome = String.Join(" ", args).Trim();
            ChatService.ValidateWelcome(newWelcome);

            newWelcome = String.IsNullOrWhiteSpace(newWelcome) ? null : newWelcome;

            ChatRoom room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);

            room.EnsureOpen();

            context.Service.ChangeWelcome(callingUser, room, newWelcome);
            context.NotificationService.ChangeWelcome(callingUser, room);
        }
    }
}