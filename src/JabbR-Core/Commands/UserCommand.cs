using System;
﻿using JabbR_Core.Hubs;
﻿using JabbR_Core.Services;
using JabbR_Core.Data.Models;

namespace JabbR_Core.Commands
{
    /// <summary>
    /// Base class for commands that require a user
    /// </summary>
    public abstract class UserCommand : ICommand
    {
        void ICommand.Execute(CommandContext context, CallerContext callerContext, string[] args)
        {
           
            ChatUser user = context.Repository.VerifyUserId(callerContext.UserId);
           
            Execute(context, callerContext, user, args);
        }

        public abstract void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args);
    }
}