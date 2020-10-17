﻿using MixItUp.Base.Commands;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Actions
{
    public enum CommandActionTypeEnum
    {
        RunCommand,
        DisableCommand,
        EnableCommand,
        DisableCommandGroup,
        EnableCommandGroup,
    }

    [DataContract]
    public class CommandAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return CommandAction.asyncSemaphore; } }

        [DataMember]
        public CommandActionTypeEnum CommandActionType { get; set; }

        [DataMember]
        public Guid CommandID { get; set; }

        [DataMember]
        public Type PreMadeType { get; set; }

        [DataMember]
        public string CommandArguments { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        public CommandAction() : base(ActionTypeEnum.Command) { }

        public CommandAction(CommandActionTypeEnum commandActionType, CommandBase command, string commandArguments)
            : this()
        {
            this.CommandActionType = commandActionType;
            if (command is PreMadeChatCommand)
            {
                this.PreMadeType = command.GetType();
                this.CommandID = Guid.Empty;
            }
            else
            {
                this.CommandID = command.ID;
                this.PreMadeType = null;
            }            
            this.CommandArguments = commandArguments;
        }

        public CommandAction(CommandActionTypeEnum commandActionType, string groupName)
            : this()
        {
            this.CommandActionType = commandActionType;
            this.GroupName = groupName;
        }

        public CommandBase Command
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<CommandBase> CommandGroup
        {
            get
            {
                return null;
            }
        }

        protected override async Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            CommandBase command = this.Command;
            if (this.CommandActionType == CommandActionTypeEnum.RunCommand)
            {
                if (command != null)
                {
                    IEnumerable<string> newArguments = null;
                    if (!string.IsNullOrEmpty(this.CommandArguments))
                    {
                        string processedMessage = await this.ReplaceStringWithSpecialModifiers(this.CommandArguments, user, arguments);
                        newArguments = processedMessage.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        newArguments = arguments;
                    }

                    await command.Perform(user, this.platform, newArguments, this.GetExtraSpecialIdentifiers());
                }
            }
            else if (this.CommandActionType == CommandActionTypeEnum.DisableCommand || this.CommandActionType == CommandActionTypeEnum.EnableCommand)
            {
                if (command != null)
                {
                    command.IsEnabled = (this.CommandActionType == CommandActionTypeEnum.EnableCommand) ? true : false;
                    ChannelSession.Services.Chat.RebuildCommandTriggers();
                }
            }
            else if (this.CommandActionType == CommandActionTypeEnum.DisableCommandGroup || this.CommandActionType == CommandActionTypeEnum.EnableCommandGroup)
            {
                IEnumerable<CommandBase> commands = this.CommandGroup;
                if (commands != null)
                {
                    foreach (CommandBase cmd in commands)
                    {
                        cmd.IsEnabled = (this.CommandActionType == CommandActionTypeEnum.EnableCommandGroup) ? true : false;
                    }
                    ChannelSession.Services.Chat.RebuildCommandTriggers();
                }
            }
        }
    }

    #region Obsolete Action Group Action

    [Obsolete]
    [DataContract]
    public class ActionGroupAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return ActionGroupAction.asyncSemaphore; } }

        [DataMember]
        public Guid ActionGroupID { get; set; }

#pragma warning disable CS0612 // Type or member is obsolete
        public ActionGroupAction() : base(ActionTypeEnum.ActionGroup) { }
#pragma warning restore CS0612 // Type or member is obsolete

        protected override Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            return Task.FromResult(0);
        }
    }

    #endregion Obsolete Action Group Action
}
